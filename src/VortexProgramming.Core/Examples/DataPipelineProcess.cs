using Microsoft.Extensions.Logging;
using System.Text.Json;
using VortexProgramming.Core.Processing;

namespace VortexProgramming.Core.Examples;

/// <summary>
/// Complex enterprise data pipeline process demonstrating advanced Vortex capabilities
/// </summary>
public class DataPipelineProcess : VortexProcess<DataPipelineInput, DataPipelineResult>
{
    public override string ProcessName => "DataPipeline";

    public DataPipelineProcess(ILogger<DataPipelineProcess>? logger = null) : base(logger)
    {
    }

    protected override async Task<DataPipelineResult> ExecuteInternalAsync(DataPipelineInput input, CancellationToken cancellationToken)
    {
        var result = new DataPipelineResult
        {
            PipelineId = input.PipelineId,
            StartedAt = DateTimeOffset.UtcNow,
            Stages = new List<PipelineStageResult>(),
            TotalRecordsProcessed = 0,
            Status = PipelineStatus.Running
        };

        try
        {
            // Stage 1: Data Extraction
            var extractionResult = await ExtractDataAsync(input, cancellationToken);
            result.Stages.Add(extractionResult);
            UpdateProgress(extractionResult.RecordsProcessed, new Dictionary<string, object> { ["stage"] = "extraction" });

            // Stage 2: Data Transformation (context-aware scaling)
            var transformationResult = await TransformDataAsync(extractionResult.Data, cancellationToken);
            result.Stages.Add(transformationResult);
            UpdateProgress(ItemsProcessed + transformationResult.RecordsProcessed, new Dictionary<string, object> { ["stage"] = "transformation" });

            // Stage 3: Data Validation
            var validationResult = await ValidateDataAsync(transformationResult.Data, cancellationToken);
            result.Stages.Add(validationResult);
            UpdateProgress(ItemsProcessed + validationResult.RecordsProcessed, new Dictionary<string, object> { ["stage"] = "validation" });

            // Stage 4: Data Loading
            var loadingResult = await LoadDataAsync(validationResult.Data, input.TargetSystem, cancellationToken);
            result.Stages.Add(loadingResult);
            UpdateProgress(ItemsProcessed + loadingResult.RecordsProcessed, new Dictionary<string, object> { ["stage"] = "loading" });

            result.Status = PipelineStatus.Completed;
            result.CompletedAt = DateTimeOffset.UtcNow;
            result.TotalRecordsProcessed = ItemsProcessed;
            result.Duration = ElapsedTime;

            return result;
        }
        catch (Exception ex)
        {
            result.Status = PipelineStatus.Failed;
            result.ErrorMessage = ex.Message;
            result.CompletedAt = DateTimeOffset.UtcNow;
            result.Duration = ElapsedTime;
            throw;
        }
    }

    private async Task<PipelineStageResult> ExtractDataAsync(DataPipelineInput input, CancellationToken cancellationToken)
    {
        var stageResult = new PipelineStageResult
        {
            StageName = "Extraction",
            StartedAt = DateTimeOffset.UtcNow,
            Data = new List<DataRecord>(),
            RecordsProcessed = 0
        };

        // Simulate data extraction from multiple sources
        var extractionTasks = input.DataSources.Select(async source =>
        {
            var records = new List<DataRecord>();
            
            // Simulate different extraction times based on source type
            var delay = source.Type switch
            {
                "database" => 300,
                "api" => 200,
                "file" => 100,
                _ => 150
            };

            await Task.Delay(delay, cancellationToken);

            // Generate sample data
            for (int i = 0; i < source.RecordCount; i++)
            {
                records.Add(new DataRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    SourceId = source.Id,
                    Data = new Dictionary<string, object>
                    {
                        ["timestamp"] = DateTimeOffset.UtcNow.AddMinutes(-Random.Shared.Next(0, 1440)),
                        ["value"] = Random.Shared.Next(1, 1000),
                        ["category"] = $"Category_{Random.Shared.Next(1, 10)}",
                        ["metadata"] = JsonSerializer.Serialize(new { source = source.Type, batch = i / 100 })
                    }
                });
            }

            return records;
        });

        var allRecords = await Task.WhenAll(extractionTasks);
        stageResult.Data = allRecords.SelectMany(r => r).ToList();
        stageResult.RecordsProcessed = stageResult.Data.Count;
        stageResult.CompletedAt = DateTimeOffset.UtcNow;
        stageResult.Duration = DateTimeOffset.UtcNow - stageResult.StartedAt;

