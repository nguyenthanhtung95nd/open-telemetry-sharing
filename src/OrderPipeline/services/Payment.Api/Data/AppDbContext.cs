using Payment.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Payment.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PaymentTransaction> Payments => Set<PaymentTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            // One order can only ever be charged once. The handler checks for an existing
            // row first, but only the database can settle two concurrent deliveries, and
            // being charged twice is the worst failure this system could produce.
            entity.HasIndex(payment => payment.OrderId).IsUnique();
        });
    }
}
