using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SafeVault.Api.Data;
using SafeVault.Api.Models;
using SafeVault.Api.Security;

namespace SafeVault.Api.Services;

public interface IAuthService
{
    Task<Guid> RegisterAsync(RegisterRequest req, CancellationToken ct);
    Task<Guid?> ValidateLoginAsync(LoginRequest req, CancellationToken ct);
}

public sealed class AuthService : IAuthService
{
    private readonly SafeVaultDbContext _db;
    private readonly IPasswordHasher<AppUser> _hasher;

    public AuthService(SafeVaultDbContext db, IPasswordHasher<AppUser> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<Guid> RegisterAsync(RegisterRequest req, CancellationToken ct)
    {
        var u = InputSanitizer.SanitizeUserName(req.UserName);
        if (u.WasModified) throw new ValidationException("Username contained forbidden characters.");
        InputSanitizer.ValidateUserNameOrThrow(u.Value);

        var exists = await _db.Users.AnyAsync(x => x.UserName == u.Value, ct);
        if (exists) throw new ValidationException("Username already exists.");

        var user = new AppUser { UserName = u.Value };
        user.PasswordHash = _hasher.HashPassword(user, req.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return user.Id;
    }

    public async Task<Guid?> ValidateLoginAsync(LoginRequest req, CancellationToken ct)
    {
        var u = InputSanitizer.SanitizeUserName(req.UserName);
        if (u.WasModified) return null;
        if (!RegexChecks.UserNameOk(u.Value)) return null;

        var user = await _db.Users.SingleOrDefaultAsync(x => x.UserName == u.Value, ct);
        if (user is null) return null;

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
        return result == PasswordVerificationResult.Success ? user.Id : null;
    }

    private static class RegexChecks
    {
        public static bool UserNameOk(string userName) =>
            System.Text.RegularExpressions.Regex.IsMatch(userName, @"^[a-zA-Z0-9_]{3,32}$");
    }
}
