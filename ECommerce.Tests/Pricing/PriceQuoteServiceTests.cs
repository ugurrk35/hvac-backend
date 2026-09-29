using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Service.Concrete;
using Moq;
using Xunit;
using FluentAssertions;

namespace ECommerce.Tests.Pricing;

public class PriceQuoteServiceTests
{
    private static PriceQuoteService CreateService() => new(
        Mock.Of<IShoppingCartRepository>(),
        Mock.Of<IShippingMethodRepository>(),
        null!);

    [Theory]
    [InlineData(100, 80, 80)]
    [InlineData(100, 0, 100)]
    [InlineData(100, 100, 100)]
    [InlineData(100, 120, 100)]
    public void GetEffectiveUnitPrice_UsesOnlyAValidDiscount(decimal basePrice, decimal discountPrice, decimal expected)
    {
        var product = new Product { BasePrice = basePrice, DiscountPrice = discountPrice };
        CreateService().GetEffectiveUnitPrice(product).Should().Be(expected);
    }

    [Fact]
    public void GetEffectiveUnitPrice_RejectsNullProduct()
    {
        Assert.Throws<ArgumentNullException>(() => CreateService().GetEffectiveUnitPrice(null!));
    }
}
