using ECommerce.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface IProductAttributeCombinationValueRepository : IRepository<ProductAttributeCombinationValue>
    {
        Task RemoveByProductIdAsync(int productId);
        Task<IEnumerable<ProductAttributeCombinationValue>> GetValuesByCombinationIdAsync(int combinationId);
        Task<ProductAttributeCombinationValue?> GetValueWithDetailsAsync(int id);
    }
}
