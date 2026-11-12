using Bookmarks.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookmarks.Api.Data.Configurations;

/// <summary>
/// Shapes the join table's own columns. The relationship itself is declared from the
/// bookmark side in <see cref="BookmarkConfiguration"/>, because a many-to-many can only be
/// configured from one end.
/// </summary>
public sealed class BookmarkTagConfiguration : IEntityTypeConfiguration<BookmarkTag>
{
    private const int IdMaxLength = 500;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BookmarkTag> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property(bt => bt.BookmarkId).HasMaxLength(IdMaxLength);
        builder.Property(bt => bt.TagId).HasMaxLength(IdMaxLength);
    }
}
