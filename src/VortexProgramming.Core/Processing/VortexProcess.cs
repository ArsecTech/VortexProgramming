using Microsoft.Extensions.Logging;
using System.Diagnostics;
using VortexProgramming.Core.Context;
using VortexProgramming.Core.Enums;
using VortexProgramming.Core.Events;

namespace VortexProgramming.Core.Processing;

/// <summary>
/// Abstract base class for all Vortex processes with context-aware and self-scaling capabilities
/// </summary>
/// <typeparam name="TInput">Type of input data</typeparam>
/// <typeparam name="TOutput">Type of output data</typeparam>
public abstract class VortexProcess<TInput, TOutput> : IDisposable
{
    private readonly ILogger _logger;
    private ProcessStatus _status = ProcessStatus.Created;
    private readonly Stopwatch _stopwatch = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;

    /// <summary>
    /// Unique identifier for this process instance
    /// </summary>
    public Guid ProcessInstanceId { get; } = Guid.NewGuid();

    /// <summary>
    /// Name of the process
    /// </summary>
    public abstract string ProcessName { get; }

    /// <summary>
    /// The execution context for this process
    /// </summary>
    protected VortexContext Context { get; private set; } = null!;

    /// <summary>
    /// Current status of the process
    /// </summary>
    public ProcessStatus Status 
    { 
        get => _status;
        private set
        {
            if (_status != value)
            {
                _status = value;
                _logger.LogDebug("Process {ProcessName} ({InstanceId}) status changed to {Status}",
                    ProcessName, ProcessInstanceId, value);
            }
        }
    }

    /// <summary>
    /// Elapsed execution time
    /// </summary>
    public TimeSpan ElapsedTime => _stopwatch.Elapsed;

    /// <summary>
    /// Number of items processed
    /// </summary>
    public long ItemsProcessed { get; protected set; }

    /// <summary>
    /// Process execution results
    /// </summary>
    public Dictionary<string, object> ExecutionMetrics { get; } = new();

    /// <summary>
    /// Initializes a new instance of the VortexProcess
    /// </summary>
    /// <param name="logger">Logger instance</param>
    protected VortexProcess(ILogger? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>
    /// Executes the process with the given context and input
    /// </summary>
    /// <param name="context">Execution context</param>
    /// <param name="input">Input data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Process output</returns>
    public async Task<TOutput> ExecuteAsync(VortexContext context, TInput input, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        Context = context;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            await InitializeAsync(_cancellationTokenSource.Token);
            
            Status = ProcessStatus.Running;
            _stopwatch.Start();

            EmitProcessStartedEvent();

            var result = await ExecuteInternalAsync(input, _cancellationTokenSource.Token);
            
            Status = ProcessStatus.Completed;
            _stopwatch.Stop();

            EmitProcessCompletedEvent();

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Status = ProcessStatus.Cancelled;
            _stopwatch.Stop();
            _logger.LogWarning("Process {ProcessName} ({InstanceId}) was cancelled", ProcessName, ProcessInstanceId);
            throw;
        }
        catch (Exception ex)
        {
            Status = ProcessStatus.Failed;
            _stopwatch.Stop();

            EmitProcessFailedEvent(ex);
            
            _logger.LogError(ex, "Process {ProcessName} ({InstanceId}) failed: {Error}", 
                ProcessName, ProcessInstanceId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Initializes the process before execution
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    protected virtual Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Status = ProcessStatus.Initializing;
        
        // Set up execution metrics based on context
        ExecutionMetrics["scale"] = Context.Scale;
        ExecutionMetrics["environment"] = Context.Environment;
        ExecutionMetrics["tenantId"] = Context.TenantId.Value;
        ExecutionMetrics["parallelism"] = Context.GetRecommendedParallelism();
        
        _logger.LogInformation("Initializing process {ProcessName} ({InstanceId}) in {Environment} at {Scale} scale",
            ProcessName, ProcessInstanceId, Context.Environment, Context.Scale);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Core execution logic to be implemented by derived classes
    /// </summary>
    /// <param name="input">Input data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Process output</returns>
    protected abstract Task<TOutput> ExecuteInternalAsync(TInput input, CancellationToken cancellationToken);

    /// <summary>
    /// Determines the optimal execution strategy based on context
    /// </summary>
    /// <returns>Execution strategy</returns>
    protected virtual ExecutionStrategy DetermineExecutionStrategy()
    {
        return Context.Scale switch
        {
            VortexScale.Small => ExecutionStrategy.Sequential,
            VortexScale.Medium => ExecutionStrategy.Parallel,
            VortexScale.Large when Context.ShouldUseDistributedExecution() => ExecutionStrategy.Distributed,
            VortexScale.Large => ExecutionStrategy.Parallel,
            _ => ExecutionStrategy.Sequential
        };
    }

    /// <summary>
    /// Creates a parallel execution options based on context
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parallel options</returns>
    protected ParallelOptions CreateParallelOptions(CancellationToken cancellationToken)
    {
        return new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Context.GetRecommendedParallelism()
        };
    }

    /// <summary>
    /// Processes a collection of items with context-aware scaling
    /// </summary>
    /// <typeparam name="TItem">Type of items to process</typeparam>
    /// <param name="items">Items to process</param>
    /// <param name="processor">Processing function</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected async Task ProcessItemsAsync<TItem>(
        IEnumerable<TItem> items,
        Func<TItem, CancellationToken, Task> processor,
        CancellationToken cancellationToken = default)
    {
        var itemList = items.ToList();
        var strategy = DetermineExecutionStrategy();

        _logger.LogDebug("Processing {ItemCount} items using {Strategy} strategy", 
            itemList.Count, strategy);

        switch (strategy)
        {
            case ExecutionStrategy.Sequential:
                await ProcessSequentiallyAsync(itemList, processor, cancellationToken);
                break;
            
            case ExecutionStrategy.Parallel:
                await ProcessInParallelAsync(itemList, processor, cancellationToken);
                break;
            
            case ExecutionStrategy.Distributed:
                await ProcessDistributedAsync(itemList, processor, cancellationToken);
                break;
        }
    }

    private async Task ProcessSequentiallyAsync<TItem>(
        IList<TItem> items,
        Func<TItem, CancellationToken, Task> processor,
        CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await processor(item, cancellationToken);
            ItemsProcessed++;
        }
    }

