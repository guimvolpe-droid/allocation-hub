using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AllocationHub.Core.Abstractions;
using AllocationHub.Core.Domain;
using Microsoft.IdentityModel.Tokens;

namespace AllocationHub.Infrastructure.Security;

/// <summary>Options for signing JWTs. Bound from configuration (Jwt section).</summary>
public class JwtOptions
{
    public string Key { get; set; } = "dev-only-super-secret-key-change-me-please-32b";
    public string Issuer { get; set; } = "AllocationHub";
    public string Audience { get; set; } = "AllocationHub";
    public int ExpiryHours { get; set; } = 8;
}

public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}

public class JwtTokenService : ITokenService
{
    private readonly JwtOptions _opt;
    public JwtTokenService(JwtOptions opt) => _opt = opt;

    public string CreateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_opt.ExpiryHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
