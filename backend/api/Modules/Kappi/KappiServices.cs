using api.Modules.Kappi.Models;
using api.Modules.Kappi.Repository;
using api.Modules.Kappi.Services;

using Microsoft.AspNetCore.Identity;

namespace Microsoft.Extensions.DependencyInjection;

public static class KappiServices
{
    public static IServiceCollection AddKappiServices(this IServiceCollection services)
    {
        // Register repositories
        services.AddScoped<IAnonymousGalleryRepository, AnonymousGalleryRepository>();
        services.AddScoped<IGalleryRepository, GalleryRepository>();
        services.AddScoped<IMediaRepository, MediaRepository>();

        // Register services
        services.AddScoped<ShortCodeGenerator>();
        services.AddScoped<MediaMetadataService>();

        // Register password hasher for Gallery
        services.AddScoped<IPasswordHasher<Gallery>, PasswordHasher<Gallery>>();

        // Add background services
        services.AddHostedService<AnonymousGalleryCleanupService>();

        return services;
    }
}