        return stageResult;
    }

    private async Task<PipelineStageResult> TransformDataAsync(List<DataRecord> inputData, CancellationToken cancellationToken)
    {
        var stageResult = new PipelineStageResult
        {
            StageName = "Transformation",
            StartedAt = DateTimeOffset.UtcNow,
            Data = new List<DataRecord>(),
            RecordsProcessed = 0
        };

        // Use context-aware parallel processing for transformations
        var transformedData = new List<DataRecord>();

        await ProcessItemsAsync(inputData, async (record, ct) =>
        {
            // Simulate complex transformation logic
            await Task.Delay(Context.Scale switch
            {
                VortexProgramming.Core.Enums.VortexScale.Small => 50,
                VortexProgramming.Core.Enums.VortexScale.Medium => 25,
                VortexProgramming.Core.Enums.VortexScale.Large => 10,
                _ => 30
            }, ct);

            var transformedRecord = new DataRecord
            {
                Id = record.Id,
                SourceId = record.SourceId,
                Data = new Dictionary<string, object>(record.Data)
            };

            // Apply transformations
            if (transformedRecord.Data.TryGetValue("value", out var value) && value is int intValue)
            {
                transformedRecord.Data["normalized_value"] = intValue / 1000.0;
                transformedRecord.Data["value_squared"] = intValue * intValue;
            }

            transformedRecord.Data["processed_at"] = DateTimeOffset.UtcNow;
            transformedRecord.Data["transformation_version"] = "1.0";

            lock (transformedData)
            {
                transformedData.Add(transformedRecord);
            }
        }, cancellationToken);

        stageResult.Data = transformedData;
        stageResult.RecordsProcessed = transformedData.Count;
        stageResult.CompletedAt = DateTimeOffset.UtcNow;
        stageResult.Duration = DateTimeOffset.UtcNow - stageResult.StartedAt;

        return stageResult;
    }

    private async Task<PipelineStageResult> ValidateDataAsync(List<DataRecord> inputData, CancellationToken cancellationToken)
    {
        var stageResult = new PipelineStageResult
        {
            StageName = "Validation",
            StartedAt = DateTimeOffset.UtcNow,
            Data = new List<DataRecord>(),
            ValidationErrors = new List<string>(),
            RecordsProcessed = 0
        };

        var validData = new List<DataRecord>();
        var errors = new List<string>();

        await ProcessItemsAsync(inputData, async (record, ct) =>
        {
            await Task.Delay(10, ct); // Quick validation

            var isValid = true;
            var recordErrors = new List<string>();

            // Validate required fields
            if (!record.Data.ContainsKey("value"))
            {
                recordErrors.Add($"Record {record.Id}: Missing 'value' field");
                isValid = false;
            }

            if (!record.Data.ContainsKey("category"))
            {
                recordErrors.Add($"Record {record.Id}: Missing 'category' field");
                isValid = false;
            }

            // Validate data ranges
            if (record.Data.TryGetValue("normalized_value", out var normalizedValue) && 
                normalizedValue is double doubleValue && doubleValue > 1.0)
            {
                recordErrors.Add($"Record {record.Id}: Normalized value out of range");
                isValid = false;
            }

            lock (validData)
            {
                if (isValid)
                {
                    validData.Add(record);
                }
                else
                {
                    errors.AddRange(recordErrors);
                }
            }
        }, cancellationToken);

        stageResult.Data = validData;
        stageResult.ValidationErrors = errors;
        stageResult.RecordsProcessed = inputData.Count;
        stageResult.CompletedAt = DateTimeOffset.UtcNow;
        stageResult.Duration = DateTimeOffset.UtcNow - stageResult.StartedAt;

        return stageResult;
    }

    private async Task<PipelineStageResult> LoadDataAsync(List<DataRecord> inputData, string targetSystem, CancellationToken cancellationToken)
    {
        var stageResult = new PipelineStageResult
        {
            StageName = "Loading",
            StartedAt = DateTimeOffset.UtcNow,
            Data = inputData,
            RecordsProcessed = 0
        };

        // Simulate loading to target system with batching
        var batchSize = Context.Scale switch
        {
            VortexProgramming.Core.Enums.VortexScale.Small => 10,
            VortexProgramming.Core.Enums.VortexScale.Medium => 50,
            VortexProgramming.Core.Enums.VortexScale.Large => 100,
            _ => 25
        };

        var batches = inputData.Chunk(batchSize);
        
        await ProcessItemsAsync(batches, async (batch, ct) =>
        {
            // Simulate batch loading
            var delay = targetSystem switch
            {
                "database" => 200,
                "warehouse" => 300,
                "cache" => 50,
                _ => 150
            };

            await Task.Delay(delay, ct);
            
            // In a real system, this would perform the actual data loading
        }, cancellationToken);

        stageResult.RecordsProcessed = inputData.Count;
        stageResult.CompletedAt = DateTimeOffset.UtcNow;
        stageResult.Duration = DateTimeOffset.UtcNow - stageResult.StartedAt;

        return stageResult;
    }
}

/// <summary>
/// Input for the data pipeline process
/// </summary>
public record DataPipelineInput
{
    public required string PipelineId { get; init; }
    public required List<DataSource> DataSources { get; init; }
    public required string TargetSystem { get; init; }
    public Dictionary<string, object> Configuration { get; init; } = new();
}

/// <summary>
/// Data source configuration
/// </summary>
public record DataSource
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string ConnectionString { get; init; }
    public required int RecordCount { get; init; }
}

/// <summary>
/// Result of the data pipeline process
/// </summary>
public record DataPipelineResult
{
    public required string PipelineId { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public required PipelineStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public required List<PipelineStageResult> Stages { get; init; }
    public required long TotalRecordsProcessed { get; set; }
}

/// <summary>
/// Result of a pipeline stage
/// </summary>
public record PipelineStageResult
{
    public required string StageName { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public long RecordsProcessed { get; set; }
    public required List<DataRecord> Data { get; set; }
    public List<string>? ValidationErrors { get; set; }
}

/// <summary>
/// Individual data record
/// </summary>
public record DataRecord
{
    public required string Id { get; init; }
    public required string SourceId { get; init; }
    public required Dictionary<string, object> Data { get; init; }
}

/// <summary>
/// Pipeline status enumeration
/// </summary>
public enum PipelineStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
} 