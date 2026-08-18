namespace OrderFlow.Domain;

/// <summary>
/// Stock movement field limits. Decoupled from <see cref="ProductConstraints"/> so changes to
/// product notes do not silently affect audit rows.
/// </summary>
public static class StockMovementConstraints
{
    /// <summary>Movement notes max length (FluentValidation, EF, Angular).</summary>
    public const int NotesMaxLength = 400;
}
