using AllocationHub.Core.Abstractions;
using Google.Apis.Auth;

namespace AllocationHub.Infrastructure.Security;

/// <summary>
/// Google sign-in settings. ClientId is PUBLIC by design (it ships in the browser); what makes the
/// flow safe is the token validation below. AllowedEmails (optional, comma-separated) restricts who
/// may enter — empty means any Google account (fine for the demo, documented as such).
/// </summary>
public class GoogleAuthOptions
{
    public string? ClientId { get; set; }
    public string? AllowedEmails { get; set; }

    public bool Enabled => !string.IsNullOrWhiteSpace(ClientId);

    public bool IsEmailAllowed(string email) =>
        string.IsNullOrWhiteSpace(AllowedEmails) ||
        AllowedEmails.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(e => e.Equals(email, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Validates the ID token cryptographically against Google's published keys and checks the audience
/// is OUR client id — i.e. the token was minted for this app, not any Google-signed token.
/// </summary>
public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly GoogleAuthOptions _options;

    public GoogleTokenValidator(GoogleAuthOptions options) => _options = options;

    public async Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken ct = default)
    {
        if (!_options.Enabled) return null;
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { _options.ClientId! } });
            if (string.IsNullOrWhiteSpace(payload.Email)) return null;
            var name = string.IsNullOrWhiteSpace(payload.Name) ? payload.Email.Split('@')[0] : payload.Name;
            return new GoogleUserInfo(payload.Email, name);
        }
        catch (InvalidJwtException)
        {
            return null; // expired, wrong audience, bad signature — all just "not valid"
        }
    }
}
