using ECommerce.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface IProductAttributeRepository : IRepository<ProductAttribute>
    {
        Task<IEnumerable<ProductAttribute>> GetAttributesWithValuesAsync();
        Task<ProductAttribute?> GetAttributeWithValuesByIdAsync(int id);
        Task<IEnumerable<ProductAttribute>> GetPersonalizationAttributesAsync();
    }
}
