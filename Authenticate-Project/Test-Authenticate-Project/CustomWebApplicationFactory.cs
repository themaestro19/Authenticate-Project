using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using SafeVault.Api.Data;
using System.Data.Common;

namespace SafeVault.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace SafeVaultDbContext with SQLite in-memory (open connection kept alive)
            var dbContextDescriptor = services.Single(s => s.ServiceType == typeof(DbContextOptions<SafeVaultDbContext>));
            services.Remove(dbContextDescriptor);

            services.AddSingleton<DbConnection>(_ =>
            {
                var conn = new SqliteConnection("DataSource=:memory:");
                conn.Open();
                return conn;
            });

            services.AddDbContext<SafeVaultDbContext>((sp, opt) =>
            {
                var conn = sp.GetRequiredService<DbConnection>();
                opt.UseSqlite(conn);
            });

            // Build provider and init schema + seed
            var sp2 = services.BuildServiceProvider();
            using var scope = sp2.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SafeVaultDbContext>();
            db.Database.EnsureCreated();

            Seed(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>());
        });
    }

    private static void Seed(SafeVaultDbContext db, IPasswordHasher<AppUser> hasher)
    {
        if (db.Users.Any()) return;

        var admin = new AppUser { UserName = "admin" };
        admin.PasswordHash = hasher.HashPassword(admin, "P@ssw0rd!LongEnough");
        db.Users.Add(admin);
        db.SaveChanges();
    }
}
