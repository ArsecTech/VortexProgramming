using System.Text.Json;
using VortexProgramming.Core.Context;
using VortexProgramming.Core.Enums;
using VortexProgramming.Core.Models;
using VortexProgramming.Core.Processing;
using VortexProgramming.Extensions.FluentApi;

namespace VortexProgramming.Extensions.Dsl;

/// <summary>
/// Mini DSL for defining Vortex processes declaratively
/// </summary>
public static class VortexDsl
{
    /// <summary>
    /// Parses a process definition from JSON and creates a VortexProcess
    /// </summary>
    /// <param name="processDefinitionJson">JSON process definition</param>
    /// <returns>Configured VortexProcess</returns>
    public static VortexProcess<object, object> FromJson(string processDefinitionJson)
    {
        var definition = JsonSerializer.Deserialize<ProcessDefinition>(processDefinitionJson)
            ?? throw new ArgumentException("Invalid process definition JSON");

        return FromDefinition(definition);
    }

    /// <summary>
    /// Creates a VortexProcess from a process definition
    /// </summary>
    /// <param name="definition">Process definition</param>
    /// <returns>Configured VortexProcess</returns>
    public static VortexProcess<object, object> FromDefinition(ProcessDefinition definition)
    {
        var builder = new VortexBuilder();
        var processBuilder = builder.CreateProcess<object, object>(definition.Name);

        // Add metadata
        foreach (var metadata in definition.Metadata)
        {
            processBuilder.WithMetadata(metadata.Key, metadata.Value);
        }

        // Add conditions
        foreach (var condition in definition.Conditions)
        {
            processBuilder.When(CreateConditionFromDefinition(condition));
        }

        // Set execution logic based on type
        switch (definition.Type.ToLowerInvariant())
        {
            case "transform":
                processBuilder.Execute(CreateTransformFromDefinition(definition));
                break;
            case "batch":
                processBuilder.Execute(CreateBatchFromDefinition(definition));
                break;
            case "custom":
                processBuilder.Execute(CreateCustomFromDefinition(definition));
                break;
            default:
                throw new ArgumentException($"Unknown process type: {definition.Type}");
        }

        return processBuilder.Build();
    }

    /// <summary>
    /// Creates a process chain from a chain definition
    /// </summary>
    /// <param name="chainDefinition">Chain definition</param>
    /// <returns>Configured process chain</returns>
    public static VortexProcessChain FromChainDefinition(ChainDefinition chainDefinition)
    {
        var builder = new VortexBuilder();
        var chain = builder.CreateChain(chainDefinition.Name);

        foreach (var stepDefinition in chainDefinition.Steps)
        {
            var process = FromDefinition(stepDefinition.Process);
            chain.AddStep(process, stepDefinition.Name);
        }

        return chain;
    }

    /// <summary>
    /// Creates a context from a context definition
    /// </summary>
    /// <param name="contextDefinition">Context definition</param>
    /// <returns>Configured VortexContext</returns>
    public static VortexContext FromContextDefinition(ContextDefinition contextDefinition)
    {
        var environment = Enum.Parse<VortexEnvironment>(contextDefinition.Environment, true);
        var scale = Enum.Parse<VortexScale>(contextDefinition.Scale, true);
        var tenantId = new TenantId(contextDefinition.TenantId);

        var context = new VortexContext(tenantId, environment, scale)
        {
            UserId = contextDefinition.UserId,
            CorrelationId = contextDefinition.CorrelationId
        };

        foreach (var property in contextDefinition.Properties)
        {
            context.SetProperty(property.Key, property.Value);
        }

        return context;
    }

    private static Func<object, VortexContext, bool> CreateConditionFromDefinition(ConditionDefinition condition)
    {
        return condition.Type.ToLowerInvariant() switch
        {
            "always" => (_, _) => true,
            "never" => (_, _) => false,
            "property" => (input, context) => EvaluatePropertyCondition(input, context, condition),
            "scale" => (_, context) => EvaluateScaleCondition(context, condition),
            "environment" => (_, context) => EvaluateEnvironmentCondition(context, condition),
            _ => throw new ArgumentException($"Unknown condition type: {condition.Type}")
        };
    }

    private static bool EvaluatePropertyCondition(object input, VortexContext context, ConditionDefinition condition)
    {
        var propertyName = condition.Parameters.GetValueOrDefault("property", "").ToString();
        var expectedValue = condition.Parameters.GetValueOrDefault("value");

        if (string.IsNullOrEmpty(propertyName))
            return false;

        var actualValue = context.GetProperty<object>(propertyName!);
        return Equals(actualValue, expectedValue);
    }

