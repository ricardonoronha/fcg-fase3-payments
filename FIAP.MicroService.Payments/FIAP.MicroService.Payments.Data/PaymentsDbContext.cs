using FIAP.MicroService.Payments.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FIAP.MicroService.Payments.Data;

public class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<Checkout> Checkouts => Set<Checkout>();
    public DbSet<CheckoutEvent> CheckoutEvents => Set<CheckoutEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Checkout>(e =>
        {
            e.HasIndex(x => x.Status);
            e.HasIndex(x => new { x.UserId, x.CreatedAt });
            e.HasMany(x => x.Events)
             .WithOne(ev => ev.Checkout)
             .HasForeignKey(ev => ev.CheckoutId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CheckoutEvent>(e =>
        {
            e.Property<long>(x => x.Id).ValueGeneratedOnAdd();
            e.HasIndex(x => new { x.CheckoutId, x.CreatedAt });
            e.Property(x => x.Type).HasMaxLength(64).IsRequired();
        });
    }
}
