using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shared.Events;

namespace Payments.Infrastructure;

public sealed class PaymentRecord
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class PaymentRepository
{
    private readonly List<PaymentRecord> _records = new();

    public Task<PaymentRecord> CreatePaymentAsync(OrderCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        var record = new PaymentRecord
        {
            Id = Guid.NewGuid(),
            OrderId = @event.OrderId,
            CustomerId = @event.CustomerId,
            Amount = @event.TotalAmount,
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        };

        _records.Add(record);
        return Task.FromResult(record);
    }

    public Task<PaymentRecord?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var record = _records.Find(r => r.OrderId == orderId);
        return Task.FromResult(record);
    }

    public Task UpdateStatusAsync(Guid paymentId, string status, CancellationToken cancellationToken = default)
    {
        var record = _records.Find(r => r.Id == paymentId);
        if (record is not null)
            record.Status = status;
        return Task.CompletedTask;
    }
}
