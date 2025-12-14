using Domain;
using Ports;
using Usecases;
using db;
using file;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
var repoAdapter = Environment.GetEnvironmentVariable("REPO_ADAPTER") ?? "db";

if(repoAdapter == "db")
    builder.Services.AddSingleton<IOrderRepository, PostgresOrderRepository>();
else if(repoAdapter == "file")
    builder.Services.AddSingleton<IOrderRepository, CsvOrderRepository>();
else 
    throw new Exception("Unknown REPO_ADAPTER. Use 'db' or 'file'.");

builder.Services.AddScoped<CreateOrder>();
builder.Services.AddScoped<GetOrder>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/orders", async (CreateOrder uc, CreateOrderDto dto) => 
{
    try
    {
        var id = await uc.ExecuteAsync(dto.Sku, dto.Qty);
        return Results.Created($"/orders/{id.value}", new { id = id.value });
    }
    catch (System.ComponentModel.DataAnnotations.ValidationException ve)
    {
        return Results.BadRequest(new { error = ve.Message });
    }
});
app.MapGet("/orders/{id}", async (GetOrder uc, string id) =>
{
    var oid = new OrderId(id);
    var order = await uc.ExecuteAsync(oid);
    if (order == null) return Results.NotFound();
    return Results.Ok(new OrderDto(order.Id.value, order.Sku, order.Qty));
});

app.Run();

public record CreateOrderDto(string Sku, int Qty);
public record OrderDto(string Id, string Sku, int Qty);
