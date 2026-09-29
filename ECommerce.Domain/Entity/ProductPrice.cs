using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class ProductPrice: AuditableEntity
    {
        public int ProductId { get; set; }
        public int? ProductAttributeCombinationId { get; set; } // varyant fiyatı
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string Currency { get; set; } = "TRY";

        public int? CustomerGroupId { get; set; }
        public CustomerGroup CustomerGroup { get; set; } // Bayi, B2B, Standart
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Product Product { get; set; }
    }

}
