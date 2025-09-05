using Microsoft.Extensions.Logging;
using VortexProgramming.Core.Context;
using VortexProgramming.Core.Processing;

namespace VortexProgramming.Extensions.FluentApi;

/// <summary>
/// Fluent API builder for creating Vortex processes without defining new classes
/// </summary>
public class VortexBuilder
{
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the VortexBuilder
    /// </summary>
    /// <param name="logger">Optional logger instance</param>
    public VortexBuilder(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a new fluent process builder
    /// </summary>
    /// <typeparam name="TInput">Input type</typeparam>
    /// <typeparam name="TOutput">Output type</typeparam>
    /// <param name="processName">Name of the process</param>
    /// <returns>Fluent process builder</returns>
    public FluentProcessBuilder<TInput, TOutput> CreateProcess<TInput, TOutput>(string processName)
    {
        return new FluentProcessBuilder<TInput, TOutput>(processName, _logger);
    }

    /// <summary>
    /// Creates a new process chain builder
    /// </summary>
    /// <param name="chainName">Name of the chain</param>
    /// <returns>Process chain</returns>
    public VortexProcessChain CreateChain(string chainName)
    {
        var chain = new VortexProcessChain(_logger as ILogger<VortexProcessChain>)
        {
            ChainName = chainName
        };
        return chain;
    }

    /// <summary>
    /// Creates a simple transformation process
    /// </summary>
    /// <typeparam name="TInput">Input type</typeparam>
    /// <typeparam name="TOutput">Output type</typeparam>
    /// <param name="processName">Process name</param>
    /// <param name="transform">Transformation function</param>
    /// <returns>Vortex process</returns>
    public VortexProcess<TInput, TOutput> Transform<TInput, TOutput>(
        string processName,
        Func<TInput, Task<TOutput>> transform)
    {
        return CreateProcess<TInput, TOutput>(processName)
            .Execute(transform)
            .Build();
    }

    /// <summary>
    /// Creates a simple transformation process with synchronous function
    /// </summary>
    /// <typeparam name="TInput">Input type</typeparam>
    /// <typeparam name="TOutput">Output type</typeparam>
    /// <param name="processName">Process name</param>
    /// <param name="transform">Transformation function</param>
    /// <returns>Vortex process</returns>
    public VortexProcess<TInput, TOutput> Transform<TInput, TOutput>(
        string processName,
        Func<TInput, TOutput> transform)
    {
        return CreateProcess<TInput, TOutput>(processName)
            .Execute((input, _) => Task.FromResult(transform(input)))
            .Build();
    }

    /// <summary>
    /// Creates a batch processing process
    /// </summary>
    /// <typeparam name="TItem">Item type</typeparam>
    /// <param name="processName">Process name</param>
    /// <param name="itemProcessor">Function to process individual items</param>
    /// <returns>Batch process</returns>
    public VortexProcess<IEnumerable<TItem>, IEnumerable<TItem>> Batch<TItem>(
        string processName,
        Func<TItem, Task<TItem>> itemProcessor)
    {
        return CreateProcess<IEnumerable<TItem>, IEnumerable<TItem>>(processName)
            .Execute(async (items, cancellationToken) =>
            {
                var results = new List<TItem>();
                foreach (var item in items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var result = await itemProcessor(item);
                    results.Add(result);
                }
                return results;
            })
            .Build();
    }
}

/// <summary>
/// Fluent builder for creating processes with method chaining
/// </summary>
/// <typeparam name="TInput">Input type</typeparam>
/// <typeparam name="TOutput">Output type</typeparam>
public class FluentProcessBuilder<TInput, TOutput>
{
    private readonly string _processName;
    private readonly ILogger? _logger;
    private Func<TInput, CancellationToken, Task<TOutput>>? _executeFunction;
    private Func<VortexContext, CancellationToken, Task>? _initializeFunction;
    private readonly List<Func<TInput, VortexContext, bool>> _conditions = new();
    private readonly Dictionary<string, object> _metadata = new();

    internal FluentProcessBuilder(string processName, ILogger? logger = null)
    {
        _processName = processName ?? throw new ArgumentNullException(nameof(processName));
        _logger = logger;
    }

    /// <summary>
    /// Sets the main execution logic
    /// </summary>
    /// <param name="executeFunction">Execution function</param>
    /// <returns>Builder for method chaining</returns>
    public FluentProcessBuilder<TInput, TOutput> Execute(Func<TInput, CancellationToken, Task<TOutput>> executeFunction)
    {
        _executeFunction = executeFunction ?? throw new ArgumentNullException(nameof(executeFunction));
        return this;
    }

