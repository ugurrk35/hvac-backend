using ECommerce.Domain.Entity;

namespace ECommerce.Domain.Interfaces
{
    public interface IShippingMethodRepository : IRepository<ShippingMethod>
    {
        Task<IReadOnlyList<ShippingMethod>> GetActiveAsync();
    }
}