    private static bool EvaluateScaleCondition(VortexContext context, ConditionDefinition condition)
    {
        var expectedScale = condition.Parameters.GetValueOrDefault("scale", "").ToString();
        if (Enum.TryParse<VortexScale>(expectedScale, true, out var scale))
        {
            return context.Scale == scale;
        }
        return false;
    }

    private static bool EvaluateEnvironmentCondition(VortexContext context, ConditionDefinition condition)
    {
        var expectedEnvironment = condition.Parameters.GetValueOrDefault("environment", "").ToString();
        if (Enum.TryParse<VortexEnvironment>(expectedEnvironment, true, out var environment))
        {
            return context.Environment == environment;
        }
        return false;
    }

    private static Func<object, CancellationToken, Task<object>> CreateTransformFromDefinition(ProcessDefinition definition)
    {
        return async (input, cancellationToken) =>
        {
            await Task.Delay(100, cancellationToken); // Simulate work
            
            var transformType = definition.Parameters.GetValueOrDefault("transformType", "passthrough").ToString();
            
            return transformType?.ToLowerInvariant() switch
            {
                "passthrough" => input,
                "uppercase" => input.ToString()?.ToUpperInvariant() ?? input,
                "lowercase" => input.ToString()?.ToLowerInvariant() ?? input,
                "reverse" => new string(input.ToString()?.Reverse().ToArray() ?? Array.Empty<char>()),
                _ => input
            };
        };
    }

    private static Func<object, CancellationToken, Task<object>> CreateBatchFromDefinition(ProcessDefinition definition)
    {
        return async (input, cancellationToken) =>
        {
            if (input is not IEnumerable<object> items)
            {
                return input;
            }

            var batchSize = Convert.ToInt32(definition.Parameters.GetValueOrDefault("batchSize", 10));
            var results = new List<object>();

            var batch = new List<object>();
            foreach (var item in items)
            {
                batch.Add(item);
                
                if (batch.Count >= batchSize)
                {
                    await Task.Delay(50, cancellationToken); // Simulate batch processing
                    results.AddRange(batch);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await Task.Delay(50, cancellationToken);
                results.AddRange(batch);
            }

            return results;
        };
    }

    private static Func<object, CancellationToken, Task<object>> CreateCustomFromDefinition(ProcessDefinition definition)
    {
        return async (input, cancellationToken) =>
        {
            var delay = Convert.ToInt32(definition.Parameters.GetValueOrDefault("delayMs", 100));
            await Task.Delay(delay, cancellationToken);
            
            var output = definition.Parameters.GetValueOrDefault("output", input);
            return output;
        };
    }
}

/// <summary>
/// Definition for a Vortex process
/// </summary>
public record ProcessDefinition
{
    /// <summary>
    /// Name of the process
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Type of process (transform, batch, custom)
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Process parameters
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();

    /// <summary>
    /// Process metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Conditions for process execution
    /// </summary>
    public List<ConditionDefinition> Conditions { get; init; } = new();
}

/// <summary>
/// Definition for a process condition
/// </summary>
public record ConditionDefinition
{
    /// <summary>
    /// Type of condition
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Condition parameters
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();
}

/// <summary>
/// Definition for a process chain
/// </summary>
public record ChainDefinition
{
    /// <summary>
    /// Name of the chain
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Steps in the chain
    /// </summary>
    public List<StepDefinition> Steps { get; init; } = new();
}

/// <summary>
/// Definition for a chain step
/// </summary>
public record StepDefinition
{
    /// <summary>
    /// Name of the step
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Process definition for this step
    /// </summary>
    public required ProcessDefinition Process { get; init; }
}

/// <summary>
/// Definition for a Vortex context
/// </summary>
public record ContextDefinition
{
    /// <summary>
    /// Tenant identifier
    /// </summary>
    public required string TenantId { get; init; }

    /// <summary>
    /// Environment name
    /// </summary>
    public required string Environment { get; init; }

    /// <summary>
    /// Scale name
    /// </summary>
    public required string Scale { get; init; }

    /// <summary>
    /// User identifier
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Correlation identifier
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Context properties
    /// </summary>
    public Dictionary<string, object> Properties { get; init; } = new();
} 