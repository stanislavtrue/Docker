# Layered Monolith
## Що таке Layered Monolith?
Архітектура Layered ділить програму на кілька шарів:
- **Presentation Layer** - відповідає за UI/UX.
- **Application Layer** - організовує бізнес-логіку та взаємодіє з рівнем домену.
- **Domain Layer** - інкапсулює основні бізнес-правила та логіку.
- **Infrastructure Layer** - обробляє стійкість даних, зовнішні API та сторонні інтеграції.

Кожен шар несе певну відповідальність, і шари взаємодіють структуровано.
## Переваги
- **Розділення проблем** - чіткі межі між шарами сприяють кращій організації та ремонтопридатності.
- **Масштабованість** - логічні шари полегшують розширення системи в міру зростання вимог.
- **Тестованість** - кожен шар може бути незалежно протестований.
- **Повторне використання** - бізнес-логіка в області та шарах додатків може бути повторно використана в декількох інтерфейсах користувача.
## Приклад
### Використані технології
- **.NET 9**
- **Podman / Podman-compose**
- **PostgreSQL 16**
- **Redis 7**
- - -
### Структура проєкту
```bash
Monolith/
├── src/
│   ├── Application/
│   │   ├── CreateProduct.cs
│   │   └── ListProducts.cs
│   ├── Domain/
│   │   ├── IProductRepository.cs
│   │   ├── Product.cs
│   │   └── ProductValidator.cs
│   ├── Infrastructure/
│   │   ├── ProductRepository.cs
│   │   └── migrations/
│   │       └── init.sql
│   └── Presentation/
│       ├── ProductController.cs
│       ├── RedisCacheService.cs
│       └── Program.cs
├── tests/
│   ├── ApplicationTests/
│   │   └── CreateProductTest.cs
│   └── DomainTests
│       └── ProductValidatorTest.cs
├── .env.example
├── docker-compose.yml
├── Dockerfile
└── README.md
```
- - - 
### .env.example
```
DB_HOST=db
DB_PORT=5432
DB_NAME=shop
DB_USER=shop
DB_PASS=shop

# API PORT
PORT=8080

# REDIS PORT
REDIS_HOST=redis
REDIS_PORT=6379
```
- - - 
### Dockerfile
>Контейнер запускається від **non-root** юзера для підвищення безпеки.
```Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

RUN useradd -m nonroot
USER nonroot

COPY --from=build /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Presentation.dll"]
```
 - - -
### docker-compose.yml
>Складається з ***API-сервісу*** на ***.NET***, бази даних на ***PostgreSQL***, ***Redis***, та контейнер для запуску тестів.  
***API***, ***Redis*** та ***PostgreSQL*** містять ***healthcheck***. База даних зберігає дані у ***volume*** для збереження інформації між перезапусками контейнерів, і імпортує ***SQL-скрипти*** для ініціалізації з директорії.
```yml
services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports: 
      - "8080:8080"
    env_file:
      - .env.example
    environment:
      - DB_HOST=${DB_HOST}
      - DB_PORT=${DB_PORT}
      - DB_NAME=${DB_NAME}
      - DB_USER=${DB_USER}
      - DB_PASS=${DB_PASS}
      - PORT=${PORT}
    depends_on:
      - db
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 10s 
      retries: 3
  db:
    image: docker.io/library/postgres:16
    restart: always
    environment:
      - POSTGRES_DB=shop
      - POSTGRES_USER=shop
      - POSTGRES_PASSWORD=shop
    volumes:
      - dbdata:/var/lib/postgresql/data
      - ./src/Infrastructure/migrations:/docker-entrypoint-initdb.d
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U shop"]
      interval: 10s 
      retries: 3
  tests:
    image: mcr.microsoft.com/dotnet/sdk:9.0
    working_dir: /app
    volumes:
      - .:/app
    depends_on:
      - db
    command: bash -c "dotnet test tests/domain/DomainTests.csproj && dotnet test tests/application/ApplicationTests.csproj"
  redis:
    image: docker.io/library/redis:7
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      retries: 3
      
volumes:
  dbdata:
```
- - -
### Product.cs
>Клас `Product` описує товар.  
- `Id` - унікальний **id** товару.  
- `Name` - назва товару.  
- `Price` - ціна.  

