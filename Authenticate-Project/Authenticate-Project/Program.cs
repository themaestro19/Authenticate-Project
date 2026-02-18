using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SafeVault.Api.Data;
using SafeVault.Api.Filters;
using SafeVault.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(o =>
{
    // Sanitizes string fields on incoming DTOs before your controller runs
    o.Filters.Add<SanitizeStringsFilter>();
});

builder.Services.AddDbContext<SafeVaultDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("SafeVault")));

builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

// Needed for WebApplicationFactory<Program> in integration tests
public partial class Program { }