    private async Task ProcessInParallelAsync<TItem>(
        IList<TItem> items,
        Func<TItem, CancellationToken, Task> processor,
        CancellationToken cancellationToken)
    {
        var options = CreateParallelOptions(cancellationToken);
        long itemsProcessed = 0;
        
        await Parallel.ForEachAsync(items, options, async (item, ct) =>
        {
            await processor(item, ct);
            Interlocked.Increment(ref itemsProcessed);
        });

        ItemsProcessed += itemsProcessed;
    }

    private async Task ProcessDistributedAsync<TItem>(
        IList<TItem> items,
        Func<TItem, CancellationToken, Task> processor,
        CancellationToken cancellationToken)
    {
        // For now, fall back to parallel processing
        // In a real implementation, this would distribute work across multiple nodes
        _logger.LogInformation("Distributed processing not yet implemented, falling back to parallel processing");
        await ProcessInParallelAsync(items, processor, cancellationToken);
    }

    /// <summary>
    /// Updates process progress and metrics
    /// </summary>
    /// <param name="itemsProcessed">Number of items processed</param>
    /// <param name="additionalMetrics">Additional metrics to record</param>
    protected void UpdateProgress(long itemsProcessed, Dictionary<string, object>? additionalMetrics = null)
    {
        ItemsProcessed = itemsProcessed;
        
        if (additionalMetrics != null)
        {
            foreach (var metric in additionalMetrics)
            {
                ExecutionMetrics[metric.Key] = metric.Value;
            }
        }

        ExecutionMetrics["itemsProcessed"] = ItemsProcessed;
        ExecutionMetrics["elapsedTime"] = ElapsedTime;
        ExecutionMetrics["itemsPerSecond"] = ElapsedTime.TotalSeconds > 0 
            ? ItemsProcessed / ElapsedTime.TotalSeconds 
            : 0;
    }

    private void EmitProcessStartedEvent()
    {
        Context.EmitEvent(new ProcessStartedEvent
        {
            ProcessName = ProcessName,
            ProcessInstanceId = ProcessInstanceId,
            Scale = Context.Scale,
            Environment = Context.Environment,
            ParallelInstances = Context.GetRecommendedParallelism(),
            Source = ProcessName
        });
    }

    private void EmitProcessCompletedEvent()
    {
        Context.EmitEvent(new ProcessCompletedEvent
        {
            ProcessName = ProcessName,
            ProcessInstanceId = ProcessInstanceId,
            Duration = ElapsedTime,
            ItemsProcessed = ItemsProcessed,
            Results = new Dictionary<string, object>(ExecutionMetrics),
            Source = ProcessName
        });
    }

    private void EmitProcessFailedEvent(Exception exception)
    {
        Context.EmitEvent(new ProcessFailedEvent
        {
            ProcessName = ProcessName,
            ProcessInstanceId = ProcessInstanceId,
            ErrorMessage = exception.Message,
            ExceptionDetails = exception.ToString(),
            Duration = ElapsedTime,
            Source = ProcessName
        });
    }

    /// <summary>
    /// Disposes the process and releases resources
    /// </summary>
    public virtual void Dispose()
    {
        if (!_disposed)
        {
            _cancellationTokenSource?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Execution strategy for processing
/// </summary>
public enum ExecutionStrategy
{
    /// <summary>
    /// Process items one at a time
    /// </summary>
    Sequential,

    /// <summary>
    /// Process items in parallel on the same machine
    /// </summary>
    Parallel,

    /// <summary>
    /// Distribute processing across multiple machines
    /// </summary>
    Distributed
} 