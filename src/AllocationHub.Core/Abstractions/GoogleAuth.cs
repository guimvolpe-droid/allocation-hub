namespace AllocationHub.Core.Abstractions;

/// <summary>The identity Google asserted for a signed-in user (already validated).</summary>
public record GoogleUserInfo(string Email, string Name);

/// <summary>
/// Validates a Google ID token (the JWT the "Sign in with Google" button hands the browser) and
/// returns the asserted identity, or null when the token is invalid. Contract lives in Core; the
/// implementation (Google.Apis.Auth) lives in Infrastructure — same seam as every other integration.
/// </summary>
public interface IGoogleTokenValidator
{
    Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken ct = default);
}
