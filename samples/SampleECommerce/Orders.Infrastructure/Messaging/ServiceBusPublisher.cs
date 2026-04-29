using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Orders.Application.Commands.CreateOrder;

namespace Orders.Infrastructure.Messaging;

/// <summary>
/// Publishes integration events to Azure Service Bus topics.
/// Implements IEventPublisher from Orders.Application.
/// </summary>
public sealed class ServiceBusPublisher : IEventPublisher
{
    private readonly IServiceBusClient _serviceBusClient;

    public ServiceBusPublisher(IServiceBusClient serviceBusClient)
    {
        _serviceBusClient = serviceBusClient;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        var typeName = typeof(T).Name;
        var topic = ToTopicName(typeName);
        var json = JsonSerializer.Serialize(@event);

        await _serviceBusClient.SendAsync(topic, json, cancellationToken);
    }

    private static string ToTopicName(string typeName) =>
        typeName.ToLowerInvariant().Replace("event", "-event");
}

public interface IServiceBusClient
{
    Task SendAsync(string topic, string messageBody, CancellationToken cancellationToken = default);
    Task<string?> ReceiveAsync(string topic, CancellationToken cancellationToken = default);
}
