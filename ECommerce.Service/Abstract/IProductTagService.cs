using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Abstract
{
    public interface IProductTagService : IService<ProductTag>
    {
        Task<ProductTag?> GetTagWithProductsByIdAsync(int id);
    }
}
