namespace CreateOrderTests;

using Moq;
using Xunit;
using Domain;
using Usecases;
using Ports;
using System.ComponentModel.DataAnnotations;

public class CreateOrderTests
{
    [Fact]
    public async Task ThrowsWhenQtyInvalid()
    {
        var repo = new Mock<IOrderRepository>();
        var uc = new CreateOrder(repo.Object);
        await Assert.ThrowsAsync<ValidationException>(() => uc.ExecuteAsync("ABC", 0));
    }

    [Fact]
    public async Task CallsRepo_On_ValidInput()
    {
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.CreateAsync(It.IsAny<Order>(), default)).ReturnsAsync((Order o, System.Threading.CancellationToken _) => o.Id);
        var uc = new CreateOrder(repo.Object);
        var id = await uc.ExecuteAsync("ABC", 2);
        Assert.False(string.IsNullOrWhiteSpace(id.value));
        repo.Verify(r => r.CreateAsync(It.IsAny<Order>(), default), Times.Once);
    }
}
