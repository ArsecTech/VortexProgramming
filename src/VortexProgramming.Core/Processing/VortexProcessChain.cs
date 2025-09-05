using Microsoft.Extensions.Logging;
using VortexProgramming.Core.Context;
using VortexProgramming.Core.Events;

namespace VortexProgramming.Core.Processing;

/// <summary>
/// Orchestrates the execution of multiple Vortex processes in a chain
/// </summary>
public class VortexProcessChain : IDisposable
{
    private readonly List<IProcessStep> _steps = new();
    private readonly ILogger<VortexProcessChain> _logger;
    private bool _disposed;

    /// <summary>
    /// Unique identifier for this process chain
    /// </summary>
    public Guid ChainId { get; } = Guid.NewGuid();

    /// <summary>
    /// Name of the process chain
    /// </summary>
    public string ChainName { get; set; } = "VortexProcessChain";

    /// <summary>
    /// Initializes a new instance of the VortexProcessChain
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public VortexProcessChain(ILogger<VortexProcessChain>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<VortexProcessChain>.Instance;
    }

    /// <summary>
    /// Adds a process step to the chain
    /// </summary>
    /// <typeparam name="TInput">Input type</typeparam>
    /// <typeparam name="TOutput">Output type</typeparam>
    /// <param name="process">The process to add</param>
    /// <param name="stepName">Optional step name</param>
    /// <returns>The chain for method chaining</returns>
    public VortexProcessChain AddStep<TInput, TOutput>(
        VortexProcess<TInput, TOutput> process,
        string? stepName = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        var step = new ProcessStep<TInput, TOutput>(process, stepName ?? process.ProcessName);
        _steps.Add(step);
        
        _logger.LogDebug("Added step '{StepName}' to chain '{ChainName}'", step.StepName, ChainName);
        return this;
    }

    /// <summary>
    /// Adds a transformation function as a step
    /// </summary>
    /// <typeparam name="TInput">Input type</typeparam>
    /// <typeparam name="TOutput">Output type</typeparam>
    /// <param name="transform">Transformation function</param>
    /// <param name="stepName">Step name</param>
    /// <returns>The chain for method chaining</returns>
    public VortexProcessChain AddTransform<TInput, TOutput>(
        Func<TInput, Task<TOutput>> transform,
        string stepName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        var transformProcess = new TransformProcess<TInput, TOutput>(transform, stepName);
        return AddStep(transformProcess);
    }

    /// <summary>
    /// Adds a conditional step that only executes if the condition is met
    /// </summary>
    /// <typeparam name="TInput">Input type</typeparam>
    /// <typeparam name="TOutput">Output type</typeparam>
    /// <param name="condition">Condition to evaluate</param>
    /// <param name="process">Process to execute if condition is true</param>
    /// <param name="stepName">Optional step name</param>
    /// <returns>The chain for method chaining</returns>
    public VortexProcessChain AddConditionalStep<TInput, TOutput>(
        Func<TInput, VortexContext, bool> condition,
        VortexProcess<TInput, TOutput> process,
        string? stepName = null)
        where TOutput : TInput
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        var conditionalProcess = new ConditionalProcess<TInput, TOutput>(condition, process);
        return AddStep(conditionalProcess, stepName ?? $"Conditional_{process.ProcessName}");
    }

    /// <summary>
    /// Executes the entire process chain
    /// </summary>
    /// <typeparam name="TInput">Initial input type</typeparam>
    /// <typeparam name="TOutput">Final output type</typeparam>
    /// <param name="context">Execution context</param>
    /// <param name="input">Initial input</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Final output</returns>
    public async Task<TOutput> ExecuteAsync<TInput, TOutput>(
        VortexContext context,
        TInput input,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        if (_steps.Count == 0)
        {
            throw new InvalidOperationException("Process chain is empty");
        }

        _logger.LogInformation("Starting execution of chain '{ChainName}' with {StepCount} steps", 
            ChainName, _steps.Count);

        context.EmitEvent(new ProcessChainStartedEvent
        {
            ChainId = ChainId,
            ChainName = ChainName,
            StepCount = _steps.Count,
            Source = nameof(VortexProcessChain)
        });

        object currentOutput = input!;
        var stepIndex = 0;

        try
        {
            foreach (var step in _steps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                _logger.LogDebug("Executing step {StepIndex}: '{StepName}'", stepIndex, step.StepName);
                
                currentOutput = await step.ExecuteAsync(context, currentOutput, cancellationToken);
                stepIndex++;
            }

            context.EmitEvent(new ProcessChainCompletedEvent
            {
                ChainId = ChainId,
                ChainName = ChainName,
                StepsCompleted = stepIndex,
                Source = nameof(VortexProcessChain)
            });

            _logger.LogInformation("Chain '{ChainName}' completed successfully", ChainName);
            
            return (TOutput)currentOutput;
        }
        catch (Exception ex)
        {
            context.EmitEvent(new ProcessChainFailedEvent
            {
                ChainId = ChainId,
                ChainName = ChainName,
                FailedAtStep = stepIndex,
                ErrorMessage = ex.Message,
                Source = nameof(VortexProcessChain)
            });

            _logger.LogError(ex, "Chain '{ChainName}' failed at step {StepIndex}", ChainName, stepIndex);
            throw;
        }
    }

    /// <summary>
    /// Gets information about all steps in the chain
    /// </summary>
    /// <returns>Step information</returns>
    public IReadOnlyList<ProcessStepInfo> GetStepInfo()
    {
        return _steps.Select((step, index) => new ProcessStepInfo
        {
            Index = index,
            StepName = step.StepName,
            InputType = step.InputType.Name,
            OutputType = step.OutputType.Name
        }).ToList();
    }

    /// <summary>
    /// Disposes the process chain and all its steps
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var step in _steps)
            {
                step.Dispose();
            }
            _steps.Clear();
            _disposed = true;
        }
    }
}

