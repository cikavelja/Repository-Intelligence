using System;
using System.Threading;
using System.Threading.Tasks;
using Orders.Domain.Entities;

namespace Orders.Application.Commands.CreateOrder;

public sealed class CreateOrderCommand
{
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
}

public sealed class CreateOrderCommandHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEventPublisher _eventPublisher;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IEventPublisher eventPublisher)
    {
        _orderRepository = orderRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<Guid> Handle(CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = Order.Create(command.CustomerId, command.TotalAmount);

        await _orderRepository.SaveAsync(order, cancellationToken);

        // Publish integration event so downstream services (Payments) can react
        var @event = new OrderCreatedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            CreatedAtUtc = order.CreatedAtUtc
        };

        await _eventPublisher.PublishAsync(@event, cancellationToken);

        return order.Id;
    }
}

public interface IOrderRepository
{
    Task SaveAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;
}
