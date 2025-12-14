namespace Ports;

using Domain;
public interface IOrderRepository 
{
    Task<OrderId> CreateAsync(Order order, CancellationToken ct = default);
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default);
}
