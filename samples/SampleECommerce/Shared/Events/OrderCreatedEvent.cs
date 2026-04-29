using System;

namespace Shared.Events;

/// <summary>
/// Integration event raised when an order is successfully created.
/// Published by Orders service, consumed by Payments service.
/// </summary>
public sealed class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
