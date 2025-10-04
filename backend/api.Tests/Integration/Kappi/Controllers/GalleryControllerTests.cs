using System.Net;
using System.Net.Http.Json;

using api.Modules.Kappi.DTOs;

using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace api.Tests.Integration.Kappi.Controllers;

public class GalleryControllerTests : ApiTestBase
{
    public GalleryControllerTests(TestApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateGallery_WithValidData_ShouldSucceed()
    {
        // Arrange
        var apiToken = await LoginUserAsync(Client);
        var createDto = new CreateGalleryDto(IsPublic: true, Password: null);

        // Act
        var response = await PostJsonAsync("/api/galleries", createDto, apiToken.Token);
        var content = await response.Content.ReadFromJsonAsync<GalleryDto>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        content.ShouldNotBeNull();
        content.Id.ShouldNotBe(Guid.Empty);
        content.ShortCode.ShouldNotBeNullOrWhiteSpace();
        content.IsPublic.ShouldBeTrue();
        content.HasPassword.ShouldBeFalse();
        content.Media.ShouldNotBeNull();
        content.Media.Media.Count.ShouldBe(0);
        content.Media.TotalCount.ShouldBe(0);

        // Verify in database
        var dbContext = CreateDbContext();
        var gallery = await dbContext.Galleries
            .FirstOrDefaultAsync(g => g.Id == content.Id);

        gallery.ShouldNotBeNull();
        gallery.ShortCode.ShouldBe(content.ShortCode);
        gallery.IsPublic.ShouldBeTrue();
        gallery.PasswordHash.ShouldBeNull();
    }

    [Fact]
    public async Task CreateGallery_WithPassword_ShouldSucceed()
    {
        // Arrange
        var apiToken = await LoginUserAsync(Client);
        var createDto = new CreateGalleryDto(IsPublic: true, Password: "SecurePassword123");

        // Act
        var response = await PostJsonAsync("/api/galleries", createDto, apiToken.Token);
        var content = await response.Content.ReadFromJsonAsync<GalleryDto>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        content.ShouldNotBeNull();
        content.HasPassword.ShouldBeTrue();

        // Verify password hash is stored
        var dbContext = CreateDbContext();
        var gallery = await dbContext.Galleries
            .FirstOrDefaultAsync(g => g.Id == content.Id);

        gallery.ShouldNotBeNull();
        gallery.PasswordHash.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CreateGallery_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var createDto = new CreateGalleryDto(IsPublic: true, Password: null);

        // Act
        var response = await PostJsonAsync("/api/galleries", createDto);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyGalleries_WithAuth_ShouldSucceed()
    {
        // Arrange
        var apiToken = await LoginUserAsync(Client);

        // Create two galleries
        var createDto1 = new CreateGalleryDto(IsPublic: true, Password: null);
        await PostJsonAsync("/api/galleries", createDto1, apiToken.Token);

        var createDto2 = new CreateGalleryDto(IsPublic: false, Password: null);
        await PostJsonAsync("/api/galleries", createDto2, apiToken.Token);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/galleries");
        request.Headers.Add("X-API-TOKEN", apiToken.Token);
        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadFromJsonAsync<GalleryListResponse>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldNotBeNull();
        content.Galleries.Count.ShouldBeGreaterThanOrEqualTo(2);
        content.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetGalleryById_WithAuth_ShouldSucceed()
    {
        // Arrange
        var apiToken = await LoginUserAsync(Client);
        var createDto = new CreateGalleryDto(IsPublic: true, Password: null);
        var createResponse = await PostJsonAsync("/api/galleries", createDto, apiToken.Token);
        var createdGallery = await createResponse.Content.ReadFromJsonAsync<GalleryDto>();
        createdGallery.ShouldNotBeNull();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/galleries/{createdGallery.Id}");
        request.Headers.Add("X-API-TOKEN", apiToken.Token);
        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadFromJsonAsync<GalleryDto>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldNotBeNull();
        content.Id.ShouldBe(createdGallery.Id);
    }

    [Fact]
    public async Task UpdateGallery_WithAuth_ShouldSucceed()
    {
        // Arrange
        var apiToken = await LoginUserAsync(Client);
        var createDto = new CreateGalleryDto(IsPublic: true, Password: null);
        var createResponse = await PostJsonAsync("/api/galleries", createDto, apiToken.Token);
        var createdGallery = await createResponse.Content.ReadFromJsonAsync<GalleryDto>();
        createdGallery.ShouldNotBeNull();

        var updateDto = new UpdateGalleryDto(IsPublic: false, Password: "NewPassword123");

        // Act
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/galleries/{createdGallery.Id}")
        {
            Content = JsonContent.Create(updateDto)
        };
        request.Headers.Add("X-API-TOKEN", apiToken.Token);
        var response = await Client.SendAsync(request);
        var content = await response.Content.ReadFromJsonAsync<GalleryDto>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldNotBeNull();
        content.IsPublic.ShouldBeFalse();
        content.HasPassword.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteGallery_WithAuth_ShouldSucceed()
    {
        // Arrange
        var apiToken = await LoginUserAsync(Client);
        var createDto = new CreateGalleryDto(IsPublic: true, Password: null);
        var createResponse = await PostJsonAsync("/api/galleries", createDto, apiToken.Token);
        var createdGallery = await createResponse.Content.ReadFromJsonAsync<GalleryDto>();
        createdGallery.ShouldNotBeNull();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/galleries/{createdGallery.Id}");
        request.Headers.Add("X-API-TOKEN", apiToken.Token);
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify deleted from database
        var dbContext = CreateDbContext();
        var gallery = await dbContext.Galleries.FirstOrDefaultAsync(g => g.Id == createdGallery.Id);
        gallery.ShouldBeNull();
    }

    [Fact]
    public async Task GetPublicGalleryByShortCode_WithoutPassword_ShouldSucceed()
    {
        // Arrange
        var apiToken = await LoginUserAsync(Client);
        var createDto = new CreateGalleryDto(IsPublic: true, Password: null);
        var createResponse = await PostJsonAsync("/api/galleries", createDto, apiToken.Token);
        var createdGallery = await createResponse.Content.ReadFromJsonAsync<GalleryDto>();
        createdGallery.ShouldNotBeNull();

        // Act - Access without auth
        var response = await Client.GetAsync($"/api/galleries/by-short-code/{createdGallery.ShortCode}");
        var content = await response.Content.ReadFromJsonAsync<GalleryDto>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldNotBeNull();
        content.Id.ShouldBe(createdGallery.Id);
    }

    [Fact]
    public async Task GetPublicGalleryByShortCode_WithPassword_ShouldRequirePassword()
    {
        // Arrange
        var apiToken = await LoginUserAsync(Client);
        var createDto = new CreateGalleryDto(IsPublic: true, Password: "SecurePass123");
        var createResponse = await PostJsonAsync("/api/galleries", createDto, apiToken.Token);
        var createdGallery = await createResponse.Content.ReadFromJsonAsync<GalleryDto>();
        createdGallery.ShouldNotBeNull();

        // Act - Try to access without password
        var response = await Client.GetAsync($"/api/galleries/by-short-code/{createdGallery.ShortCode}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task VerifyGalleryPassword_WithCorrectPassword_ShouldSucceed()
    {
        // Arrange
        var apiToken = await LoginUserAsync(Client);
        var password = "SecurePass123";
        var createDto = new CreateGalleryDto(IsPublic: true, Password: password);
        var createResponse = await PostJsonAsync("/api/galleries", createDto, apiToken.Token);
        var createdGallery = await createResponse.Content.ReadFromJsonAsync<GalleryDto>();
        createdGallery.ShouldNotBeNull();

        var verifyDto = new VerifyGalleryPasswordDto(password);

        // Act
        var response = await PostJsonAsync($"/api/galleries/by-short-code/{createdGallery.ShortCode}/verify-password", verifyDto);
        var content = await response.Content.ReadFromJsonAsync<GalleryDto>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldNotBeNull();
        content.Id.ShouldBe(createdGallery.Id);
    }

    [Fact]
    public async Task VerifyGalleryPassword_WithIncorrectPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var apiToken = await LoginUserAsync(Client);
        var createDto = new CreateGalleryDto(IsPublic: true, Password: "SecurePass123");
        var createResponse = await PostJsonAsync("/api/galleries", createDto, apiToken.Token);
        var createdGallery = await createResponse.Content.ReadFromJsonAsync<GalleryDto>();
        createdGallery.ShouldNotBeNull();

        var verifyDto = new VerifyGalleryPasswordDto("WrongPassword");

        // Act
        var response = await PostJsonAsync($"/api/galleries/by-short-code/{createdGallery.ShortCode}/verify-password", verifyDto);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
