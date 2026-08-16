namespace OrderFlow.Application.Common.Interfaces;

/// <summary>BCrypt password hashing. Never log plaintext passwords.</summary>
public interface IPasswordHasher
{
    /// <summary>Returns a BCrypt hash. Never log <paramref name="password"/>.</summary>
    string Hash(string password);

    /// <summary>Constant-time verify against a stored BCrypt hash.</summary>
    bool Verify(string password, string passwordHash);
}
