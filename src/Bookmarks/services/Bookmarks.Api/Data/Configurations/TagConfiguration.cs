using Bookmarks.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookmarks.Api.Data.Configurations;

/// <summary>Maps <see cref="Tag"/> onto its table.</summary>
public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    private const int IdMaxLength = 500;
    private const int NameMaxLength = 50;
    private const int DescriptionMaxLength = 50;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasMaxLength(IdMaxLength);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(NameMaxLength);
        builder.Property(t => t.Description).HasMaxLength(DescriptionMaxLength);

        // This index enforces exact-name uniqueness only. Refusing names that differ just by
        // case is a service rule in TagService, which means two concurrent requests could in
        // principle both pass that check. Closing that gap needs a case-insensitive collation
        // or an index on lower(name), neither of which this demo needs.
        builder.HasIndex(t => t.Name).IsUnique();
    }
}
