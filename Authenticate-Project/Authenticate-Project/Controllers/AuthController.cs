using Microsoft.AspNetCore.Mvc;
using SafeVault.Api.Models;
using SafeVault.Api.Security;
using SafeVault.Api.Services;

namespace SafeVault.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req, CancellationToken ct)
    {
        try
        {
            var userId = await _auth.RegisterAsync(req, ct);
            return CreatedAtAction(nameof(Register), new { userId }, new { userId });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req, CancellationToken ct)
    {
        var userId = await _auth.ValidateLoginAsync(req, ct);
        if (userId is null) return Unauthorized();

        // Demo only: return the userId; in real app return a JWT / session
        return Ok(new { userId });
    }
}
