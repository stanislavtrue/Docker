namespace db;

using Microsoft.Extensions.Configuration;
using Npgsql;
using Dapper;
using Ports;
using Domain;
using System.Dynamic;

public class PostgresOrderRepository : IOrderRepository
{
    private readonly string connStr;
    public PostgresOrderRepository(IConfiguration config)
    {
        var cs = config.GetConnectionString("db") ?? throw new Exception("Missing connection string 'db'");
        connStr = cs;
    }

    public async Task<OrderId> CreateAsync(Order order, CancellationToken ct = default)
    {
        const string sql = @"INSERT INTO orders(id, sku, qty) VALUES(@Id, @Sku, @Qty);";
        await using var conn = new NpgsqlConnection(connStr);
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = order.Id.value, order.Sku, order.Qty }, cancellationToken: ct));
        return order.Id;
    }

    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default)
    {
        const string sql = "SELECT id, sku, qty FROM orders WHERE id = @Id LIMIT 1";
        await using var conn = new NpgsqlConnection(connStr);
        var row = await conn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(sql, new { Id = id.value }, cancellationToken: ct));
        if (row == null) return null;
        return new Order(new OrderId((string)row.id), (string)row.sku, (int)row.qty);
    }
}
