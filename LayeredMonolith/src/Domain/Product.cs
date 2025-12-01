using System.ComponentModel.DataAnnotations;

namespace Domain;

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