>Усі властивості доступні лише для читання. Створення об'єкта здійснюється через конструктор.
```csharp
public class Product 
{
    public int Id { get; }
    public string Name { get; }
    public decimal Price { get;}
    public Product(int Id, string Name, decimal Price)
    {
        this.Id = Id;
        this.Name = Name;
        this.Price = Price;
    }
}
```
- - -
### ProductValidator.cs
>Клас `ProductValidator` відповідає за перевірку коректності даних об'єкта `Product`.  
```csharp
public class ProductValidator
{
    public void Validate(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
            throw new ValidationException("Name cannot be empty!");
        if (product.Name.Length > 100)
            throw new ValidationException("Name is too long!");
        if (product.Price <= 0)
            throw new ValidationException("Price cannot be less than zero!");
    }
}
```
- - -
### IProductRepository.cs
>`IProductRepository` - це інтерфейс який описує два асинхронних методи.
1. `Task<int> CreateAsync(Product product)` - додає новий товар у сховище та повертає його **id**.
2. `Task<IReadOnlyList<Product>> ListAsync()` - повертає колекцію всіх товарів.
```csharp
public interface IProductRepository
{
    Task<int> CreateAsync(Product product);
    Task<IReadOnlyList<Product>> ListAsync();
}
```
- - -
### CreateProduct.cs
>`CreateProduct` - відповідає за створення нового товару в системі.  
- `ExecuteAsync` - отримує назву та ціну товару, потім створює об'єкт `Product`, **id** буде згенероване під час збереження, далі виконується перевірка валідності даних, якщо дані коректні то передає їх до `IProductRepository.CreateAsync()`. 
```csharp
public class CreateProduct
{
    private readonly IProductRepository repo;
    private readonly ProductValidator validator;
    public CreateProduct(IProductRepository repo) 
    {
        this.repo = repo;
        validator = new ProductValidator();
    }

    public async Task<int> ExecuteAsync(string name, decimal price)
    {
        var product = new Product(0, name, price);
        validator.Validate(product);
        return await repo.CreateAsync(product);
    }
}
```
- - -
### ListProducts.cs
>`ListProducts` - ***use-case*** клас 
- `Task<IReadOnlyList<Product>> ExecuteAsync()` - асинхронно виконує запит до репозиторію, і повертає список продуктів.
```csharp
public class ListProducts 
{
    private readonly IProductRepository repo;
    public ListProducts(IProductRepository repo) => this.repo = repo;
    public Task<IReadOnlyList<Product>> ExecuteAsync() => repo.ListAsync();
}
```
- - -
### ProductRepository.cs
>`ProductRepository` - працює з ***PostgreSQL*** через ***Dapper***
- `connStr` - зберігає рядок підключення до БД.
- `CreateAsync` - виконує `INSERT` через ***Dapper***, та повертає **Id** створеного продукту.
- `ListAsync` - виконує `SELECT` всих продуктів, і потів результат перетворюється на `List` і повертається як `IReadOnlyList<Product>`.
```csharp
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
```
- - - 
### RedisCacheService.cs
>`RedisCacheService` - сервіс для кещування через ***Redis***.  
- `SetAsync` - зберігає об'єкт у ***Redis***:  
  1. Серіалізує об'єкт у ***JSON***;
  2. Записує у ***Redis** під ключем `key`;
  3. `ttl` - час життя кешу.
- `GetAsync` - читає з ***Redis***:  
  1. Береться значення за ключем;
  2. Якщо ключа не існує, повертається `default`;
  3. Якщо значення є, то десеріалізує його.
