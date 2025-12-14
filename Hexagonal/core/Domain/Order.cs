namespace Domain;

public sealed record OrderId(string value)
{
    public static OrderId New() => new(Guid.NewGuid().ToString("N"));
}
public sealed record Order(OrderId Id, string Sku, int Qty);
