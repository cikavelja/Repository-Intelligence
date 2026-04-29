using System;

namespace Orders.Domain.Entities;

public sealed class Order
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Status { get; private set; } = "Pending";
    public decimal TotalAmount { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Order() { }

    public static Order Create(Guid customerId, decimal totalAmount)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            TotalAmount = totalAmount,
            CreatedAtUtc = DateTime.UtcNow,
            Status = "Pending"
        };
    }

    public void MarkConfirmed()
    {
        if (Status != "Pending")
            throw new InvalidOperationException("Only pending orders can be confirmed.");
        Status = "Confirmed";
    }

    public void MarkCancelled()
    {
        if (Status == "Cancelled")
            throw new InvalidOperationException("Order is already cancelled.");
        Status = "Cancelled";
    }
}
