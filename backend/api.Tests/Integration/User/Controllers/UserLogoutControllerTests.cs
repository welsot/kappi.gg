using System.Net;
using System.Net.Http.Json;

using api.Modules.Common.DTO;
using api.Modules.User.DTOs;

using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace api.Tests.Integration.User.Controllers;

public class UserLogoutControllerTests : ApiTestBase
{
    public UserLogoutControllerTests(TestApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Logout_WithValidToken_RevokesRefreshTokens()
    {
        // Register and login to get tokens
        var registrationDto = new UserRegistrationDto { Email = "logout-test@example.com" };
        await PostJsonAsync("/api/users/register", registrationDto);

        var dbContext = CreateDbContext();
        var user = await dbContext.Users.FirstAsync(u => u.Email == "logout-test@example.com");
        var otp = await dbContext.OneTimePasswords.FirstAsync(o => o.UserId == user.Id);

        var loginDto = new UserLoginDto { Email = "logout-test@example.com", Code = otp.Code };
        var loginResponse = await PostJsonAsync("/api/users/login", loginDto);
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<ApiTokenResponse>();

        // Logout using the access token
        var logoutResponse = await PostJsonAsync("/api/users/logout", new { }, loginContent!.Token);

        // Assertions
        logoutResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var logoutResponseContent = await logoutResponse.Content.ReadFromJsonAsync<SuccessResponse>();
        logoutResponseContent.ShouldNotBeNull();
        logoutResponseContent.Message.ShouldBe("logged_out");

        // Verify refresh token is revoked
        var refreshToken = await dbContext.RefreshTokens.FirstAsync(t => t.Token == loginContent.RefreshToken);
        refreshToken.RevokedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Logout_WithoutToken_ReturnsUnauthorized()
    {
        var logoutResponse = await PostJsonAsync("/api/users/logout", new { });

        logoutResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
