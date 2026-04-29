# SampleECommerce

This is a fake e-commerce solution used to demonstrate the Repository Intelligence indexer.

## Structure

- `Orders.Domain` — Order domain entity
- `Orders.Application` — CreateOrder command handler and interfaces
- `Orders.Infrastructure` — Service Bus publisher (messaging)
- `Shared.Events` — Shared integration events (OrderCreatedEvent)
- `Payments.API` — Order created event listener / payment consumer
- `Payments.Infrastructure` — Payment repository
- `Tests` — Unit tests for order creation flow
- `Catalog.API` — Unrelated product controller (for filter testing)
- `CRM.Application` — Unrelated customer profile service (for filter testing)
- `Inventory.Application` — Unrelated inventory report generator (for filter testing)

## Demo Issue

```
Payment is not triggered after order creation.
```

Expected relevant files recommended by the analyzer:

1. `Orders.Application/Commands/CreateOrder/CreateOrderCommandHandler.cs`
2. `Shared/Events/OrderCreatedEvent.cs`
3. `Orders.Infrastructure/Messaging/ServiceBusPublisher.cs`
4. `Payments.API/Consumers/OrderCreatedEventListenerService.cs`
5. `Payments.Infrastructure/PaymentRepository.cs`
6. `Tests/CreateOrderCommandHandlerTests.cs`
