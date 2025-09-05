using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using VortexProgramming.Core.Enums;
using VortexProgramming.Core.Events;
using VortexProgramming.Core.Models;

namespace VortexProgramming.Core.Context;

/// <summary>
/// Provides context-aware execution environment for Vortex processes
/// </summary>
public class VortexContext : IDisposable
{
    private readonly Subject<VortexEvent> _eventStream = new();
    private readonly ConcurrentDictionary<string, object> _properties = new();
    private readonly ILogger<VortexContext> _logger;
    private bool _disposed;

    /// <summary>
    /// Unique identifier for this context instance
    /// </summary>
    public Guid ContextId { get; } = Guid.NewGuid();

    /// <summary>
    /// The tenant this context belongs to
    /// </summary>
    public TenantId TenantId { get; }

    /// <summary>
    /// The execution environment
    /// </summary>
    public VortexEnvironment Environment { get; }

    /// <summary>
    /// The target execution scale
    /// </summary>
    public VortexScale Scale { get; private set; }

    /// <summary>
    /// When this context was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Observable stream of events emitted in this context
    /// </summary>
    public IObservable<VortexEvent> EventStream => _eventStream.AsObservable();

    /// <summary>
    /// Configuration properties for this context
    /// </summary>
    public IReadOnlyDictionary<string, object> Properties => _properties;

    /// <summary>
    /// Current user or system identifier
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Optional correlation ID for tracking related operations
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Initializes a new instance of the VortexContext
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="environment">The execution environment</param>
    /// <param name="scale">The target execution scale</param>
    /// <param name="logger">Logger instance</param>
    public VortexContext(
        TenantId tenantId,
        VortexEnvironment environment = VortexEnvironment.Development,
        VortexScale scale = VortexScale.Auto,
        ILogger<VortexContext>? logger = null)
    {
        TenantId = tenantId;
        Environment = environment;
        Scale = scale;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<VortexContext>.Instance;

        // Auto-determine scale based on environment if set to Auto
        if (Scale == VortexScale.Auto)
        {
            Scale = DetermineOptimalScale();
        }

        _logger.LogInformation("VortexContext created: {ContextId} for tenant {TenantId} in {Environment} at {Scale} scale",
            ContextId, TenantId, Environment, Scale);
    }

    /// <summary>
    /// Creates a context optimized for development work
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="logger">Optional logger instance</param>
    /// <returns>Development-optimized context</returns>
    public static VortexContext ForDevelopment(TenantId tenantId, ILogger<VortexContext>? logger = null)
    {
        var context = new VortexContext(tenantId, VortexEnvironment.Development, VortexScale.Small, logger);
        context.SetProperty("debug.enabled", true);
        context.SetProperty("logging.level", "Debug");
        context.SetProperty("performance.tracking", true);
        return context;
    }

    /// <summary>
    /// Creates a context optimized for production workloads
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="scale">The execution scale</param>
    /// <param name="logger">Optional logger instance</param>
    /// <returns>Production-optimized context</returns>
    public static VortexContext ForProduction(TenantId tenantId, VortexScale scale = VortexScale.Auto, ILogger<VortexContext>? logger = null)
    {
        var context = new VortexContext(tenantId, VortexEnvironment.Production, scale, logger);
        context.SetProperty("debug.enabled", false);
        context.SetProperty("logging.level", "Information");
        context.SetProperty("performance.tracking", true);
        context.SetProperty("monitoring.enabled", true);
        context.SetProperty("resilience.enabled", true);
        return context;
    }

    /// <summary>
    /// Sets a property value in the context
    /// </summary>
    /// <param name="key">Property key</param>
    /// <param name="value">Property value</param>
    /// <returns>The current context for method chaining</returns>
    public VortexContext SetProperty(string key, object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        _properties.AddOrUpdate(key, value, (_, _) => value);
        return this;
    }

    /// <summary>
    /// Gets a property value from the context
    /// </summary>
    /// <typeparam name="T">Expected type of the property</typeparam>
    /// <param name="key">Property key</param>
    /// <param name="defaultValue">Default value if property doesn't exist</param>
    /// <returns>Property value or default</returns>
    public T GetProperty<T>(string key, T defaultValue = default!)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        
        if (_properties.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }

