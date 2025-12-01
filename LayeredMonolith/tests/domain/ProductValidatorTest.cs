using Domain;
using System.ComponentModel.DataAnnotations;

namespace DomainTests;

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
