using Microsoft.EntityFrameworkCore;

namespace SafeVault.Api.Data;

public sealed class SafeVaultDbContext : DbContext
{
    public SafeVaultDbContext(DbContextOptions<SafeVaultDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<FinancialRecord> FinancialRecords => Set<FinancialRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.UserName)
            .IsUnique();

        modelBuilder.Entity<AppUser>()
            .Property(u => u.UserName)
            .HasMaxLength(32);

        modelBuilder.Entity<FinancialRecord>()
            .Property(r => r.Name)
            .HasMaxLength(80);

        modelBuilder.Entity<FinancialRecord>()
            .Property(r => r.Currency)
            .HasMaxLength(3);
    }
}
