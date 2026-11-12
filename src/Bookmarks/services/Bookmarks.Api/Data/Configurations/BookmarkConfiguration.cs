using Bookmarks.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookmarks.Api.Data.Configurations;

/// <summary>Maps <see cref="Bookmark"/> onto its table, including its tags.</summary>
public sealed class BookmarkConfiguration : IEntityTypeConfiguration<Bookmark>
{
    private const int IdMaxLength = 500;
    private const int TitleMaxLength = 200;
    private const int UrlMaxLength = 2000;
    private const int DescriptionMaxLength = 500;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Bookmark> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasMaxLength(IdMaxLength);
        builder.Property(b => b.Title).IsRequired().HasMaxLength(TitleMaxLength);
        builder.Property(b => b.Url).IsRequired().HasMaxLength(UrlMaxLength);
        builder.Property(b => b.Description).HasMaxLength(DescriptionMaxLength);

        builder.HasMany(b => b.Tags)
            .WithMany(t => t.Bookmarks)
            .UsingEntity<BookmarkTag>(
                right => right
                    .HasOne<Tag>()
                    .WithMany()
                    .HasForeignKey(bt => bt.TagId)
                    .OnDelete(DeleteBehavior.Cascade),
                left => left
                    .HasOne<Bookmark>()
                    .WithMany()
                    .HasForeignKey(bt => bt.BookmarkId)
                    .OnDelete(DeleteBehavior.Cascade),
                join => join.HasKey(bt => new { bt.BookmarkId, bt.TagId }));
    }
}
