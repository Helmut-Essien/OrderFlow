namespace OrderFlow.Domain.Entities;

/// <summary>
/// Immutable snapshot of one SKU on an order. Name, SKU, and unit price are copied at create so later catalog edits do not rewrite history.
/// </summary>
public class OrderLine
{
    /// <summary>ULID primary key.</summary>
    public string Id { get; private set; } = NUlid.Ulid.NewUlid().ToString();

    /// <summary>Parent order.</summary>
    public string OrderId { get; private set; } = string.Empty;

    /// <summary>Tenant shop; denormalized so EF can filter lines without joining orders.</summary>
    public string ShopId { get; private set; } = string.Empty;

    /// <summary>Catalog product this line was sold from. The product may later be renamed or deactivated.</summary>
    public string ProductId { get; private set; } = string.Empty;

    /// <summary>Product name at order time, max 200 characters.</summary>
    public string ProductName { get; private set; } = string.Empty;

    /// <summary>Uppercase SKU at order time, max 50 characters.</summary>
    public string Sku { get; private set; } = string.Empty;

    /// <summary>Units ordered (1–99,999,999).</summary>
    public int Quantity { get; private set; }

    /// <summary>Unit price in GHS at order time, rounded to 2 decimal places.</summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>Line total in GHS (<see cref="Quantity"/> × <see cref="UnitPrice"/>).</summary>
    public decimal LineTotal { get; private set; }

    /// <summary>Parent order navigation. Required by EF; do not use in handlers.</summary>
    public Order Order { get; private set; } = null!;

    /// <summary>Catalog product navigation. Required by EF; do not use in handlers.</summary>
    public Product Product { get; private set; } = null!;

    private OrderLine()
    {
    }

    /// <summary>Creates a line after the parent order id exists. Does not touch stock.</summary>
    public static OrderLine Create(
        string orderId,
        string shopId,
        string productId,
        string productName,
        string sku,
        int quantity,
        decimal unitPrice)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);
        ArgumentException.ThrowIfNullOrWhiteSpace(shopId);
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);

        if (quantity < OrderConstraints.MinLineQuantity || quantity > OrderConstraints.MaxLineQuantity)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be between 1 and 99,999,999.");

        if (unitPrice < 0 || unitPrice > ProductConstraints.MaxPrice)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price must be between 0 and 999,999,999.99.");

        var roundedPrice = decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero);
        var lineTotal = decimal.Round(quantity * roundedPrice, 2, MidpointRounding.AwayFromZero);

        return new OrderLine
        {
            OrderId = orderId.Trim(),
            ShopId = shopId.Trim(),
            ProductId = productId.Trim(),
            ProductName = NormalizeRequired(productName, ProductConstraints.NameMaxLength, nameof(productName)),
            Sku = Product.NormalizeSku(sku),
            Quantity = quantity,
            UnitPrice = roundedPrice,
            LineTotal = lineTotal
        };
    }

    private static string NormalizeRequired(string value, int maxLength, string paramName)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
            throw new ArgumentException("Value cannot be empty.", paramName);
        if (trimmed.Length > maxLength)
            throw new ArgumentOutOfRangeException(paramName, $"Value cannot exceed {maxLength} characters.");
        return trimmed;
    }
}
