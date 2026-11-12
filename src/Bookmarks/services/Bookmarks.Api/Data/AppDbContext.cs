using Bookmarks.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bookmarks.Api.Data;

/// <summary>Entity Framework context for the bookmarks schema.</summary>
/// <param name="options">Context options supplied by dependency injection.</param>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>The saved links.</summary>
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();

    /// <summary>The labels that can be attached to bookmarks.</summary>
    public DbSet<Tag> Tags => Set<Tag>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(Schemas.Application);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
