using System.Net;
using System.Net.Http.Json;

using api.Modules.Kappi.DTOs;

using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace api.Tests.Integration.Kappi.Controllers;

public class AnonymousGalleryControllerTests : ApiTestBase
{
    public AnonymousGalleryControllerTests(TestApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateAnonymousGallery_ShouldSucceed()
    {
        // Act
        var response = await Client.PostAsync("/api/galleries/anonymous", null);
        var content = await response.Content.ReadFromJsonAsync<CreateAnonymousGalleryResponse>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        content.ShouldNotBeNull();
        content.GalleryId.ShouldNotBe(Guid.Empty);
        content.ShortCode.ShouldNotBeNullOrWhiteSpace();
        content.ShortCode.Length.ShouldBeGreaterThanOrEqualTo(4);
        content.AccessKey.ShouldNotBeNullOrWhiteSpace();
        content.ExpiresAt.ShouldBeGreaterThan(DateTime.UtcNow);
        content.ExpiresAt.ShouldBeLessThan(DateTime.UtcNow.AddDays(31));

        // Verify in database
        var dbContext = CreateDbContext();
        var gallery = await dbContext.AnonymousGalleries
            .FirstOrDefaultAsync(g => g.Id == content.GalleryId);

        gallery.ShouldNotBeNull();
        gallery.ShortCode.ShouldBe(content.ShortCode);
        gallery.AccessKey.ShouldBe(content.AccessKey);
        // Compare ExpiresAt with tolerance for potential precision differences
        Math.Abs((gallery.ExpiresAt - content.ExpiresAt).TotalSeconds).ShouldBeLessThan(1);
    }

    [Fact]
    public async Task RequestUploadUrl_WithValidAccessKey_ShouldSucceed()
    {
        // Arrange - Create an anonymous gallery first
        var createResponse = await Client.PostAsync("/api/galleries/anonymous", null);
        var gallery = await createResponse.Content.ReadFromJsonAsync<CreateAnonymousGalleryResponse>();
        gallery.ShouldNotBeNull();

        var requestDto = new RequestUploadUrlDto("test-image.jpg", "image/jpeg");

        // Act
        var response = await PostJsonAsync($"/api/galleries/anonymous/{gallery.AccessKey}/media/request-upload", requestDto);
        var content = await response.Content.ReadFromJsonAsync<UploadUrlResponse>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldNotBeNull();
        content.MediaId.ShouldNotBe(Guid.Empty);
        content.UploadUrl.ShouldNotBeNullOrWhiteSpace();
        content.S3Key.ShouldNotBeNullOrWhiteSpace();
        content.S3Key.ShouldContain(gallery.GalleryId.ToString());
        content.S3Key.ShouldContain("test-image.jpg");

        // Verify media record created in database
        var dbContext = CreateDbContext();
        var media = await dbContext.Media
            .FirstOrDefaultAsync(m => m.Id == content.MediaId);

        media.ShouldNotBeNull();
        media.AnonymousGalleryId.ShouldBe(gallery.GalleryId);
        media.S3Key.ShouldBe(content.S3Key);
    }

    [Fact]
    public async Task RequestUploadUrl_WithInvalidAccessKey_ShouldReturnNotFound()
    {
        // Arrange
        var requestDto = new RequestUploadUrlDto("test-image.jpg", "image/jpeg");

        // Act
        var response = await PostJsonAsync("/api/galleries/anonymous/invalid-access-key/media/request-upload", requestDto);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetGalleryByShortCode_WithValidShortCode_ShouldSucceed()
    {
        // Arrange - Create an anonymous gallery
        var createResponse = await Client.PostAsync("/api/galleries/anonymous", null);
        var gallery = await createResponse.Content.ReadFromJsonAsync<CreateAnonymousGalleryResponse>();
        gallery.ShouldNotBeNull();

        // Act
        var response = await Client.GetAsync($"/api/galleries/anonymous/by-short-code/{gallery.ShortCode}");
        var content = await response.Content.ReadFromJsonAsync<AnonymousGalleryDto>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldNotBeNull();
        content.Id.ShouldBe(gallery.GalleryId);
        content.ShortCode.ShouldBe(gallery.ShortCode);
        content.Media.ShouldNotBeNull();
        content.Media.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetGalleryByShortCode_WithInvalidShortCode_ShouldReturnNotFound()
    {
        // Act
        var response = await Client.GetAsync("/api/galleries/anonymous/by-short-code/invalid");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