    /// <summary>
    /// Sets the main execution logic (async without cancellation token)
    /// </summary>
    /// <param name="executeFunction">Execution function</param>
    /// <returns>Builder for method chaining</returns>
    public FluentProcessBuilder<TInput, TOutput> Execute(Func<TInput, Task<TOutput>> executeFunction)
    {
        ArgumentNullException.ThrowIfNull(executeFunction, nameof(executeFunction));
        return Execute((input, _) => executeFunction(input));
    }

    /// <summary>
    /// Sets the main execution logic (synchronous)
    /// </summary>
    /// <param name="executeFunction">Execution function</param>
    /// <returns>Builder for method chaining</returns>
    public FluentProcessBuilder<TInput, TOutput> Execute(Func<TInput, TOutput> executeFunction)
    {
        ArgumentNullException.ThrowIfNull(executeFunction, nameof(executeFunction));
        return Execute((input, _) => Task.FromResult(executeFunction(input)));
    }

    /// <summary>
    /// Sets custom initialization logic
    /// </summary>
    /// <param name="initializeFunction">Initialization function</param>
    /// <returns>Builder for method chaining</returns>
    public FluentProcessBuilder<TInput, TOutput> Initialize(Func<VortexContext, CancellationToken, Task> initializeFunction)
    {
        _initializeFunction = initializeFunction ?? throw new ArgumentNullException(nameof(initializeFunction));
        return this;
    }

    /// <summary>
    /// Adds a condition that must be met for the process to execute
    /// </summary>
    /// <param name="condition">Condition function</param>
    /// <returns>Builder for method chaining</returns>
    public FluentProcessBuilder<TInput, TOutput> When(Func<TInput, VortexContext, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition, nameof(condition));
        _conditions.Add(condition);
        return this;
    }

    /// <summary>
    /// Adds a simple condition based on input
    /// </summary>
    /// <param name="condition">Condition function</param>
    /// <returns>Builder for method chaining</returns>
    public FluentProcessBuilder<TInput, TOutput> When(Func<TInput, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition, nameof(condition));
        return When((input, _) => condition(input));
    }

    /// <summary>
    /// Adds metadata to the process
    /// </summary>
    /// <param name="key">Metadata key</param>
    /// <param name="value">Metadata value</param>
    /// <returns>Builder for method chaining</returns>
    public FluentProcessBuilder<TInput, TOutput> WithMetadata(string key, object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Builds the final Vortex process
    /// </summary>
    /// <returns>Configured Vortex process</returns>
    public VortexProcess<TInput, TOutput> Build()
    {
        if (_executeFunction == null)
        {
            throw new InvalidOperationException("Execute function must be specified");
        }

        return new FluentVortexProcess<TInput, TOutput>(
            _processName,
            _executeFunction,
            _initializeFunction,
            _conditions,
            _metadata,
            _logger);
    }
}

/// <summary>
/// Implementation of VortexProcess created through the fluent API
/// </summary>
/// <typeparam name="TInput">Input type</typeparam>
/// <typeparam name="TOutput">Output type</typeparam>
internal class FluentVortexProcess<TInput, TOutput> : VortexProcess<TInput, TOutput>
{
    private readonly Func<TInput, CancellationToken, Task<TOutput>> _executeFunction;
    private readonly Func<VortexContext, CancellationToken, Task>? _initializeFunction;
    private readonly List<Func<TInput, VortexContext, bool>> _conditions;
    private readonly Dictionary<string, object> _metadata;

    public override string ProcessName { get; }

    public FluentVortexProcess(
        string processName,
        Func<TInput, CancellationToken, Task<TOutput>> executeFunction,
        Func<VortexContext, CancellationToken, Task>? initializeFunction,
        List<Func<TInput, VortexContext, bool>> conditions,
        Dictionary<string, object> metadata,
        ILogger? logger = null)
        : base(logger)
    {
        ProcessName = processName;
        _executeFunction = executeFunction;
        _initializeFunction = initializeFunction;
        _conditions = conditions;
        _metadata = metadata;
    }

    protected override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await base.InitializeAsync(cancellationToken);

        // Add metadata to execution metrics
        foreach (var kvp in _metadata)
        {
            ExecutionMetrics[kvp.Key] = kvp.Value;
        }

        if (_initializeFunction != null)
        {
            await _initializeFunction(Context, cancellationToken);
        }
    }

    protected override async Task<TOutput> ExecuteInternalAsync(TInput input, CancellationToken cancellationToken)
    {
        // Check all conditions
        foreach (var condition in _conditions)
        {
            if (!condition(input, Context))
            {
                throw new InvalidOperationException($"Process condition not met for {ProcessName}");
            }
        }

        return await _executeFunction(input, cancellationToken);
    }
} 