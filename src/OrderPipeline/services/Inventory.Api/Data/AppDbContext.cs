using Inventory.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    /// <summary>One row per order that has reserved stock. See <see cref="StockReservation"/>.</summary>
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockReservation>(entity =>
        {
            entity.HasKey(reservation => reservation.OrderId);

            // The order id comes from the event, so the database must not generate it.
            // Without this the key would be an identity column and every delivery would
            // get a fresh row instead of colliding, which is the whole point of the table.
            entity.Property(reservation => reservation.OrderId).ValueGeneratedNever();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasData(
                new Product { Id = 1, Name = "Keyboard", Price = 49.99m, AvailableQuantity = 100 },
                new Product { Id = 2, Name = "Mouse", Price = 19.99m, AvailableQuantity = 200 },
                new Product { Id = 3, Name = "Monitor", Price = 199.99m, AvailableQuantity = 50 }
            );
        });
    }
}
