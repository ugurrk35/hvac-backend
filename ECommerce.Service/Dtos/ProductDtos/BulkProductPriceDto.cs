using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductDtos
{
    public class BulkProductPriceDto
    {
        // Filtreleme
        public int? CategoryId { get; set; }                       // Kategori bazlı fiyat ekleme
        public List<int>? ProductIds { get; set; }                 // Belirli ürünler
        public int? CustomerGroupId { get; set; }                 // Bayi, B2B, Standart vb.
        public int? ProductAttributeCombinationId { get; set; }   // Varyant bazlı fiyat (opsiyonel)

        // Fiyat Bilgisi
        public decimal Price { get; set; }                        // Temel fiyat
        public decimal? DiscountPrice { get; set; }               // İndirimli fiyat
        public string Currency { get; set; } = "TRY";             // Para birimi

        // Tarih Aralığı
        public DateTime? StartDate { get; set; }                  // Geçerlilik başlangıcı
        public DateTime? EndDate { get; set; }                    // Geçerlilik sonu

        // Yüzdelik artış / azalış (örn: %10 arttır -> 10, %5 düşür -> -5)
        public decimal? PercentageChange { get; set; }

        // Opsiyonel: Sadece aktif fiyatları hedefleme
        public bool OnlyActivePrices { get; set; } = true;
    }
}
