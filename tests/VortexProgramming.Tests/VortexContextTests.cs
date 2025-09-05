using Microsoft.Extensions.Logging;
using VortexProgramming.Core.Context;
using VortexProgramming.Core.Enums;
using VortexProgramming.Core.Events;
using VortexProgramming.Core.Models;
using Xunit;

namespace VortexProgramming.Tests;

public class VortexContextTests
{
    [Fact]
    public void Constructor_ShouldCreateContextWithCorrectProperties()
    {
        // Arrange
        var tenantId = new TenantId("test-tenant");
        var environment = VortexEnvironment.Development;
        var scale = VortexScale.Medium;

        // Act
        using var context = new VortexContext(tenantId, environment, scale);

        // Assert
        Assert.Equal(tenantId, context.TenantId);
        Assert.Equal(environment, context.Environment);
        Assert.Equal(scale, context.Scale);
        Assert.NotEqual(Guid.Empty, context.ContextId);
        Assert.True(context.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Constructor_WithAutoScale_ShouldDetermineOptimalScale()
    {
        // Arrange
        var tenantId = new TenantId("test-tenant");

        // Act & Assert - Development should be Small
        using var devContext = new VortexContext(tenantId, VortexEnvironment.Development, VortexScale.Auto);
        Assert.Equal(VortexScale.Small, devContext.Scale);

        // Act & Assert - Production should be Large
        using var prodContext = new VortexContext(tenantId, VortexEnvironment.Production, VortexScale.Auto);
        Assert.Equal(VortexScale.Large, prodContext.Scale);
    }

    [Fact]
    public void ForDevelopment_ShouldCreateOptimizedDevelopmentContext()
    {
        // Arrange
        var tenantId = new TenantId("dev-tenant");

        // Act
        using var context = VortexContext.ForDevelopment(tenantId);

        // Assert
        Assert.Equal(VortexEnvironment.Development, context.Environment);
        Assert.Equal(VortexScale.Small, context.Scale);
        Assert.True(context.GetProperty<bool>("debug.enabled"));
        Assert.Equal("Debug", context.GetProperty<string>("logging.level"));
    }

    [Fact]
    public void ForProduction_ShouldCreateOptimizedProductionContext()
    {
        // Arrange
        var tenantId = new TenantId("prod-tenant");

        // Act
        using var context = VortexContext.ForProduction(tenantId);

        // Assert
        Assert.Equal(VortexEnvironment.Production, context.Environment);
        Assert.Equal(VortexScale.Large, context.Scale);
        Assert.False(context.GetProperty<bool>("debug.enabled"));
        Assert.True(context.GetProperty<bool>("monitoring.enabled"));
    }

    [Fact]
    public void SetProperty_ShouldStorePropertyValue()
    {
        // Arrange
        using var context = new VortexContext("test-tenant");
        var key = "test.property";
        var value = "test-value";

        // Act
        var result = context.SetProperty(key, value);

        // Assert
        Assert.Same(context, result); // Should return self for chaining
        Assert.Equal(value, context.GetProperty<string>(key));
    }

    [Fact]
    public void GetProperty_WithNonExistentKey_ShouldReturnDefault()
    {
        // Arrange
        using var context = new VortexContext("test-tenant");

        // Act
        var result = context.GetProperty<string>("non-existent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetProperty_WithDefaultValue_ShouldReturnDefaultWhenKeyNotFound()
    {
        // Arrange
        using var context = new VortexContext("test-tenant");
        var defaultValue = "default-value";

        // Act
        var result = context.GetProperty("non-existent", defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public void EmitEvent_ShouldPublishEventToStream()
    {
        // Arrange
        using var context = new VortexContext("test-tenant");
        VortexEvent? receivedEvent = null;
        
        context.EventStream.Subscribe(evt => receivedEvent = evt);

        var testEvent = new TestEvent { Source = "test" };

        // Act
        context.EmitEvent(testEvent);

        // Assert
        Assert.NotNull(receivedEvent);
        Assert.Equal("test-tenant", receivedEvent.TenantId.Value);
        Assert.Equal("test", receivedEvent.Source);
    }

    [Fact]
    public void CreateChildContext_ShouldInheritPropertiesAndConfiguration()
    {
        // Arrange
        using var parentContext = new VortexContext("parent-tenant", VortexEnvironment.Production, VortexScale.Large)
        {
            UserId = "parent-user"
        };
        parentContext.SetProperty("shared.config", "shared-value");

        // Act
        using var childContext = parentContext.CreateChildContext("child-correlation");

        // Assert
        Assert.Equal(parentContext.TenantId, childContext.TenantId);
        Assert.Equal(parentContext.Environment, childContext.Environment);
        Assert.Equal(parentContext.Scale, childContext.Scale);
        Assert.Equal("parent-user", childContext.UserId);
        Assert.Equal("child-correlation", childContext.CorrelationId);
        Assert.Equal("shared-value", childContext.GetProperty<string>("shared.config"));
        Assert.NotEqual(parentContext.ContextId, childContext.ContextId);
    }

    [Fact]
    public void UpdateScale_ShouldChangeScaleAndEmitEvent()
    {
        // Arrange
        using var context = new VortexContext("test-tenant", scale: VortexScale.Small);
        ContextScaleChangedEvent? scaleChangedEvent = null;
        
        context.EventStream.Subscribe(evt =>
        {
            if (evt is ContextScaleChangedEvent scaleEvent)
                scaleChangedEvent = scaleEvent;
        });

        // Act
        context.UpdateScale(VortexScale.Large);

        // Assert
        Assert.Equal(VortexScale.Large, context.Scale);
        Assert.NotNull(scaleChangedEvent);
        Assert.Equal(VortexScale.Small, scaleChangedEvent.OldScale);
        Assert.Equal(VortexScale.Large, scaleChangedEvent.NewScale);
    }

    [Fact]
    public void UpdateScale_WithSameScale_ShouldNotEmitEvent()
    {
        // Arrange
        using var context = new VortexContext("test-tenant", scale: VortexScale.Medium);
        var eventReceived = false;
        
        context.EventStream.Subscribe(_ => eventReceived = true);

        // Act
        context.UpdateScale(VortexScale.Medium);

        // Assert
        Assert.False(eventReceived);
    }

    [Theory]
    [InlineData(VortexScale.Small, false)]
    [InlineData(VortexScale.Medium, true)]
    [InlineData(VortexScale.Large, true)]
    public void ShouldUseParallelExecution_ShouldReturnCorrectValue(VortexScale scale, bool expected)
    {
        // Arrange
        using var context = new VortexContext("test-tenant", scale: scale);

        // Act
        var result = context.ShouldUseParallelExecution();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(VortexScale.Small, 1)]
    [InlineData(VortexScale.Medium, true)] // Should be > 1
    [InlineData(VortexScale.Large, true)] // Should be > 1
    public void GetRecommendedParallelism_ShouldReturnAppropriateValue(VortexScale scale, object expected)
    {
        // Arrange
        using var context = new VortexContext("test-tenant", scale: scale);

        // Act
        var result = context.GetRecommendedParallelism();

        // Assert
        if (expected is int expectedInt)
        {
            Assert.Equal(expectedInt, result);
        }
        else if (expected is bool expectedBool && expectedBool)
        {
            Assert.True(result > 1);
        }
    }

    [Theory]
    [InlineData(VortexScale.Large, VortexEnvironment.Production, true)]
    [InlineData(VortexScale.Large, VortexEnvironment.Development, false)]
    [InlineData(VortexScale.Medium, VortexEnvironment.Production, false)]
    public void ShouldUseDistributedExecution_ShouldReturnCorrectValue(
        VortexScale scale, 
        VortexEnvironment environment, 
        bool expected)
    {
        // Arrange
        using var context = new VortexContext("test-tenant", environment, scale);

        // Act
        var result = context.ShouldUseDistributedExecution();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Dispose_ShouldCompleteEventStream()
    {
        // Arrange
        var context = new VortexContext("test-tenant");
        var streamCompleted = false;
        
        context.EventStream.Subscribe(
            onNext: _ => { },
            onCompleted: () => streamCompleted = true
        );

        // Act
        context.Dispose();

        // Assert
        Assert.True(streamCompleted);
    }

    [Fact]
    public void EmitEvent_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var context = new VortexContext("test-tenant");
        context.Dispose();

        var testEvent = new TestEvent { Source = "test" };

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => context.EmitEvent(testEvent));
    }

    private record TestEvent : VortexEvent;
} 