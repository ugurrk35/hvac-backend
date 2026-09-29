using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Abstract
{
    public interface IProductAttributeCombinationValueService : IService<ProductAttributeCombinationValue>
    {
        Task<IEnumerable<ProductAttributeCombinationValue>> GetValuesByCombinationIdAsync(int combinationId);
        Task<ProductAttributeCombinationValue?> GetValueWithDetailsAsync(int id);
    }

}
