namespace file;

using Domain;
using Ports;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.ComponentModel.DataAnnotations;

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
