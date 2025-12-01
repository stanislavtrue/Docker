using Microsoft.Extensions.Configuration;
using Npgsql;
using Dapper;
using Domain;

namespace Infrastructure;

public class ProductRepository : IProductRepository 
{
    private readonly string connStr;
    public ProductRepository(IConfiguration config)
    {
        connStr = config.GetConnectionString("db") ?? throw new Exception("Missing connection string");
    }
    public async Task<int> CreateAsync(Product product)
    {
        const string sql = @"
            INSERT INTO products(name, price)
            VALUES (@Name, @Price)
            RETURNING id;";
        await using var conn = new NpgsqlConnection(connStr);
        return await conn.ExecuteScalarAsync<int>(sql, new {product.Name, product.Price});
    }
    public async Task<IReadOnlyList<Product>> ListAsync()
    {
        const string sql = "SELECT id, name, price FROM products";
        await using var conn = new NpgsqlConnection(connStr);
        return (await conn.QueryAsync<Product>(sql)).ToList();
    }
}
