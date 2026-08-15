namespace OrderFlow.Application.Common.Interfaces;

/// <summary>BCrypt password hashing. Never log plaintext passwords.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
