namespace OrderFlow.Domain;

/// <summary>
/// Canonical product field limits. FluentValidation, EF, Shared DTOs, and Angular <c>PRODUCT_FIELD_LIMITS</c> must match these values.
/// </summary>
public static class ProductConstraints
{
    public const int NameMaxLength = 200;
    public const int SkuMaxLength = 50;
    public const int CategoryMaxLength = 80;
    public const int NotesMaxLength = 400;
    public const int MaxStock = 99_999_999;
    public const decimal MaxPrice = 999_999_999.99m;
}
