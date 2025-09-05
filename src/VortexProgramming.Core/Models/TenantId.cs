using System.Diagnostics.CodeAnalysis;

namespace VortexProgramming.Core.Models;

/// <summary>
/// Represents a tenant identifier in a multi-tenant system
/// </summary>
public readonly record struct TenantId
{
    /// <summary>
    /// The unique identifier for the tenant
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the TenantId struct
    /// </summary>
    /// <param name="value">The tenant identifier value</param>
    /// <exception cref="ArgumentException">Thrown when value is null or whitespace</exception>
    public TenantId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        Value = value;
    }

    /// <summary>
    /// Default tenant for single-tenant scenarios
    /// </summary>
    public static TenantId Default => new("default");

    /// <summary>
    /// System tenant for internal operations
    /// </summary>
    public static TenantId System => new("system");

    /// <summary>
    /// Implicitly converts a string to a TenantId
    /// </summary>
    /// <param name="value">The string value</param>
    public static implicit operator TenantId(string value) => new(value);

    /// <summary>
    /// Implicitly converts a TenantId to a string
    /// </summary>
    /// <param name="tenantId">The TenantId</param>
    public static implicit operator string(TenantId tenantId) => tenantId.Value;

    /// <summary>
    /// Returns the string representation of the tenant ID
    /// </summary>
    /// <returns>The tenant ID value</returns>
    public override string ToString() => Value;

    /// <summary>
    /// Tries to parse a string into a TenantId
    /// </summary>
    /// <param name="value">The string to parse</param>
    /// <param name="tenantId">The resulting TenantId if successful</param>
    /// <returns>True if parsing was successful, false otherwise</returns>
    public static bool TryParse(string? value, [NotNullWhen(true)] out TenantId tenantId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            tenantId = default;
            return false;
        }

        tenantId = new TenantId(value);
        return true;
    }
} 