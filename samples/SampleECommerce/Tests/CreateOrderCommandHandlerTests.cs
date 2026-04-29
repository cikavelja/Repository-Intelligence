using System;
using System.Threading;
using System.Threading.Tasks;
using Orders.Application.Commands.CreateOrder;
using Orders.Domain.Entities;
using Xunit;

namespace Tests;

public sealed class CreateOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateOrder_AndPublishOrderCreatedEvent()
    {
        // Arrange
        var repository = new FakeOrderRepository();
        var publisher = new FakeEventPublisher();
        var handler = new CreateOrderCommandHandler(repository, publisher);
        var command = new CreateOrderCommand
        {
            CustomerId = Guid.NewGuid(),
            TotalAmount = 99.99m
        };

        // Act
        var orderId = await handler.Handle(command);

        // Assert
        Assert.NotEqual(Guid.Empty, orderId);
        Assert.Single(repository.SavedOrders);
        Assert.Single(publisher.PublishedEvents);
        Assert.IsType<OrderCreatedEvent>(publisher.PublishedEvents[0]);

        var publishedEvent = (OrderCreatedEvent)publisher.PublishedEvents[0];
        Assert.Equal(orderId, publishedEvent.OrderId);
        Assert.Equal(command.CustomerId, publishedEvent.CustomerId);
        Assert.Equal(command.TotalAmount, publishedEvent.TotalAmount);
    }

    [Fact]
    public async Task Handle_ShouldPublishOrderCreatedEvent_WithCorrectAmount()
    {
        var repository = new FakeOrderRepository();
        var publisher = new FakeEventPublisher();
        var handler = new CreateOrderCommandHandler(repository, publisher);
        var command = new CreateOrderCommand { CustomerId = Guid.NewGuid(), TotalAmount = 250.00m };

        await handler.Handle(command);

        var evt = Assert.Single(publisher.PublishedEvents);
        var orderEvent = Assert.IsType<OrderCreatedEvent>(evt);
        Assert.Equal(250.00m, orderEvent.TotalAmount);
    }
}

public sealed class FakeOrderRepository : IOrderRepository
{
    public List<Order> SavedOrders { get; } = new();

    public Task SaveAsync(Order order, CancellationToken cancellationToken = default)
    {
        SavedOrders.Add(order);
        return Task.CompletedTask;
    }

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(SavedOrders.Find(o => o.Id == id));
}

public sealed class FakeEventPublisher : IEventPublisher
{
    public List<object> PublishedEvents { get; } = new();

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        PublishedEvents.Add(@event!);
        return Task.CompletedTask;
    }
}
