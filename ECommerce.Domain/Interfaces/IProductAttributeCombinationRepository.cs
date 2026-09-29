using ECommerce.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface IProductAttributeCombinationRepository : IRepository<ProductAttributeCombination>
    {
        Task RemoveByProductIdAsync(int productId);
        Task<IEnumerable<ProductAttributeCombination>> GetCombinationsByProductIdAsync(int productId);
        Task<ProductAttributeCombination?> GetCombinationWithValuesAsync(int id);
        Task<ProductAttributeCombination?> GetCombinationBySkuAsync(string sku);
        Task<IEnumerable<ProductAttributeCombination>> GetCombinationsWithValuesAsync();
    }
}
