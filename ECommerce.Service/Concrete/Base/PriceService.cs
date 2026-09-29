using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Concrete.Base
{
    public class PriceService : IPriceService
    {
        public decimal GetPrice(Product product, int? combinationId, int? customerGroupId)
        {
            // Varsayılan olarak en ucuz aktif fiyat (eğer hiç fiyat bulunmazsa 0 döner)
            decimal price = 0;

            var priceEntry = product.ProductPrices
                .Where(p => p.ProductAttributeCombinationId == combinationId
                         && (p.CustomerGroupId == customerGroupId || p.CustomerGroupId == null) // tüm müşteriler veya belirli grup
                         && (!p.StartDate.HasValue || p.StartDate <= DateTime.Now)
                         && (!p.EndDate.HasValue || p.EndDate >= DateTime.Now)
                         && p.IsActive)
                .OrderByDescending(p => p.CustomerGroupId.HasValue) // özel fiyat önce gelsin
                .ThenByDescending(p => p.StartDate) // en güncel fiyat
                .FirstOrDefault();

            if (priceEntry != null)
                price = priceEntry.DiscountPrice ?? priceEntry.Price;

            return price;
        }

    }
}