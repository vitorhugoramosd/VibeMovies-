using Microsoft.EntityFrameworkCore;
using VibeMovies.Domain.Entities;

namespace VibeMovies.Infrastructure.Persistence;

public class VibeMoviesDbContext : DbContext
{
    public VibeMoviesDbContext(DbContextOptions<VibeMoviesDbContext> options) : base(options)
    {
    }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<WatchlistItem> WatchlistItems => Set<WatchlistItem>();
    public DbSet<CachedMovie> CachedMovies => Set<CachedMovie>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Name).HasMaxLength(120).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(180).IsRequired();
            entity.Property(u => u.PreferredRegion).HasMaxLength(10).HasDefaultValue("BR");
            entity.Property(u => u.DefaultCity).HasMaxLength(100).HasDefaultValue("Joinville/SC");
            entity.Property(u => u.MyProvidersCsv).HasMaxLength(500).IsRequired().HasDefaultValue(string.Empty);
        });

        modelBuilder.Entity<WatchlistItem>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.HasIndex(w => new { w.UserId, w.TmdbMovieId }).IsUnique();
            entity.Property(w => w.Title).HasMaxLength(250).IsRequired();
            entity.HasOne(w => w.User)
                  .WithMany(u => u.WatchlistItems)
                  .HasForeignKey(w => w.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CachedMovie>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => new { c.TmdbMovieId, c.Region }).IsUnique();
        });
    }
}
