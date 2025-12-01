using System.Reflection.Metadata;
using Domain;

namespace Application;

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
