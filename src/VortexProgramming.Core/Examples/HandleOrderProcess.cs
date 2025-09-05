using Microsoft.Extensions.Logging;
using VortexProgramming.Core.Processing;

namespace VortexProgramming.Core.Examples;

/// <summary>
/// Example enterprise process for handling customer orders
/// </summary>
public class HandleOrderProcess : VortexProcess<OrderRequest, OrderResult>
{
    public override string ProcessName => "HandleOrder";

    public HandleOrderProcess(ILogger<HandleOrderProcess>? logger = null) : base(logger)
    {
    }

    protected override async Task<OrderResult> ExecuteInternalAsync(OrderRequest input, CancellationToken cancellationToken)
    {
        var result = new OrderResult
        {
            OrderId = input.OrderId,
            CustomerId = input.CustomerId,
            Status = OrderStatus.Processing,
            ProcessedAt = DateTimeOffset.UtcNow,
            Items = new List<ProcessedOrderItem>()
        };

        // Step 1: Validate Order
        await ValidateOrderAsync(input, cancellationToken);
        UpdateProgress(1, new Dictionary<string, object> { ["step"] = "validation" });

        // Step 2: Check Inventory (context-aware scaling)
        var inventoryResults = await CheckInventoryAsync(input.Items, cancellationToken);
        UpdateProgress(2, new Dictionary<string, object> { ["step"] = "inventory" });

        // Step 3: Process Payment
        var paymentResult = await ProcessPaymentAsync(input, cancellationToken);
        UpdateProgress(3, new Dictionary<string, object> { ["step"] = "payment" });

        // Step 4: Reserve Items
        await ReserveItemsAsync(inventoryResults, cancellationToken);
        UpdateProgress(4, new Dictionary<string, object> { ["step"] = "reservation" });

        // Step 5: Create Shipment
        var shipmentId = await CreateShipmentAsync(input, cancellationToken);
        UpdateProgress(5, new Dictionary<string, object> { ["step"] = "shipment" });

        result.Status = OrderStatus.Confirmed;
        result.PaymentTransactionId = paymentResult.TransactionId;
        result.ShipmentId = shipmentId;
        result.EstimatedDelivery = DateTimeOffset.UtcNow.AddDays(3);
        result.Items = inventoryResults.Select(ir => new ProcessedOrderItem
        {
            ProductId = ir.ProductId,
            Quantity = ir.AvailableQuantity,
            Price = ir.Price,
            ReservationId = ir.ReservationId
        }).ToList();

        return result;
    }

    private async Task ValidateOrderAsync(OrderRequest order, CancellationToken cancellationToken)
    {
        // Simulate validation logic
        await Task.Delay(Context.Environment == VortexProgramming.Core.Enums.VortexEnvironment.Development ? 100 : 50, cancellationToken);
        
        if (order.Items.Count == 0)
        {
            throw new InvalidOperationException("Order must contain at least one item");
        }

        if (string.IsNullOrEmpty(order.CustomerId))
        {
            throw new InvalidOperationException("Customer ID is required");
        }
    }

    private async Task<List<InventoryResult>> CheckInventoryAsync(List<OrderItem> items, CancellationToken cancellationToken)
    {
        var results = new List<InventoryResult>();

        // Use context-aware processing based on scale
        await ProcessItemsAsync(items, async (item, ct) =>
        {
            // Simulate inventory check
            var delay = Context.Scale switch
            {
                VortexProgramming.Core.Enums.VortexScale.Small => 200,
                VortexProgramming.Core.Enums.VortexScale.Medium => 100,
                VortexProgramming.Core.Enums.VortexScale.Large => 50,
                _ => 100
            };

            await Task.Delay(delay, ct);

            var inventoryResult = new InventoryResult
            {
                ProductId = item.ProductId,
                RequestedQuantity = item.Quantity,
                AvailableQuantity = Math.Min(item.Quantity, Random.Shared.Next(1, item.Quantity + 5)),
                Price = item.Price,
                ReservationId = Guid.NewGuid().ToString()
            };

            lock (results)
            {
                results.Add(inventoryResult);
            }
        }, cancellationToken);

        return results;
    }

    private async Task<PaymentResult> ProcessPaymentAsync(OrderRequest order, CancellationToken cancellationToken)
    {
        // Simulate payment processing
        var delay = Context.Environment == VortexProgramming.Core.Enums.VortexEnvironment.Production ? 500 : 200;
        await Task.Delay(delay, cancellationToken);

        return new PaymentResult
        {
            TransactionId = Guid.NewGuid().ToString(),
            Amount = order.Items.Sum(i => i.Price * i.Quantity),
            Status = "Completed",
            ProcessedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task ReserveItemsAsync(List<InventoryResult> inventoryResults, CancellationToken cancellationToken)
    {
        // Simulate item reservation
        await ProcessItemsAsync(inventoryResults, async (item, ct) =>
        {
            await Task.Delay(100, ct);
            // In a real system, this would update inventory reservations
        }, cancellationToken);
    }

    private async Task<string> CreateShipmentAsync(OrderRequest order, CancellationToken cancellationToken)
    {
        // Simulate shipment creation
        await Task.Delay(300, cancellationToken);
        return Guid.NewGuid().ToString();
    }
}

/// <summary>
/// Input data for order processing
/// </summary>
public record OrderRequest
{
    public required string OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required List<OrderItem> Items { get; init; }
    public string? SpecialInstructions { get; init; }
}

/// <summary>
/// Individual order item
/// </summary>
public record OrderItem
{
    public required string ProductId { get; init; }
    public required int Quantity { get; init; }
    public required decimal Price { get; init; }
}

/// <summary>
/// Result of order processing
/// </summary>
public record OrderResult
{
    public required string OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required OrderStatus Status { get; set; }
    public required DateTimeOffset ProcessedAt { get; init; }
    public string? PaymentTransactionId { get; set; }
    public string? ShipmentId { get; set; }
    public DateTimeOffset? EstimatedDelivery { get; set; }
    public required List<ProcessedOrderItem> Items { get; set; }
}

/// <summary>
/// Processed order item with reservation details
/// </summary>
public record ProcessedOrderItem
{
    public required string ProductId { get; init; }
    public required int Quantity { get; init; }
    public required decimal Price { get; init; }
    public string? ReservationId { get; init; }
}

/// <summary>
/// Order status enumeration
/// </summary>
public enum OrderStatus
{
    Pending,
    Processing,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}

/// <summary>
/// Inventory check result
/// </summary>
internal record InventoryResult
{
    public required string ProductId { get; init; }
    public required int RequestedQuantity { get; init; }
    public required int AvailableQuantity { get; init; }
    public required decimal Price { get; init; }
    public required string ReservationId { get; init; }
}

/// <summary>
/// Payment processing result
/// </summary>
internal record PaymentResult
{
    public required string TransactionId { get; init; }
    public required decimal Amount { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset ProcessedAt { get; init; }
} 