```csharp
public class RedisCacheService
{
    private readonly IDatabase db;

    public RedisCacheService(string host, int port)
    {
        var conn = ConnectionMultiplexer.Connect($"{host}:{port}");
        db = conn.GetDatabase();
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
    {
        var json = JsonSerializer.Serialize(value);
        await db.StringSetAsync(key, json, ttl);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await db.StringGetAsync(key);
        if(value.IsNullOrEmpty)
            return default;
        return JsonSerializer.Deserialize<T>(value!);
    }
}
```
- - -
### ProductsController.cs
>ProductsController` - він забезпечує отримання списку товарів з кешуванням у ***Redis*** для підвищення продуктивності.  
- `Get()`
  1. Використовує ключ `product_all` для спроби отримати список продуктів із кешу;
  2. Якщо кеш не порожній, повертає дані без звернення до БД.
  3. Якщо кеш порожній, викликає `listProducts.ExecuteAsync()` для отримання всіх товарів, зберігає отримані дані у ***Redis*** на 30 секунд, і повертає список.
```csharp
[ApiController]
[Route("[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ListProducts listProducts;
    private readonly RedisCacheService cache;

    public ProductsController(ListProducts listProducts, RedisCacheService cache)
    {
        this.listProducts = listProducts;
        this.cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var cacheKey = "products_all";
        var cached = await cache.GetAsync<List<ProductDto>>(cacheKey);
        if(cached != null)
            return Ok(cached);
        
        var products = await listProducts.ExecuteAsync();
        var dtos = products.Select(p => new ProductDto(p.Id, p.Name, p.Price)).ToList();

        await cache.SetAsync(cacheKey, dtos, TimeSpan.FromSeconds(30));
        return Ok(dtos);
    }
}
```
- - -
### Program.cs
```csharp
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
```
## Тести
### Domain
>В цьому тесті виконується перевірка на порожню назву продукту, та продукт з від'ємною ціною.
```csharp
public class ProductValidatorTest
{
    [Fact]
    public void Product_WithEmptyName()
    {
        var validator = new ProductValidator();
        var product = new Product(Id: 1, Name: "", Price: 10);
        Assert.Throws<ValidationException>(() => validator.Validate(product));
    }
    [Fact]
    public void Product_WithNegativePrice()
    {
        var validator = new ProductValidator();
        var product = new Product(Id: 1, Name: "Name", Price: -1234);
        Assert.Throws<ValidationException>(() => validator.Validate(product));
    }
}
```
- - - 
## Application
>В цьому тесті виконується перевірка, що при створення продукту з порожньою назвою викликається виняток, і перевірити успішне створення продукту з коректними даними.
```csharp
public class CreateProductTest
{
    [Fact]
    public async Task CreateProduct_InvalidName()
    {
        var repoMock = new Mock<IProductRepository>();
        var useCase = new CreateProduct(repoMock.Object);

        await Assert.ThrowsAsync<ValidationException>(() => useCase.ExecuteAsync("", 100));
    }

    [Fact]
    public async Task CreateProduct_ValidData()
    {
        var repoMock = new Mock<IProductRepository>();
        repoMock.Setup(r => r.CreateAsync(It.IsAny<Product>())).ReturnsAsync(1);

        var useCase = new CreateProduct(repoMock.Object);
        var id = await useCase.ExecuteAsync("Name", 999m);

        Assert.Equal(1, id);
        repoMock.Verify(r => r.CreateAsync(It.IsAny<Product>()), Times.Once);
    }
}
```
- - - 
## Перевірка
- **Запуск**  
![](https://github.com/user-attachments/assets/be8aa105-5b81-42ff-88fd-f818452e682f)
![](https://github.com/user-attachments/assets/a44933e9-07bf-4a85-8321-b498477ac024)
![](https://github.com/user-attachments/assets/54c3ce6c-79bc-42dc-86fa-db996ffb14dd)
![](https://github.com/user-attachments/assets/b1084c68-85ef-40e1-9db6-eda14df55254)
- `curl localhost:8080/health`    
![](https://github.com/user-attachments/assets/522e5ed4-5c0b-4d53-9ac3-5bbb36a0ff07)
- `curl -X POST localhost:8080/products -H 'Content-Type: application/json' -d '{"name":"Desk","price":199.9}'`  
![](https://github.com/user-attachments/assets/e5d8dc41-425c-46a7-96b3-6aad275a60e2)
- `curl localhost:8080/products`  
![](https://github.com/user-attachments/assets/c607eb7a-35f5-4a2e-954a-6d24f6167f30)
