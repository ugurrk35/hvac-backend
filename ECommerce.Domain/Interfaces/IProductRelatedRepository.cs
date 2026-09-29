using ECommerce.Domain.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface IProductRelatedRepository : IRepository<ProductRelated>
    {
        Task RemoveByProductIdAsync(int productId);
        Task<IReadOnlyList<Product>> GetRelatedProductsAsync(int productId, int count = 4, bool? forCart = null);
    }
}
