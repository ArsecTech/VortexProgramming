namespace VortexProgramming.Core.Enums;

/// <summary>
/// Represents the scale of execution for Vortex processes
/// </summary>
public enum VortexScale
{
    /// <summary>
    /// Small scale - single instance, minimal resources
    /// </summary>
    Small,

    /// <summary>
    /// Medium scale - moderate parallelization and resource usage
    /// </summary>
    Medium,

    /// <summary>
    /// Large scale - high parallelization, distributed execution
    /// </summary>
    Large,

    /// <summary>
    /// Auto scale - framework determines optimal scale based on context
    /// </summary>
    Auto
} 