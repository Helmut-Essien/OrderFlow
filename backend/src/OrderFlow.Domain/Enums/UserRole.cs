namespace OrderFlow.Domain.Enums;

/// <summary>
/// Shop staff role stored as a PostgreSQL string. Owner is created at signup; Assistant is added later.
/// </summary>
public enum UserRole
{
    /// <summary>Shop owner; first user created at signup.</summary>
    Owner = 0,

    /// <summary>Staff user with limited permissions (settings slice).</summary>
    Assistant = 1
}
