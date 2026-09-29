using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Abstract
{
    public interface IProductAttributeCombinationService : IService<ProductAttributeCombination>
    {
        Task<IEnumerable<ProductAttributeCombination>> GetCombinationsByProductIdAsync(int productId);
        Task<ProductAttributeCombination?> GetCombinationWithValuesAsync(int id);
        Task<ProductAttributeCombination?> GetCombinationBySkuAsync(string sku);
        Task<bool> IsSkuUniqueAsync(string sku, int? excludeId = null);
        Task<List<string>> ValidateCombinationAsync(ProductAttributeCombination entity);
    }
}
