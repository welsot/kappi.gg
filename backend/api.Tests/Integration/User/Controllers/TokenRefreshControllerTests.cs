using System.Net;
using System.Net.Http.Json;

using api.Modules.Common.DTO;
using api.Modules.User.DTOs;

using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace api.Tests.Integration.User.Controllers;

public class TokenRefreshControllerTests : ApiTestBase
{
    public TokenRefreshControllerTests(TestApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task RefreshToken_WithValidRefreshToken_ReturnsNewAccessToken()
    {
        // Register and login to get initial tokens
        var registrationDto = new UserRegistrationDto { Email = "refresh-test@example.com" };
        await PostJsonAsync("/api/users/register", registrationDto);

        var dbContext = CreateDbContext();
        var user = await dbContext.Users.FirstAsync(u => u.Email == "refresh-test@example.com");
        var otp = await dbContext.OneTimePasswords.FirstAsync(o => o.UserId == user.Id);

        var loginDto = new UserLoginDto { Email = "refresh-test@example.com", Code = otp.Code };
        var loginResponse = await PostJsonAsync("/api/users/login", loginDto);
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<ApiTokenResponse>();

        // Use refresh token to get new access token
        var refreshDto = new TokenRefreshDto { RefreshToken = loginContent!.RefreshToken };
        var refreshResponse = await PostJsonAsync("/api/token/refresh", refreshDto);
        var refreshContent = await refreshResponse.Content.ReadFromJsonAsync<ApiTokenResponse>();

        // Assertions
        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        refreshContent.ShouldNotBeNull();
        refreshContent.Token.ShouldNotBeNullOrEmpty();
        refreshContent.Token.ShouldNotBe(loginContent.Token); // Should be a new token
        refreshContent.RefreshToken.ShouldBe(loginContent.RefreshToken); // Same refresh token
        refreshContent.User.Email.ShouldBe("refresh-test@example.com");
    }

    [Fact]
    public async Task RefreshToken_WithInvalidRefreshToken_ReturnsUnauthorized()
    {
        var refreshDto = new TokenRefreshDto { RefreshToken = "invalid-refresh-token" };
        var refreshResponse = await PostJsonAsync("/api/token/refresh", refreshDto);

        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var errorContent = await refreshResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        errorContent.ShouldNotBeNull();
        errorContent.Code.ShouldBe("invalid_refresh_token");
    }

    [Fact]
    public async Task RefreshToken_WithExpiredRefreshToken_ReturnsUnauthorized()
    {
        // Register and login to get initial tokens
        var registrationDto = new UserRegistrationDto { Email = "expired-refresh-test@example.com" };
        await PostJsonAsync("/api/users/register", registrationDto);

        var dbContext = CreateDbContext();
        var user = await dbContext.Users.FirstAsync(u => u.Email == "expired-refresh-test@example.com");
        var otp = await dbContext.OneTimePasswords.FirstAsync(o => o.UserId == user.Id);

        var loginDto = new UserLoginDto { Email = "expired-refresh-test@example.com", Code = otp.Code };
        var loginResponse = await PostJsonAsync("/api/users/login", loginDto);
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<ApiTokenResponse>();

        // Manually expire the refresh token
        var refreshToken = await dbContext.RefreshTokens.FirstAsync(t => t.Token == loginContent!.RefreshToken);
        dbContext.Entry(refreshToken).Property("ExpiresAt").CurrentValue = DateTime.UtcNow.AddDays(-1);
        await dbContext.SaveChangesAsync();

        // Try to use expired refresh token
        var refreshDto = new TokenRefreshDto { RefreshToken = loginContent!.RefreshToken };
        var refreshResponse = await PostJsonAsync("/api/token/refresh", refreshDto);

        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var errorContent = await refreshResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        errorContent.ShouldNotBeNull();
        errorContent.Code.ShouldBe("invalid_refresh_token");
    }
}
