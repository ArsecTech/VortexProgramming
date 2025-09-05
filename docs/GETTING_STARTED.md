# 🚀 Getting Started with Vortex Programming

Welcome to the future of enterprise development! This guide will have you building context-aware, self-scaling applications in under 10 minutes.

## 📋 Prerequisites

- ✅ **.NET 9 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
- ✅ **C# 13** knowledge (basic async/await, records, pattern matching)
- ✅ **Visual Studio 2022** or **VS Code** with C# extension

## 🏃‍♂️ Quick Start (2 Minutes)

### 1. Create New Project
```bash
dotnet new console -n MyVortexApp
cd MyVortexApp
dotnet add package VortexProgramming.Core
dotnet add package VortexProgramming.Extensions
```

### 2. Your First Vortex Process
Replace `Program.cs` with:

```csharp
using VortexProgramming.Core.Context;
using VortexProgramming.Extensions.FluentApi;

// 🎯 Create context-aware environment
var context = VortexContext.ForDevelopment("my-company");

// 🔥 Build process without classes using Fluent API
var builder = new VortexBuilder();
var textProcessor = builder.Transform<string, string>(
    "UppercaseProcessor",
    input => input.ToUpperInvariant()
);

// 🚀 Execute with automatic scaling
var result = await textProcessor.ExecuteAsync(context, "hello vortex!");
Console.WriteLine($"Result: {result}"); // Result: HELLO VORTEX!

Console.WriteLine($"Environment: {context.Environment}");      // Development
Console.WriteLine($"Scale: {context.Scale}");                  // Small  
Console.WriteLine($"Threads: {context.GetRecommendedParallelism()}"); // 1
```

### 3. Run It!
```bash
dotnet run
# Result: HELLO VORTEX!
# Environment: Development
# Scale: Small
# Threads: 1
```

🎉 **Congratulations!** You just built your first context-aware process!

## 🧠 Understanding the Magic

### What Just Happened?
1. **VortexContext** detected you're in Development mode
2. **Automatically configured** for 1 thread, debug logging enabled
3. **Fluent API** created a process without writing a class
4. **Self-scaling** chose sequential execution for dev environment

### Change Environment, Change Behavior
```csharp
// Same code, different context = different behavior!
var prodContext = VortexContext.ForProduction("my-company");
var result = await textProcessor.ExecuteAsync(prodContext, "hello vortex!");

// Now runs with:
// - 40 threads (automatic)
// - Production monitoring
// - Resilience patterns enabled
```

## 🏗️ Building Real Applications

### Example 1: Order Processing System
```csharp
using VortexProgramming.Core.Processing;

public class OrderProcessor : VortexProcess<Order, OrderResult>
{
    public override string ProcessName => "OrderProcessor";

    protected override async Task<OrderResult> ExecuteInternalAsync(
        Order order, 
        CancellationToken cancellationToken)
    {
        // 🧠 Framework chooses execution strategy based on context
        await ProcessItemsAsync(order.Items, async (item, ct) =>
        {
            // Validate item
            await ValidateItem(item, ct);
            
            // Check inventory  
            await CheckInventory(item, ct);
            
            // Reserve item
            await ReserveItem(item, ct);
            
        }, cancellationToken);

        return new OrderResult 
        { 
            OrderId = order.Id,
            Status = "Processed",
            ItemsProcessed = ItemsProcessed,
            ProcessingTime = ElapsedTime
        };
    }
}

// Usage
var context = VortexContext.ForProduction("ecommerce-corp", VortexScale.Large);
var processor = new OrderProcessor();

var order = new Order 
{ 
    Id = "ORD-001", 
    Items = GenerateOrderItems(1000) // 1000 items
};

var result = await processor.ExecuteAsync(context, order);
// Automatically processes 1000 items in parallel using 40 threads!
```

### Example 2: Data Pipeline with Chaining
```csharp
var builder = new VortexBuilder();

// 🔗 Build composable ETL pipeline
var dataPipeline = builder.CreateChain("ETL-Pipeline")
    .AddTransform<string, RawData>(
        json => JsonSerializer.Deserialize<RawData>(json), 
        "ParseJSON")
    .AddTransform<RawData, CleanData>(
        raw => CleanAndValidate(raw), 
        "CleanData")
    .AddTransform<CleanData, EnrichedData>(
        clean => EnrichWithExternalData(clean), 
        "EnrichData")
    .AddTransform<EnrichedData, string>(
        enriched => JsonSerializer.Serialize(enriched), 
        "SerializeResult");

// 🚀 Process 10,000 records
var context = VortexContext.ForProduction("data-corp", VortexScale.Large);
var results = new List<string>();

foreach (var rawJson in GetRawDataStream(10000))
{
    var processed = await dataPipeline.ExecuteAsync<string, string>(context, rawJson);
    results.Add(processed);
}

// Framework automatically:
// ✅ Scales each stage independently
// ✅ Monitors performance per stage  
// ✅ Handles failures gracefully
// ✅ Provides detailed metrics
```

## 📊 Monitoring & Observability

