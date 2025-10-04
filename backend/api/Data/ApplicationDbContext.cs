using api.Modules.Kappi.Models;
using api.Modules.User.Models;

using Microsoft.EntityFrameworkCore;

namespace api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    public DbSet<ApiToken> ApiTokens { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<OneTimePassword> OneTimePasswords { get; set; }

    public DbSet<AnonymousGallery> AnonymousGalleries { get; set; }
    public DbSet<Gallery> Galleries { get; set; }
    public DbSet<Media> Media { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //modelBuilder.ApplyConfiguration(new OneTimePassword.Configuration());
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder.UseSnakeCaseNamingConvention());
    }
}