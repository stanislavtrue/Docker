using Application;
using Microsoft.AspNetCore.Mvc;

namespace Presentation;

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
