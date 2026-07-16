using System.Security.Claims;
using AllocationHub.Api.Mapping;
using AllocationHub.Core.Abstractions;
using AllocationHub.Core.Domain;
using AllocationHub.Core.Dtos;
using AllocationHub.Infrastructure.Data;
using AllocationHub.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllocationHub.Api.Controllers;

public record GoogleLoginRequest(string IdToken);
public record AuthConfigDto(bool GoogleEnabled, string? GoogleClientId);

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IGoogleTokenValidator _google;
    private readonly GoogleAuthOptions _googleOptions;

    public AuthController(
        AppDbContext db, IPasswordHasher hasher, ITokenService tokens,
        IGoogleTokenValidator google, GoogleAuthOptions googleOptions)
    {
        _db = db; _hasher = hasher; _tokens = tokens; _google = google; _googleOptions = googleOptions;
    }

    /// <summary>What login methods the SPA should offer. ClientId is public by design.</summary>
    [AllowAnonymous]
    [HttpGet("config")]
    public ActionResult<AuthConfigDto> Config() =>
        new AuthConfigDto(_googleOptions.Enabled, _googleOptions.ClientId);

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        // Google-only accounts have no password hash — never feed an empty hash to BCrypt.
        if (user is null || string.IsNullOrEmpty(user.PasswordHash) || !_hasher.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        return new AuthResponse(_tokens.CreateToken(user), user.ToDto());
    }

    /// <summary>
    /// OAuth sign-in: the browser gets an ID token from Google, we validate it cryptographically
    /// (audience = our client id), find-or-create the user, and issue OUR OWN JWT — the rest of the
    /// app never learns or cares how the session started.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> Google(GoogleLoginRequest req, CancellationToken ct)
    {
        if (!_googleOptions.Enabled)
            return BadRequest(new { message = "Google sign-in is not configured." });

        var identity = await _google.ValidateAsync(req.IdToken, ct);
        if (identity is null)
            return Unauthorized(new { message = "Invalid Google token." });
        if (!_googleOptions.IsEmailAllowed(identity.Email))
            return Forbid();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == identity.Email, ct);
        if (user is null)
        {
            user = new User
            {
                Name = identity.Name, Email = identity.Email,
                PasswordHash = string.Empty, // Google-only account: password login stays blocked
                Role = UserRole.Admin        // single-role MVP; RBAC is documented roadmap
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
        }

        return new AuthResponse(_tokens.CreateToken(user), user.ToDto());
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue("sub") ?? "0");
        var user = await _db.Users.FindAsync(id);
        return user is null ? Unauthorized() : user.ToDto();
    }
}
