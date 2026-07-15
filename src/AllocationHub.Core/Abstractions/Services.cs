using AllocationHub.Core.Domain;

namespace AllocationHub.Core.Abstractions;

/// <summary>Hashes and verifies passwords. Implemented with BCrypt in Infrastructure.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

/// <summary>Issues signed JWT access tokens for authenticated users.</summary>
public interface ITokenService
{
    string CreateToken(User user);
}
