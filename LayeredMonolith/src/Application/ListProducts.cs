using Domain;
namespace Application;

public class ListProducts 
{
    private readonly IProductRepository repo;
    public ListProducts(IProductRepository repo) => this.repo = repo;
    public Task<IReadOnlyList<Product>> ExecuteAsync() => repo.ListAsync();
}
