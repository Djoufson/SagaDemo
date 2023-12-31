using SagaDemo.OrderService.Events;

namespace SagaDemo.OrderService.Services.Orchestrator;

public interface IOrchestratorClient
{
    Task RaiseOrderPlacedEvent(OrderPlaced @event);
    Task RaiseOrderSuccessAsync(OrderSuccess @event);
    Task RaiserOrderUnDoneAsync(OrderUnDone @event);
}
