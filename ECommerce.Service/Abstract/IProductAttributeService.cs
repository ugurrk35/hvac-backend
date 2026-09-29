using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Abstract
{
    public interface IProductAttributeService : IService<ProductAttribute>
    {
        Task<IEnumerable<ProductAttribute>> GetAttributesWithValuesAsync();
        Task<ProductAttribute?> GetAttributeWithValuesByIdAsync(int id);
        Task<IEnumerable<ProductAttribute>> GetPersonalizationAttributesAsync();
    }
}
