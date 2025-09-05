namespace VortexProgramming.Core.Enums;

/// <summary>
/// Represents the execution environment for Vortex processes
/// </summary>
public enum VortexEnvironment
{
    /// <summary>
    /// Development environment - optimized for debugging and rapid iteration
    /// </summary>
    Development,

    /// <summary>
    /// Testing environment - optimized for validation and quality assurance
    /// </summary>
    Testing,

    /// <summary>
    /// Staging environment - production-like environment for final validation
    /// </summary>
    Staging,

    /// <summary>
    /// Production environment - optimized for performance and reliability
    /// </summary>
    Production
} 