using VortexProgramming.Core.Enums;
using VortexProgramming.Core.Models;

namespace VortexProgramming.Core.Events;

/// <summary>
/// Event emitted when a Vortex process starts execution
/// </summary>
public record ProcessStartedEvent : VortexEvent
{
    /// <summary>
    /// Name of the process that started
    /// </summary>
    public required string ProcessName { get; init; }

    /// <summary>
    /// Unique identifier for the process instance
    /// </summary>
    public required Guid ProcessInstanceId { get; init; }

    /// <summary>
    /// The scale at which the process is running
    /// </summary>
    public required VortexScale Scale { get; init; }

    /// <summary>
    /// The environment in which the process is running
    /// </summary>
    public required VortexEnvironment Environment { get; init; }

    /// <summary>
    /// Expected duration of the process (if known)
    /// </summary>
    public TimeSpan? ExpectedDuration { get; init; }

    /// <summary>
    /// Number of parallel instances or workers
    /// </summary>
    public int ParallelInstances { get; init; } = 1;
}

/// <summary>
/// Event emitted when a Vortex process completes successfully
/// </summary>
public record ProcessCompletedEvent : VortexEvent
{
    /// <summary>
    /// Name of the process that completed
    /// </summary>
    public required string ProcessName { get; init; }

    /// <summary>
    /// Unique identifier for the process instance
    /// </summary>
    public required Guid ProcessInstanceId { get; init; }

    /// <summary>
    /// Actual duration of the process execution
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Number of items processed (if applicable)
    /// </summary>
    public long ItemsProcessed { get; init; }

    /// <summary>
    /// Process execution results or output data
    /// </summary>
    public Dictionary<string, object> Results { get; init; } = new();
}

/// <summary>
/// Event emitted when a Vortex process fails
/// </summary>
public record ProcessFailedEvent : VortexEvent
{
    /// <summary>
    /// Name of the process that failed
    /// </summary>
    public required string ProcessName { get; init; }

    /// <summary>
    /// Unique identifier for the process instance
    /// </summary>
    public required Guid ProcessInstanceId { get; init; }

    /// <summary>
    /// The error that caused the failure
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Exception details if available
    /// </summary>
    public string? ExceptionDetails { get; init; }

    /// <summary>
    /// Duration before failure occurred
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Whether the process can be retried
    /// </summary>
    public bool CanRetry { get; init; } = true;

    /// <summary>
    /// Number of retry attempts made
    /// </summary>
    public int RetryCount { get; init; }
} 