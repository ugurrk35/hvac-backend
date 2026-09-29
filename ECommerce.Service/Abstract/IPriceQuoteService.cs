using ECommerce.Domain.Dtos;
using ECommerce.Domain.Entity;

namespace ECommerce.Service.Abstract
{
    public interface IPriceQuoteService
    {
        Task<CartQuoteDto> GetCartQuoteAsync(int cartId, int? shippingMethodId = null, string? couponCode = null, string? city = null, string? district = null);
        decimal GetEffectiveUnitPrice(Product product, ProductAttributeCombination? combination = null);
    }
}
