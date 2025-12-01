namespace Domain;

public interface IProductRepository
{
    Task<int> CreateAsync(Product product);
    Task<IReadOnlyList<Product>> ListAsync();
}
