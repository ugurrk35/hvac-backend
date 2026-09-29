using ECommerce.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Abstract.Base
{
    public interface IPriceService
    {
        decimal GetPrice(Product product, int? combinationId, int? customerGroupId);
    }

}
