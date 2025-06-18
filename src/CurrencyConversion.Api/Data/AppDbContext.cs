using Microsoft.EntityFrameworkCore;
using CurrencyConversion.Api.Models;

namespace CurrencyConversion.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<ExchangeRateLog> ExchangeRateLogs => Set<ExchangeRateLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Currency>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<ExchangeRateLog>(entity =>
        {
            entity.Property(e => e.Rate).HasColumnType("decimal(18,6)");
            entity.HasOne(e => e.BaseCurrency)
                .WithMany(c => c.BaseRateLogs)
                .HasForeignKey(e => e.BaseCurrencyId);
            entity.HasOne(e => e.TargetCurrency)
                .WithMany(c => c.TargetRateLogs)
                .HasForeignKey(e => e.TargetCurrencyId);
        });
    }
}
