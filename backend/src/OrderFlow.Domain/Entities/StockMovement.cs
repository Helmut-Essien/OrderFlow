using OrderFlow.Domain;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Domain.Entities;

/// <summary>
/// Immutable audit row for a stock change. Adjustment is manual; Reserve/Deduct/Release come from order status changes.
/// </summary>
public class StockMovement
{
    /// <summary>ULID primary key.</summary>
    public string Id { get; private set; } = NUlid.Ulid.NewUlid().ToString();

    /// <summary>Tenant shop; must match the product's shop.</summary>
    public string ShopId { get; private set; } = string.Empty;

    /// <summary>Product this movement belongs to.</summary>
    public string ProductId { get; private set; } = string.Empty;

    /// <summary>Signed quantity applied (positive inbound, negative outbound).</summary>
    public int QuantityDelta { get; private set; }

    /// <summary>On-hand stock after this movement (0–99,999,999).</summary>
    public int ResultingStock { get; private set; }

    /// <summary>Adjustment for manual stock; Reserve/Deduct/Release for order status changes.</summary>
    public StockMovementType Type { get; private set; } = StockMovementType.Adjustment;

    /// <summary>Optional reason, max 400 characters.</summary>
    public string? Notes { get; private set; }

    /// <summary>JWT user who applied the change, when known.</summary>
    public string? CreatedByUserId { get; private set; }

    /// <summary>UTC insert time. Rows are immutable after create.</summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>Product navigation. Required by EF; do not use in handlers.</summary>
    public Product Product { get; private set; } = null!;

    private StockMovement()
    {
    }

    /// <summary>Creates a movement after stock has already been applied on the product.</summary>
    public static StockMovement Create(
        string shopId,
        string productId,
        int quantityDelta,
        int resultingStock,
        StockMovementType type,
        string? notes,
        string? createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shopId);
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);

        if (resultingStock < 0 || resultingStock > ProductConstraints.MaxStock)
            throw new ArgumentOutOfRangeException(nameof(resultingStock), "Resulting stock must be between 0 and 99,999,999.");

        var normalizedNotes = NormalizeOptional(notes, ProductConstraints.NotesMaxLength, nameof(notes));
        var normalizedUserId = string.IsNullOrWhiteSpace(createdByUserId) ? null : createdByUserId.Trim();

        return new StockMovement
        {
            ShopId = shopId.Trim(),
            ProductId = productId.Trim(),
            QuantityDelta = quantityDelta,
            ResultingStock = resultingStock,
            Type = type,
            Notes = normalizedNotes,
            CreatedByUserId = normalizedUserId
        };
    }

    private static string? NormalizeOptional(string? value, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new ArgumentOutOfRangeException(paramName, $"Value cannot exceed {maxLength} characters.");

        return trimmed;
    }
}
