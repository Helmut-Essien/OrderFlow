using Serilog.Core;
using Serilog.Events;

namespace OrderFlow.Api.Logging;

/// <summary>
/// Serilog destructuring policy that replaces license keys, passwords, and integration keys with <c>***REDACTED***</c>.
/// Never log plaintext secrets even in Development.
/// </summary>
public sealed class SecretRedactingPolicy : IDestructuringPolicy
{
    private static readonly HashSet<string> SensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "LicenseKey",
        "Password",
        "ConfirmPassword",
        "IntegrationKey",
        "ProtectedLicenseKey"
    };

    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue result)
    {
        var type = value.GetType();
        if (type.IsPrimitive || type.IsEnum || value is string or decimal or DateTime or DateTimeOffset)
        {
            result = null!;
            return false;
        }

        var properties = type.GetProperties();
        if (properties.Length == 0 || properties.All(p => !SensitiveNames.Contains(p.Name)))
        {
            result = null!;
            return false;
        }

        var structure = new List<LogEventProperty>(properties.Length);
        foreach (var property in properties)
        {
            object? raw = SensitiveNames.Contains(property.Name)
                ? "***REDACTED***"
                : property.GetValue(value);

            structure.Add(new LogEventProperty(
                property.Name,
                propertyValueFactory.CreatePropertyValue(raw, destructureObjects: true)));
        }

        result = new StructureValue(structure, type.Name);
        return true;
    }
}
