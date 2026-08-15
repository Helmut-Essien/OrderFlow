using OrderFlow.Application.Common.Interfaces;

namespace OrderFlow.Infrastructure.Identity;

/// <summary>BCrypt password hashing. Never log plaintext passwords.</summary>
public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
