namespace Usecases;

using Domain;
using Ports;
public class GetOrder
{
    private readonly IOrderRepository repo;
    public GetOrder(IOrderRepository repo) => this.repo = repo;
    public Task<Order?> ExecuteAsync(OrderId id, CancellationToken ct = default) => repo.GetByIdAsync(id, ct);
}
