namespace VortexProgramming.Core.Enums;

/// <summary>
/// Represents the current status of a Vortex process
/// </summary>
public enum ProcessStatus
{
    /// <summary>
    /// Process has been created but not yet started
    /// </summary>
    Created,

    /// <summary>
    /// Process is currently initializing
    /// </summary>
    Initializing,

    /// <summary>
    /// Process is currently running
    /// </summary>
    Running,

    /// <summary>
    /// Process is paused and can be resumed
    /// </summary>
    Paused,

    /// <summary>
    /// Process completed successfully
    /// </summary>
    Completed,

    /// <summary>
    /// Process failed with an error
    /// </summary>
    Failed,

    /// <summary>
    /// Process was cancelled before completion
    /// </summary>
    Cancelled,

    /// <summary>
    /// Process timed out during execution
    /// </summary>
    TimedOut
} 