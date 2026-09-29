using ECommerce.Domain.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface IProductPriceRepository : IRepository<ProductPrice>
    {
       Task<List<Product>> GetProductsByIdsOrCategoryAsync(List<int> productIds, int? categoryId);
        Task<ProductPrice> GetLatestPriceAsync(int productId, int? productAttributeCombinationId = null, int? customerGroupId = null);
        Task RemoveByProductIdAsync(int productId);
        
    }
}
