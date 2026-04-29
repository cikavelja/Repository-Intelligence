using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Shared.Events;

namespace Payments.API.Consumers;

/// <summary>
/// Background service that listens for OrderCreatedEvent messages from the
/// Service Bus and triggers payment processing.
/// </summary>
public sealed class OrderCreatedEventListenerService : BackgroundService
{
    private readonly IMessageBusReceiver _messageBusReceiver;
    private readonly IPaymentProcessor _paymentProcessor;

    public OrderCreatedEventListenerService(
        IMessageBusReceiver messageBusReceiver,
        IPaymentProcessor paymentProcessor)
    {
        _messageBusReceiver = messageBusReceiver;
        _paymentProcessor = paymentProcessor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var message = await _messageBusReceiver.ReceiveAsync("ordercreated-event", stoppingToken);
            if (message is null)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                continue;
            }

            try
            {
                var @event = JsonSerializer.Deserialize<OrderCreatedEvent>(message);
                if (@event is not null)
                    await _paymentProcessor.ProcessPaymentForOrderAsync(@event, stoppingToken);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[OrderCreatedEventListenerService] Error processing message: {ex.Message}");
            }
        }
    }
}

public interface IMessageBusReceiver
{
    Task<string?> ReceiveAsync(string topic, CancellationToken cancellationToken = default);
}

public interface IPaymentProcessor
{
    Task ProcessPaymentForOrderAsync(OrderCreatedEvent @event, CancellationToken cancellationToken = default);
}
