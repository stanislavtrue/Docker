using Application;
using Domain;
using Infrastructure;
using Presentation;

var builder = WebApplication.CreateBuilder(args);

var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
var redisPort = int.Parse(Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379");

builder.Services.AddSingleton(new RedisCacheService(redisHost, redisPort));
builder.Services.AddSingleton<IProductRepository,ProductRepository>();
builder.Services.AddTransient<CreateProduct>();
builder.Services.AddTransient<ListProducts>();

var app = builder.Build();

app.MapGet("/health", () => new { status = "ok" });
app.MapGet("/products", async (ListProducts uc) =>
{
    var products = await uc.ExecuteAsync();
    return Results.Ok(products);
});

app.MapPost("/products", async (CreateProduct uc, ProductDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
    {
        return Results.BadRequest("Name cannot be empty!");
    }
    if (dto.Price <= 0)
    {
        return Results.BadRequest("Price cannot be less than zero!");
    }
    var id = await uc.ExecuteAsync(dto.Name, dto.Price);
    return Results.Created($"/products/{id}", new { id });
});

app.Run();

record ProductDto(int Id, string Name, decimal Price);
