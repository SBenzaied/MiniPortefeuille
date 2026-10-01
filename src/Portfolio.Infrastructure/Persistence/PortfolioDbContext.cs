using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Abstractions;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Persistence;

public sealed class PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : DbContext(options), IPortfolioDbContext
{
    public DbSet<Advisor> Advisors => Set<Advisor>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<PositionAuditEntry> PositionAudit => Set<PositionAuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Advisor>(e =>
        {
            e.ToTable("advisors");
            e.Property(a => a.UserName).HasMaxLength(50);
            e.HasIndex(a => a.UserName).IsUnique();
            e.Property(a => a.DisplayName).HasMaxLength(100);
            e.Property(a => a.PasswordHash).HasMaxLength(500);
        });

        modelBuilder.Entity<Client>(e =>
        {
            e.ToTable("clients");
            e.Property(c => c.Name).HasMaxLength(150);
            e.Property(c => c.RiskProfile).HasConversion<string>().HasMaxLength(20);
            e.HasOne(c => c.Advisor).WithMany(a => a.Clients).HasForeignKey(c => c.AdvisorId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(c => new { c.AdvisorId, c.RiskProfile });
        });

        modelBuilder.Entity<Currency>(e =>
        {
            e.ToTable("currencies");
            e.HasKey(c => c.Code);
            e.Property(c => c.Code).HasMaxLength(3);
            e.Property(c => c.Name).HasMaxLength(50);
            e.Property(c => c.RateToChf).HasPrecision(18, 8);
        });

        modelBuilder.Entity<Position>(e =>
        {
            e.ToTable("positions");
            e.Property(p => p.Instrument).HasMaxLength(100);
            e.Property(p => p.AssetClass).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.Quantity).HasPrecision(18, 6);
            e.Property(p => p.UnitPrice).HasPrecision(18, 6);
            e.Property(p => p.CurrencyCode).HasMaxLength(3);
            e.Ignore(p => p.MarketValue);
            e.HasOne(p => p.Client).WithMany(c => c.Positions).HasForeignKey(p => p.ClientId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Currency>().WithMany().HasForeignKey(p => p.CurrencyCode).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PositionAuditEntry>(e =>
        {
            e.ToTable("position_audit");
            e.Property(a => a.Action).HasConversion<string>().HasMaxLength(20);
            e.Property(a => a.Before).HasMaxLength(500);
            e.Property(a => a.After).HasMaxLength(500);
            e.HasIndex(a => a.ClientId);
        });
    }
}
