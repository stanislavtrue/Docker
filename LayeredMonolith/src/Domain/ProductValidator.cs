using System.ComponentModel.DataAnnotations;

namespace Domain;

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