        return defaultValue;
    }

    /// <summary>
    /// Emits an event in this context
    /// </summary>
    /// <param name="vortexEvent">The event to emit</param>
    public void EmitEvent(VortexEvent vortexEvent)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        var enrichedEvent = vortexEvent with 
        { 
            TenantId = TenantId,
            CorrelationId = CorrelationId ?? vortexEvent.CorrelationId
        };

        _eventStream.OnNext(enrichedEvent);
        _logger.LogDebug("Event emitted: {EventType} with ID {EventId}", 
            vortexEvent.GetType().Name, vortexEvent.EventId);
    }

    /// <summary>
    /// Creates a child context with the same configuration
    /// </summary>
    /// <param name="childCorrelationId">Optional correlation ID for the child context</param>
    /// <returns>New child context</returns>
    public VortexContext CreateChildContext(string? childCorrelationId = null)
    {
        var childContext = new VortexContext(TenantId, Environment, Scale, _logger)
        {
            UserId = UserId,
            CorrelationId = childCorrelationId ?? Guid.NewGuid().ToString()
        };

        // Copy properties to child context
        foreach (var property in _properties)
        {
            childContext.SetProperty(property.Key, property.Value);
        }

        return childContext;
    }

    /// <summary>
    /// Updates the execution scale dynamically
    /// </summary>
    /// <param name="newScale">The new execution scale</param>
    public void UpdateScale(VortexScale newScale)
    {
        if (Scale != newScale)
        {
            var oldScale = Scale;
            Scale = newScale;
            
            _logger.LogInformation("Context scale updated from {OldScale} to {NewScale} for context {ContextId}",
                oldScale, newScale, ContextId);

            EmitEvent(new ContextScaleChangedEvent
            {
                ContextId = ContextId,
                OldScale = oldScale,
                NewScale = newScale,
                Source = nameof(VortexContext)
            });
        }
    }

    /// <summary>
    /// Determines if the context should use parallel execution
    /// </summary>
    /// <returns>True if parallel execution should be used</returns>
    public bool ShouldUseParallelExecution()
    {
        return Scale is VortexScale.Medium or VortexScale.Large;
    }

    /// <summary>
    /// Gets the recommended degree of parallelism for the current scale
    /// </summary>
    /// <returns>Recommended parallel degree</returns>
    public int GetRecommendedParallelism()
    {
        return Scale switch
        {
            VortexScale.Small => 1,
            VortexScale.Medium => Math.Max(2, System.Environment.ProcessorCount / 2),
            VortexScale.Large => System.Environment.ProcessorCount * 2,
            _ => 1
        };
    }

    /// <summary>
    /// Determines if distributed execution should be used
    /// </summary>
    /// <returns>True if distributed execution should be used</returns>
    public bool ShouldUseDistributedExecution()
    {
        return Scale == VortexScale.Large && Environment == VortexEnvironment.Production;
    }

    private VortexScale DetermineOptimalScale()
    {
        // Simple heuristics for auto-scaling
        return Environment switch
        {
            VortexEnvironment.Development => VortexScale.Small,
            VortexEnvironment.Testing => VortexScale.Small,
            VortexEnvironment.Staging => VortexScale.Medium,
            VortexEnvironment.Production => VortexScale.Large,
            _ => VortexScale.Small
        };
    }

    /// <summary>
    /// Disposes the context and completes the event stream
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _eventStream.OnCompleted();
            _eventStream.Dispose();
            _disposed = true;
            
            _logger.LogInformation("VortexContext disposed: {ContextId}", ContextId);
        }
    }
}

/// <summary>
/// Event emitted when context scale changes
/// </summary>
public record ContextScaleChangedEvent : VortexEvent
{
    /// <summary>
    /// The context that changed scale
    /// </summary>
    public required Guid ContextId { get; init; }

    /// <summary>
    /// The previous scale
    /// </summary>
    public required VortexScale OldScale { get; init; }

    /// <summary>
    /// The new scale
    /// </summary>
    public required VortexScale NewScale { get; init; }
} 