using System.Text.Json;
using VortexProgramming.Core.Models;

namespace VortexProgramming.Core.Events;

/// <summary>
/// Base class for all Vortex events in the event-driven architecture
/// </summary>
public abstract record VortexEvent
{
    /// <summary>
    /// Unique identifier for this event instance
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Timestamp when the event was created
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The tenant that triggered this event
    /// </summary>
    public TenantId TenantId { get; init; } = TenantId.Default;

    /// <summary>
    /// Source process or component that emitted this event
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// Optional correlation ID for tracking related events
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Event-specific metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Serializes the event to JSON
    /// </summary>
    /// <returns>JSON representation of the event</returns>
    public virtual string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Creates a copy of the event with updated metadata
    /// </summary>
    /// <param name="additionalMetadata">Additional metadata to include</param>
    /// <returns>New event instance with updated metadata</returns>
    public virtual VortexEvent WithMetadata(Dictionary<string, object> additionalMetadata)
    {
        var newMetadata = new Dictionary<string, object>(Metadata);
        foreach (var kvp in additionalMetadata)
        {
            newMetadata[kvp.Key] = kvp.Value;
        }
        
        return this with { Metadata = newMetadata };
    }
} 