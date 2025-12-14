namespace Usecases;

using System.ComponentModel.DataAnnotations;
using Domain;
using Ports;

public class CreateOrder 
{
    private readonly IOrderRepository repo;
    public CreateOrder(IOrderRepository repo) => this.repo = repo;

    public async Task<OrderId> ExecuteAsync(string sku, int qty, CancellationToken ct = default)
    {
        if(string.IsNullOrWhiteSpace(sku))
            throw new ValidationException("Sku cannot be empty!");
        if(qty <= 0)
            throw new ValidationException("Qty cannot be below than zero!");
        var order = new Order(OrderId.New(), sku, qty);
        return await repo.CreateAsync(order, ct);
    }
}
