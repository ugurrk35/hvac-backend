using ECommerce.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface IProductAttributeValueRepository : IRepository<ProductAttributeValue>
    {
        Task<IEnumerable<ProductAttributeValue>> GetValuesByAttributeIdAsync(int attributeId);
        Task<ProductAttributeValue?> GetValueWithAttributeAsync(int id);
        Task RemoveByProductIdAsync(int productId);
    }
}
