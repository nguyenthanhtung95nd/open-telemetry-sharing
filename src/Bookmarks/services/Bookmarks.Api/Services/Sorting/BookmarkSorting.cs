using Bookmarks.Api.Domain;

namespace Bookmarks.Api.Services.Sorting;

/// <summary>
/// Translates a caller's sort expression into an ordered query.
/// </summary>
/// <remarks>
/// Only the fields named in <see cref="AllowedFields"/> can be sorted on, and the mapping to
/// an actual ordering is an explicit switch. Nothing a caller sends is ever interpolated into
/// a query, so an unknown field is a rejected request rather than an error from deep inside
/// the data layer.
/// </remarks>
public static class BookmarkSorting
{
    private const string DescendingKeyword = "desc";

    /// <summary>Ordering applied when the caller does not ask for one.</summary>
    public const string DefaultField = "createdAtUtc";

    /// <summary>Fields a caller may sort on.</summary>
    public static readonly IReadOnlySet<string> AllowedFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "title",
            "createdAtUtc",
            "status",
            "kind"
        };

    /// <summary>Whether <paramref name="sort"/> names a field that can be sorted on.</summary>
    /// <param name="sort">A sort expression such as <c>title desc</c>. Null or blank is valid.</param>
    /// <returns><c>true</c> when the expression is usable.</returns>
    public static bool IsValid(string? sort) =>
        string.IsNullOrWhiteSpace(sort) || AllowedFields.Contains(FieldOf(sort));

    /// <summary>Extracts just the field name from a sort expression, for error messages.</summary>
    /// <param name="sort">A sort expression such as <c>title desc</c>.</param>
    /// <returns>The field token, or an empty string when there is none.</returns>
    public static string FieldOf(string? sort) =>
        sort?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

    /// <summary>Applies the caller's ordering, falling back to newest-last by creation time.</summary>
    /// <param name="query">The query to order.</param>
    /// <param name="sort">A validated sort expression. Null or blank selects the default.</param>
    /// <returns>The ordered query.</returns>
    public static IQueryable<Bookmark> Apply(IQueryable<Bookmark> query, string? sort)
    {
        ArgumentNullException.ThrowIfNull(query);

        var field = string.IsNullOrWhiteSpace(sort) ? DefaultField : FieldOf(sort);
        var descending = IsDescending(sort);

        return (field.ToLowerInvariant(), descending) switch
        {
            ("title", false) => query.OrderBy(b => b.Title),
            ("title", true) => query.OrderByDescending(b => b.Title),
            ("status", false) => query.OrderBy(b => b.Status),
            ("status", true) => query.OrderByDescending(b => b.Status),
            ("kind", false) => query.OrderBy(b => b.Kind),
            ("kind", true) => query.OrderByDescending(b => b.Kind),
            (_, true) => query.OrderByDescending(b => b.CreatedAtUtc),
            _ => query.OrderBy(b => b.CreatedAtUtc)
        };
    }

    private static bool IsDescending(string? sort) =>
        sort?.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .FirstOrDefault()
            ?.Equals(DescendingKeyword, StringComparison.OrdinalIgnoreCase) ?? false;
}