### Real-time Event Streaming
```csharp
// 🔔 Subscribe to all events
context.EventStream.Subscribe(evt =>
{
    Console.WriteLine($"[{evt.Timestamp:HH:mm:ss}] {evt.GetType().Name}");
    
    switch (evt)
    {
        case ProcessStartedEvent started:
            Console.WriteLine($"  🚀 {started.ProcessName} started");
            Console.WriteLine($"  📊 Scale: {started.Scale}, Threads: {started.ParallelInstances}");
            break;
            
        case ProcessCompletedEvent completed:
            Console.WriteLine($"  ✅ Completed in {completed.Duration.TotalMilliseconds:F0}ms");
            Console.WriteLine($"  📈 Processed {completed.ItemsProcessed} items");
            Console.WriteLine($"  ⚡ Throughput: {completed.ItemsProcessed / completed.Duration.TotalSeconds:F1} items/sec");
            break;
            
        case ProcessFailedEvent failed:
            Console.WriteLine($"  ❌ Failed: {failed.ErrorMessage}");
            break;
    }
});
```

### Performance Metrics
```csharp
// 📈 Collect performance data
var performanceCollector = new List<ProcessCompletedEvent>();

context.EventStream
    .OfType<ProcessCompletedEvent>()
    .Subscribe(evt => performanceCollector.Add(evt));

// After processing...
var avgDuration = performanceCollector.Average(e => e.Duration.TotalMilliseconds);
var totalThroughput = performanceCollector.Sum(e => e.ItemsProcessed);

Console.WriteLine($"Average Duration: {avgDuration:F0}ms");
Console.WriteLine($"Total Throughput: {totalThroughput:N0} items");
```

## 🎯 Best Practices

### ✅ DO: Use Context Appropriately
```csharp
// ✅ Let framework choose optimal settings
var context = VortexContext.ForProduction("my-tenant", VortexScale.Auto);

// ✅ Use different contexts for different scenarios  
var devContext = VortexContext.ForDevelopment("my-tenant");     // Fast iteration
var testContext = VortexContext.ForTesting("my-tenant");        // Predictable results
var prodContext = VortexContext.ForProduction("my-tenant");     // Maximum performance
```

### ✅ DO: Leverage Process Chaining
```csharp
// ✅ Compose complex workflows from simple steps
var workflow = builder.CreateChain("UserRegistration")
    .AddStep(validateEmailProcess)
    .AddStep(checkDuplicateProcess)  
    .AddStep(createAccountProcess)
    .AddStep(sendWelcomeEmailProcess);
```

### ✅ DO: Monitor Everything
```csharp
// ✅ Subscribe to events for observability
context.EventStream.Subscribe(evt => logger.LogInformation("{Event}", evt));
```

### ❌ DON'T: Manual Environment Logic
```csharp
// ❌ Don't write environment-specific code
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
{
    // Use 10 threads
}
else
{
    // Use 1 thread  
}

// ✅ Let Vortex handle this automatically
var context = VortexContext.ForEnvironment(environmentName, VortexScale.Auto);
```

### ❌ DON'T: Ignore Context Capabilities
```csharp
// ❌ Don't use Parallel.ForEach manually
Parallel.ForEach(items, item => ProcessItem(item));

// ✅ Use VortexProcess.ProcessItemsAsync for context-aware scaling
await ProcessItemsAsync(items, ProcessItem, cancellationToken);
```

## 🚀 Next Steps

### Explore Advanced Features
1. **📜 DSL Processes** - Define processes using JSON
2. **🔗 Complex Chaining** - Build sophisticated workflows  
3. **🏢 Multi-Tenant** - Secure tenant isolation
4. **📊 Custom Metrics** - Build your own monitoring

### Sample Projects
- **🛒 E-Commerce Platform** - `examples/ECommerce/`
- **📊 Data Analytics** - `examples/DataAnalytics/`  
- **🔄 ETL Pipeline** - `examples/ETLPipeline/`
- **🌐 Microservices** - `examples/Microservices/`

### Community & Support
- 📖 **Full Documentation** - [docs.vortexprogramming.com](https://docs.vortexprogramming.com)
- 💬 **Community Discord** - [discord.gg/vortexprogramming](https://discord.gg/vortexprogramming)
- 🐛 **Issues & Questions** - [GitHub Issues](https://github.com/ArsecTech/VortexProgramming/issues)

## 💡 Pro Tips

### Performance Optimization
```csharp
// 🚀 For CPU-intensive work, use Large scale
var context = VortexContext.ForProduction("my-tenant", VortexScale.Large);

// 📊 For I/O-intensive work, Medium scale often optimal
var context = VortexContext.ForProduction("my-tenant", VortexScale.Medium);

// 🧪 For testing, always use Small scale for predictability
var context = VortexContext.ForTesting("my-tenant"); // Always Small scale
```

### Debugging Tips
```csharp
// 🔍 Enable detailed logging in development
var context = VortexContext.ForDevelopment("my-tenant")
    .SetProperty("logging.level", "Debug")
    .SetProperty("performance.tracking", true);

// 📊 Add custom properties for debugging
context.SetProperty("debug.requestId", requestId)
       .SetProperty("debug.userId", userId);
```

### Error Handling
```csharp
try
{
    var result = await process.ExecuteAsync(context, input);
}
catch (OperationCanceledException)
{
    // Process was cancelled
}
catch (InvalidOperationException ex) when (ex.Message.Contains("condition not met"))
{
    // Process condition failed
}
catch (Exception ex)
{
    // Other process failures
    logger.LogError(ex, "Process failed: {ProcessName}", process.ProcessName);
}
```

---

🎉 **You're now ready to build revolutionary enterprise applications with Vortex Programming!**

**Next:** Check out our [Enterprise Examples](ENTERPRISE_EXAMPLES.md) or dive into [Advanced Features](ADVANCED_FEATURES.md). 