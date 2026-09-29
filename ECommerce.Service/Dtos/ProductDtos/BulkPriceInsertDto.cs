using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductDtos
{
    public class BulkPriceInsertDto
    {
        public int? CategoryId { get; set; }                  // Kategori bazlı
        public int? ProductId { get; set; }                   // Ürün bazlı
        public int? ProductAttributeCombinationId { get; set; } // Varyant bazlı
        public int? CustomerGroupId { get; set; }            // Müşteri grubu bazlı

        public decimal Price { get; set; }                   // Yeni fiyat
        public decimal? DiscountPrice { get; set; }          // İndirimli fiyat
        public string Currency { get; set; } = "TRY";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
