# Hexagonal
## Що таке Hexagonal?
**Hexagonal Architecture**, також відома як **Ports and Adapters Architecture** - це шаблон проєктування, який зосереджується на тому, щоб зробити програмне забезпечення гнучким і адаптивним, відокремлюючи основну логіку від зовнішніх залежностей, таких як бази даних або інтерфейси користувача. У цьому підході основна система спілкується із зовнішнім світом за допомогою чітко визначених «портових» інтерфейсів, тоді як «адаптери» обробляють деталі реалізації цих портів. Це дозволяє логіці ядра залишатися незмінною, навіть якщо зовнішні компоненти потрібно замінити.
## Переваги
- **Розділення проблем** - чітко відокремлює основну бізнес-логіку від зовнішніх систем, таких як бази даних, API або інтерфейси користувача. Це робить основну логіку незалежною від того, як дані вводяться, зберігаються або відображаються, дозволяючи більш чистий модульний код.
- **Гнучкість і адаптивність** - архітектура забезпечує гнучкість в інтеграції нових технологій або заміни старих. Наприклад, ви можете перейти з бази даних ***SQL*** на базу даних ***NoSQL*** або з ***REST API*** на ***API GraphQL***, просто змінивши відповідний адаптер, не торкаючись основної логіки.
- **Покращене тестування** - оскільки основна логіка ізольована від зовнішніх систем, її можна легко перевірити.
- **Підтримуваність** - кожен компонент, будь то основна логіка, адаптери або порти, має чітко визначену роль і може підтримуватися окремо.
- **Розширюваність** - нові функції або компоненти можуть бути додані в систему без зміни існуючої логіки ядра. 
## Приклад
### Використані технології
- **.NET 9**
- **Podman / Podman-compose**
- **PostgreSQL 16**
---
### Структура проєкту
```md
Hexagonal/
├── core/
│   ├── Domain/
│   │   └── Order.cs
│   ├── Ports/
│   │   └── IOrderRepository.cs
│   └── Usecases/
│       ├── CreateOrder.cs
│       └── GetOrder.cs
├── adapters/
│   ├── DB/
│   │   └── PostgresOrderRepository.cs
│   ├── File/
│   │   └── CsvOrderRepository.cs
│   └── Http/
│       └── Program.cs
├── Infrastructure/
│   └── migrations/
│       └── init.sql
├── tests/
│   └── CreateOrderTests/
│       └── CreateOrderTests.cs
├── .env.example
├── docker-compose.yml
├── Dockerfile
└── README.md
```
---
### .env.example
```env
PORT=8081

# "db" or "file"
REPO_ADAPTER=db

# Postgres 
DB_HOST=db
DB_PORT=5432
DB_NAME=orders
DB_USER=postgres
DB_PASS=1234

# CSV
CSV_PATH=/data/orders.csv
```
---
### Dockerfile
```Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /core

COPY . .
RUN dotnet publish adapters/Http/http.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /app .

ENV PORT=8081
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "http.dll"]
```
---
### docker-compose.yml
```yml
services:
  api:
    build: .
    env_file:
      - .env
    ports:
      - "${PORT}:8080"
    depends_on:
      - db
    volumes:
      - ./infrastructure/migrations:/migrations:ro
      - csv_data:/data
  db:
    image: postgres:16
    env_file:
      - .env
    environment:
      - POSTGRES_DB=${DB_NAME}
      - POSTGRES_USER=${DB_USER}
      - POSTGRES_PASSWORD=${DB_PASS}
    ports:
      - "${DB_PORT}:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./infrastructure/migrations:/docker-entrypoint-initdb.d:ro
volumes:
  postgres_data:
  csv_data:
```
---
### Order.cs
> Визначає доменно-орієнтовані типи для роботи із замовленнями, використовуючи `record` у ***C#***. Такий підхід підвищує типобезпеку, читабельність і відповідність принципам ***DDD (Domain-Driven Design)***.
```csharp
public sealed record OrderId(string value)
{
    public static OrderId New() => new(Guid.NewGuid().ToString("N"));
}
public sealed record Order(OrderId Id, string Sku, int Qty);
```
---
### IOrderRepository.cs
```csharp
public interface IOrderRepository 
{
    Task<OrderId> CreateAsync(Order order, CancellationToken ct = default);
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default);
}
```
---
### CreateOrder.cs
> `CreateOrder` - це ***Use case***, який інкапсулює сценарій створення замовлення, і відповідає за валідацію даних, створення об'єкта `Order`, та збереження замовлення через репозиторій.
``` csharp
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
```
---
### GetOrder.cs
> `GetOrder` - це ***Use case*** для отримання замовлення за індентифікатором.
```csharp
public class GetOrder
{
    private readonly IOrderRepository repo;
    public GetOrder(IOrderRepository repo) => this.repo = repo;
    public Task<Order?> ExecuteAsync(OrderId id, CancellationToken ct = default) => repo.GetByIdAsync(id, ct);
}
```
---
### PostgresOrderRepository.cs
```csharp
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
```
---
### CsvOrderRepository.cs
```csharp
public class CsvOrderRepository : IOrderRepository
{
    private readonly string path;
    private readonly  SemaphoreSlim _lock = new(1, 1);
    
    public CsvOrderRepository(IConfiguration config)
    {
        path = config["CSV_PATH"] ?? throw new Exception("CSV_PATH is required");
        var dir = Path.GetDirectoryName(path);
        if(!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);
        if(!File.Exists(path)) 
            File.WriteAllText(path, "id,sku,qty\n");
    }

    public async Task<OrderId> CreateAsync(Order order, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var line = $"{order.Id.value},{Escape(order.Sku)},{order.Qty}\n";
            await File.AppendAllTextAsync(path, line, ct);
            return order.Id;
        }
        finally { _lock.Release(); }
    }

    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var lines = await File.ReadAllLinesAsync(path, ct);
            for(int i = 1; i < lines.Length; i++)
            {
                var parts = SplitCsvLine(lines[i]);
                if(parts.Length < 3) continue;
                if(parts[0] == id.value)
                    return new Order(new OrderId(parts[0]), parts[1], int.Parse(parts[2], CultureInfo.InvariantCulture));
            }
            return null;
        }
        finally { _lock.Release(); }
    }

    private static string Escape(string s) => s.Replace("\"", "\"\"");
    private static string[] SplitCsvLine(string line) => line.Split(',', StringSplitOptions.None).Select(p => p.Trim()).ToArray();
}
```
