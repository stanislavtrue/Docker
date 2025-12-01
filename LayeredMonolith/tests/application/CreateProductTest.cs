using Moq;
using Application;
using Domain;
using System.ComponentModel.DataAnnotations;

namespace ApplicationTests;

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
