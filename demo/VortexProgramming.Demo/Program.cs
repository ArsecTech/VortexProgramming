using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VortexProgramming.Core.Context;
using VortexProgramming.Core.Enums;
using VortexProgramming.Core.Events;
using VortexProgramming.Core.Examples;
using VortexProgramming.Core.Models;
using VortexProgramming.Core.Processing;
using VortexProgramming.Extensions.Dsl;
using VortexProgramming.Extensions.FluentApi;

namespace VortexProgramming.Demo;

/// <summary>
/// Vortex Programming Framework Demo Application
/// Demonstrates context-aware, self-scaling, and composable processes
/// </summary>
class Program
{
    private static ILogger<Program> _logger = null!;

    static async Task Main(string[] args)
    {
        Console.WriteLine("🌪️  Vortex Programming Framework Demo");
        Console.WriteLine("=====================================");
        Console.WriteLine();

        // Set up dependency injection and logging
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddLogging(builder => 
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            })
            .Build();

        _logger = host.Services.GetRequiredService<ILogger<Program>>();

        try
        {
            // Demo 1: Basic Context and Process Execution
            await DemoBasicContextAndProcesses();

            Console.WriteLine("\n" + new string('=', 50) + "\n");

            // Demo 2: Context-Aware Scaling
            await DemoContextAwareScaling();

            Console.WriteLine("\n" + new string('=', 50) + "\n");

            // Demo 3: Process Chaining
            await DemoProcessChaining();

            Console.WriteLine("\n" + new string('=', 50) + "\n");

            // Demo 4: Fluent API
            await DemoFluentApi();

            Console.WriteLine("\n" + new string('=', 50) + "\n");

            // Demo 5: DSL-Based Process Definition
            await DemoDslProcesses();

            Console.WriteLine("\n" + new string('=', 50) + "\n");

            // Demo 6: Enterprise Order Processing
            await DemoEnterpriseOrderProcessing();

            Console.WriteLine("\n" + new string('=', 50) + "\n");

            // Demo 7: Complex Data Pipeline
            await DemoDataPipeline();

            Console.WriteLine("\n✅ All demos completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo failed: {Error}", ex.Message);
            Console.WriteLine($"\n❌ Demo failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Demonstrates basic context creation and process execution
    /// </summary>
    static async Task DemoBasicContextAndProcesses()
    {
        Console.WriteLine("📋 Demo 1: Basic Context and Process Execution");
        Console.WriteLine("----------------------------------------------");

        // Create contexts for different environments
        var devContext = VortexContext.ForDevelopment("demo-tenant");
        var prodContext = VortexContext.ForProduction("demo-tenant", VortexScale.Medium);

        Console.WriteLine($"Development Context: {devContext.ContextId} (Scale: {devContext.Scale})");
        Console.WriteLine($"Production Context: {prodContext.ContextId} (Scale: {prodContext.Scale})");

        // Subscribe to events
        devContext.EventStream.Subscribe(evt => 
            Console.WriteLine($"🔔 Dev Event: {evt.GetType().Name} from {evt.Source}"));

        // Simple process using fluent API
        var builder = new VortexBuilder(_logger);
        var simpleProcess = builder.Transform<string, string>(
            "UppercaseTransform",
            input => input.ToUpperInvariant()
        );

        var result = await simpleProcess.ExecuteAsync(devContext, "hello vortex programming!");
        Console.WriteLine($"Result: {result}");

        devContext.Dispose();
        prodContext.Dispose();
    }

    /// <summary>
    /// Demonstrates how processes adapt to different scales and environments
    /// </summary>
    static async Task DemoContextAwareScaling()
    {
        Console.WriteLine("⚡ Demo 2: Context-Aware Scaling");
        Console.WriteLine("--------------------------------");

        var tenantId = new TenantId("scaling-demo");

        // Test different scales
        var scales = new[] { VortexScale.Small, VortexScale.Medium, VortexScale.Large };
        var environments = new[] { VortexEnvironment.Development, VortexEnvironment.Production };

        foreach (var env in environments)
        {
            foreach (var scale in scales)
            {
                Console.WriteLine($"\n🎯 Testing {env} environment at {scale} scale:");

                using var context = new VortexContext(tenantId, env, scale);
                
                Console.WriteLine($"  - Recommended Parallelism: {context.GetRecommendedParallelism()}");
                Console.WriteLine($"  - Use Parallel Execution: {context.ShouldUseParallelExecution()}");
                Console.WriteLine($"  - Use Distributed Execution: {context.ShouldUseDistributedExecution()}");

                // Create a batch processing example
                var builder = new VortexBuilder(_logger);
                var batchProcess = builder.Batch<int>("NumberProcessor", 
                    async num => 
                    {
                        await Task.Delay(10); // Simulate work
                        return num * 2;
                    });

                var numbers = Enumerable.Range(1, 20).ToList();
                var startTime = DateTime.UtcNow;
                
                var results = await batchProcess.ExecuteAsync(context, numbers);
                
                var duration = DateTime.UtcNow - startTime;
                Console.WriteLine($"  - Processed {results.Count()} items in {duration.TotalMilliseconds:F0}ms");
                Console.WriteLine($"  - Items per second: {batchProcess.ItemsProcessed / Math.Max(0.001, batchProcess.ElapsedTime.TotalSeconds):F1}");
            }
        }
    }

    /// <summary>
    /// Demonstrates process chaining and orchestration
    /// </summary>
    static async Task DemoProcessChaining()
    {
        Console.WriteLine("🔗 Demo 3: Process Chaining");
        Console.WriteLine("---------------------------");

        using var context = VortexContext.ForDevelopment("chain-demo");
        
        var builder = new VortexBuilder(_logger);
        
        // Create a chain of processes
        var chain = builder.CreateChain("DataProcessingChain")
            .AddTransform<string, string>(input => Task.FromResult(input.Trim()), "TrimWhitespace")
            .AddTransform<string, string>(input => Task.FromResult(input.ToLowerInvariant()), "ToLowerCase")
            .AddTransform<string, string[]>(input => Task.FromResult(input.Split(' ')), "SplitWords")
            .AddTransform<string[], string[]>(words => Task.FromResult(words.Where(w => w.Length > 3).ToArray()), "FilterShortWords")
            .AddTransform<string[], string>(words => Task.FromResult(string.Join("-", words)), "JoinWithDashes");

        // Subscribe to chain events
        context.EventStream.Subscribe(evt =>
        {
            if (evt is ProcessChainStartedEvent started)
                Console.WriteLine($"🚀 Chain started: {started.ChainName} with {started.StepCount} steps");
            else if (evt is ProcessChainCompletedEvent completed)
                Console.WriteLine($"✅ Chain completed: {completed.ChainName} ({completed.StepsCompleted} steps)");
        });

        var input = "  Hello World from Vortex Programming Framework  ";
        Console.WriteLine($"Input: '{input}'");

        var result = await chain.ExecuteAsync<string, string>(context, input);
        Console.WriteLine($"Output: '{result}'");

        // Show chain step information
        Console.WriteLine("\nChain Steps:");
        foreach (var step in chain.GetStepInfo())
        {
            Console.WriteLine($"  {step.Index + 1}. {step.StepName} ({step.InputType} → {step.OutputType})");
        }

        chain.Dispose();
    }

    /// <summary>
    /// Demonstrates the fluent API for creating processes
    /// </summary>
    static async Task DemoFluentApi()
    {
        Console.WriteLine("🎨 Demo 4: Fluent API");
        Console.WriteLine("---------------------");

        using var context = VortexContext.ForDevelopment("fluent-demo");
        
        var builder = new VortexBuilder(_logger);

        // Create a complex process using fluent API
        var complexProcess = builder.CreateProcess<List<int>, Dictionary<string, object>>("StatisticsCalculator")
            .WithMetadata("version", "1.0")
            .WithMetadata("author", "Vortex Team")
            .When((numbers, ctx) => numbers.Count > 0)
            .Initialize(async (ctx, cancellationToken) =>
            {
                Console.WriteLine("🔧 Initializing statistics calculation...");
                await Task.Delay(100, cancellationToken);
            })
            .Execute(async (numbers, cancellationToken) =>
            {
                await Task.Delay(200, cancellationToken); // Simulate computation
                
                return new Dictionary<string, object>
                {
                    ["count"] = numbers.Count,
                    ["sum"] = numbers.Sum(),
                    ["average"] = numbers.Average(),
                    ["min"] = numbers.Min(),
                    ["max"] = numbers.Max(),
                    ["variance"] = CalculateVariance(numbers),
                    ["processed_at"] = DateTimeOffset.UtcNow
                };
            })
            .Build();

        var numbers = Enumerable.Range(1, 100).ToList();
        var stats = await complexProcess.ExecuteAsync(context, numbers);

        Console.WriteLine("📊 Statistics Results:");
        foreach (var kvp in stats)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }

        complexProcess.Dispose();
    }

    /// <summary>
    /// Demonstrates DSL-based process definition
    /// </summary>
    static async Task DemoDslProcesses()
    {
        Console.WriteLine("📜 Demo 5: DSL-Based Process Definition");
        Console.WriteLine("---------------------------------------");

        using var context = VortexContext.ForDevelopment("dsl-demo");

        // Define a process using JSON DSL
        var processDefinitionJson = """
        {
            "Name": "TextProcessor",
            "Type": "transform",
            "Parameters": {
                "transformType": "uppercase"
            },
            "Metadata": {
                "description": "Converts text to uppercase",
                "version": "1.0"
            },
            "Conditions": [
                {
                    "Type": "environment",
                    "Parameters": {
                        "environment": "Development"
                    }
                }
            ]
        }
        """;

        var dslProcess = VortexDsl.FromJson(processDefinitionJson);
        var result = await dslProcess.ExecuteAsync(context, "hello from dsl!");

        Console.WriteLine($"DSL Process Result: {result}");

        // Define a process chain using DSL
        var chainDefinition = new ChainDefinition
        {
            Name = "TextProcessingChain",
            Steps = new List<StepDefinition>
            {
                new() 
                { 
                    Name = "Uppercase", 
                    Process = new ProcessDefinition 
                    { 
                        Name = "UppercaseStep", 
                        Type = "transform",
                        Parameters = new() { ["transformType"] = "uppercase" }
                    }
                },
                new() 
                { 
                    Name = "Reverse", 
                    Process = new ProcessDefinition 
                    { 
                        Name = "ReverseStep", 
                        Type = "transform",
                        Parameters = new() { ["transformType"] = "reverse" }
                    }
                }
            }
        };

        var dslChain = VortexDsl.FromChainDefinition(chainDefinition);
        var chainResult = await dslChain.ExecuteAsync<object, object>(context, "vortex programming");

        Console.WriteLine($"DSL Chain Result: {chainResult}");

        dslProcess.Dispose();
        dslChain.Dispose();
    }

    /// <summary>
    /// Demonstrates enterprise order processing
    /// </summary>
    static async Task DemoEnterpriseOrderProcessing()
    {
        Console.WriteLine("🏢 Demo 6: Enterprise Order Processing");
        Console.WriteLine("--------------------------------------");

        var tenantId = new TenantId("enterprise-corp");
        using var context = VortexContext.ForProduction(tenantId, VortexScale.Medium);
        
        context.UserId = "user123";
        context.CorrelationId = Guid.NewGuid().ToString();

        // Subscribe to order processing events
        context.EventStream.Subscribe(evt =>
        {
            switch (evt)
            {
                case ProcessStartedEvent started:
                    Console.WriteLine($"🚀 Order processing started: {started.ProcessInstanceId}");
                    break;
                case ProcessCompletedEvent completed:
                    Console.WriteLine($"✅ Order processing completed in {completed.Duration.TotalMilliseconds:F0}ms");
                    Console.WriteLine($"   Items processed: {completed.ItemsProcessed}");
                    break;
            }
        });

        var orderProcess = new HandleOrderProcess();

        var orderRequest = new OrderRequest
        {
            OrderId = "ORD-2024-001",
            CustomerId = "CUST-12345",
            Items = new List<OrderItem>
            {
                new() { ProductId = "PROD-001", Quantity = 2, Price = 29.99m },
                new() { ProductId = "PROD-002", Quantity = 1, Price = 149.99m },
                new() { ProductId = "PROD-003", Quantity = 3, Price = 9.99m }
            },
            SpecialInstructions = "Handle with care"
        };

        Console.WriteLine($"Processing order: {orderRequest.OrderId}");
        Console.WriteLine($"Customer: {orderRequest.CustomerId}");
        Console.WriteLine($"Items: {orderRequest.Items.Count}");

        var orderResult = await orderProcess.ExecuteAsync(context, orderRequest);

        Console.WriteLine("\n📦 Order Processing Results:");
        Console.WriteLine($"  Status: {orderResult.Status}");
        Console.WriteLine($"  Payment Transaction: {orderResult.PaymentTransactionId}");
        Console.WriteLine($"  Shipment ID: {orderResult.ShipmentId}");
        Console.WriteLine($"  Estimated Delivery: {orderResult.EstimatedDelivery:yyyy-MM-dd}");
        Console.WriteLine($"  Items Confirmed: {orderResult.Items.Count}");

        orderProcess.Dispose();
    }

    /// <summary>
    /// Demonstrates complex data pipeline processing
    /// </summary>
    static async Task DemoDataPipeline()
    {
        Console.WriteLine("🔄 Demo 7: Complex Data Pipeline");
        Console.WriteLine("--------------------------------");

        var tenantId = new TenantId("data-corp");
        using var context = VortexContext.ForProduction(tenantId, VortexScale.Large);

        // Subscribe to pipeline events
        context.EventStream.Subscribe(evt =>
        {
            if (evt is ProcessStartedEvent started)
                Console.WriteLine($"🚀 Pipeline started: {started.ProcessName}");
        });

        var pipelineProcess = new DataPipelineProcess();

        var pipelineInput = new DataPipelineInput
        {
            PipelineId = "PIPE-2024-001",
            TargetSystem = "warehouse",
            DataSources = new List<DataSource>
            {
                new() { Id = "DB1", Type = "database", ConnectionString = "Server=db1", RecordCount = 1000 },
                new() { Id = "API1", Type = "api", ConnectionString = "https://api1.com", RecordCount = 500 },
                new() { Id = "FILE1", Type = "file", ConnectionString = "/data/file1.csv", RecordCount = 2000 }
            }
        };

        Console.WriteLine($"Starting data pipeline: {pipelineInput.PipelineId}");
        Console.WriteLine($"Data sources: {pipelineInput.DataSources.Count}");
        Console.WriteLine($"Expected records: {pipelineInput.DataSources.Sum(ds => ds.RecordCount)}");

        var pipelineResult = await pipelineProcess.ExecuteAsync(context, pipelineInput);

        Console.WriteLine("\n📊 Pipeline Results:");
        Console.WriteLine($"  Status: {pipelineResult.Status}");
        Console.WriteLine($"  Duration: {pipelineResult.Duration.TotalSeconds:F2} seconds");
        Console.WriteLine($"  Total Records Processed: {pipelineResult.TotalRecordsProcessed:N0}");

        Console.WriteLine("\n📋 Stage Details:");
        foreach (var stage in pipelineResult.Stages)
        {
            Console.WriteLine($"  {stage.StageName}:");
            Console.WriteLine($"    Records: {stage.RecordsProcessed:N0}");
            Console.WriteLine($"    Duration: {stage.Duration.TotalMilliseconds:F0}ms");
            if (stage.ValidationErrors?.Any() == true)
            {
                Console.WriteLine($"    Validation Errors: {stage.ValidationErrors.Count}");
            }
        }

        pipelineProcess.Dispose();
    }

    /// <summary>
    /// Helper method to calculate variance
    /// </summary>
    private static double CalculateVariance(List<int> numbers)
    {
        var mean = numbers.Average();
        return numbers.Average(x => Math.Pow(x - mean, 2));
    }
} 