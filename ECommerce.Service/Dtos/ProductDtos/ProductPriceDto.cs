using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductDtos
{
    public class ProductPriceDto
    {
        public int ProductId { get; set; }  // zorunlu
        public int? ProductAttributeCombinationId { get; set; }  // varyant fiyatı, opsiyonel
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string Currency { get; set; } = "TRY";
        public int? CustomerGroupId { get; set; }  // opsiyonel
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }


}
