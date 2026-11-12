using Bookmarks.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookmarks.Api.Extensions;

/// <summary>Database startup helpers.</summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Applies any pending migrations.
    /// </summary>
    /// <param name="app">The running application.</param>
    /// <returns>A task that completes when the database is up to date.</returns>
    /// <remarks>
    /// Development only. Migrating from the application process races when more than one
    /// instance starts at once, so a real deployment runs migrations as a separate step.
    /// </remarks>
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