/// <summary>
/// Interface for process steps in a chain
/// </summary>
internal interface IProcessStep : IDisposable
{
    string StepName { get; }
    Type InputType { get; }
    Type OutputType { get; }
    Task<object> ExecuteAsync(VortexContext context, object input, CancellationToken cancellationToken);
}

/// <summary>
/// Concrete implementation of a process step
/// </summary>
/// <typeparam name="TInput">Input type</typeparam>
/// <typeparam name="TOutput">Output type</typeparam>
internal class ProcessStep<TInput, TOutput> : IProcessStep
{
    private readonly VortexProcess<TInput, TOutput> _process;

    public string StepName { get; }
    public Type InputType => typeof(TInput);
    public Type OutputType => typeof(TOutput);

    public ProcessStep(VortexProcess<TInput, TOutput> process, string stepName)
    {
        _process = process ?? throw new ArgumentNullException(nameof(process));
        StepName = stepName ?? throw new ArgumentNullException(nameof(stepName));
    }

    public async Task<object> ExecuteAsync(VortexContext context, object input, CancellationToken cancellationToken)
    {
        if (input is not TInput typedInput)
        {
            throw new ArgumentException($"Expected input of type {typeof(TInput).Name}, but got {input?.GetType().Name ?? "null"}");
        }

        return await _process.ExecuteAsync(context, typedInput, cancellationToken);
    }

    public void Dispose()
    {
        _process.Dispose();
    }
}

/// <summary>
/// A simple transformation process
/// </summary>
/// <typeparam name="TInput">Input type</typeparam>
/// <typeparam name="TOutput">Output type</typeparam>
internal class TransformProcess<TInput, TOutput> : VortexProcess<TInput, TOutput>
{
    private readonly Func<TInput, Task<TOutput>> _transform;

    public override string ProcessName { get; }

    public TransformProcess(Func<TInput, Task<TOutput>> transform, string processName)
    {
        _transform = transform ?? throw new ArgumentNullException(nameof(transform));
        ProcessName = processName ?? throw new ArgumentNullException(nameof(processName));
    }

    protected override Task<TOutput> ExecuteInternalAsync(TInput input, CancellationToken cancellationToken)
    {
        return _transform(input);
    }
}

/// <summary>
/// A conditional process that only executes if a condition is met
/// </summary>
/// <typeparam name="TInput">Input type</typeparam>
/// <typeparam name="TOutput">Output type</typeparam>
internal class ConditionalProcess<TInput, TOutput> : VortexProcess<TInput, TOutput>
    where TOutput : TInput
{
    private readonly Func<TInput, VortexContext, bool> _condition;
    private readonly VortexProcess<TInput, TOutput> _innerProcess;

    public override string ProcessName => $"Conditional_{_innerProcess.ProcessName}";

    public ConditionalProcess(
        Func<TInput, VortexContext, bool> condition,
        VortexProcess<TInput, TOutput> innerProcess)
    {
        _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        _innerProcess = innerProcess ?? throw new ArgumentNullException(nameof(innerProcess));
    }

    protected override async Task<TOutput> ExecuteInternalAsync(TInput input, CancellationToken cancellationToken)
    {
        if (_condition(input, Context))
        {
            return await _innerProcess.ExecuteAsync(Context, input, cancellationToken);
        }

        // If condition is false, return input as output (assuming TOutput : TInput)
        return (TOutput)(object)input;
    }

    public override void Dispose()
    {
        _innerProcess.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Information about a process step
/// </summary>
public record ProcessStepInfo
{
    /// <summary>
    /// Index of the step in the chain
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Name of the step
    /// </summary>
    public required string StepName { get; init; }

    /// <summary>
    /// Input type name
    /// </summary>
    public required string InputType { get; init; }

    /// <summary>
    /// Output type name
    /// </summary>
    public required string OutputType { get; init; }
}

/// <summary>
/// Event emitted when a process chain starts
/// </summary>
public record ProcessChainStartedEvent : VortexEvent
{
    public required Guid ChainId { get; init; }
    public required string ChainName { get; init; }
    public required int StepCount { get; init; }
}

/// <summary>
/// Event emitted when a process chain completes
/// </summary>
public record ProcessChainCompletedEvent : VortexEvent
{
    public required Guid ChainId { get; init; }
    public required string ChainName { get; init; }
    public required int StepsCompleted { get; init; }
}

/// <summary>
/// Event emitted when a process chain fails
/// </summary>
public record ProcessChainFailedEvent : VortexEvent
{
    public required Guid ChainId { get; init; }
    public required string ChainName { get; init; }
    public required int FailedAtStep { get; init; }
    public required string ErrorMessage { get; init; }
} 