namespace OrderFlow.Domain;

/// <summary>
/// Canonical product field limits. FluentValidation, EF, Shared DTOs, and Angular <c>PRODUCT_FIELD_LIMITS</c> must match these values.
/// </summary>
public static class ProductConstraints
{
    /// <summary>Display name max length (FluentValidation, EF, Angular).</summary>
    public const int NameMaxLength = 200;

    /// <summary>SKU max length; stored uppercase and unique per shop.</summary>
    public const int SkuMaxLength = 50;

    /// <summary>Optional category max length.</summary>
    public const int CategoryMaxLength = 80;

    /// <summary>Stock-adjustment notes max length.</summary>
    public const int NotesMaxLength = 400;

    /// <summary>On-hand stock upper bound (0–this value).</summary>
    public const int MaxStock = 99_999_999;

    /// <summary>Unit price upper bound in GHS (2 decimal places).</summary>
    public const decimal MaxPrice = 999_999_999.99m;
}
