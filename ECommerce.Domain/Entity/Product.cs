using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class Product : AuditableEntity
    {
        // Temel Ürün Bilgileri
        public string Name { get; set; }                             // Ürün adı (H1)
        public string Slug { get; set; }                             // URL dostu ad
        public string SKU { get; set; }                              // Stok kodu
        public string ShortDescription { get; set; }                 // Kısa açıklama (meta için)
        public string Description { get; set; }                      // Detaylı açıklama (HTML içerebilir)
        public string TechnicalDetails { get; set; } = string.Empty; // Ürüne özel teknik bilgi sekmesi
        public string DeliveryInstallationDetails { get; set; } = string.Empty; // Ürüne özel teslimat ve montaj bilgisi
        public string DocumentsDetails { get; set; } = string.Empty; // Ürüne özel doküman açıklaması
        public decimal BasePrice { get; set; }                       // Fiyat
        public decimal? DiscountPrice { get; set; }                  // İndirimli fiyat
        public bool IsPublished { get; set; }                        // Yayında mı?
        public int Quantity { get; set; }                            // Stok
        public ICollection<ProductPrice> ProductPrices { get; set; } // fiyatlar (farklı müşteri grupları için)

        // SEO Metadata
        public string MetaTitle { get; set; }                        // <title>
        public string MetaDescription { get; set; }                  // <meta name="description">
        public string MetaKeywords { get; set; }                     // <meta name="keywords">
        public string CanonicalUrl { get; set; }                     // <link rel="canonical">
        public string OgTitle { get; set; }                          // Open Graph: Facebook başlığı
        public string OgDescription { get; set; }                    // Open Graph: Açıklama
        public string OgImage { get; set; }                          // Open Graph: Görsel (URL)
        public string TwitterCardType { get; set; } = "summary_large_image"; // Twitter kart tipi

        // Yapılandırılmış Veri Desteği (JSON-LD vs. için)
        public string Brand { get; set; }                            // Marka
        public string BrandLogoUrl { get; set; } = string.Empty;     // Ürün kartında gösterilecek marka logosu
        public string CardHighlightsJson { get; set; } = "[]";       // Ürün kartındaki kısa fayda listesi
        public string GTIN { get; set; }                             // Global Trade Item Number
        public string MPN { get; set; }                              // Manufacturer Part Number

        // Zengin İçerik
        public ICollection<ProductImage> ProductImages { get; set; }
        public ICollection<ProductAttributeCombination> ProductAttributeCombination { get; set; }

        // Kategori İlişkisi
        public int CategoryId { get; set; }
        public string AdditionalCategoryIdsJson { get; set; } = "[]"; // İkincil vitrin kategorileri
        public Category Category { get; set; }

        // Siparişler ve Sepet
        public ICollection<OrderItem> OrderItems { get; set; }
        public ICollection<CartItem> CartItems { get; set; }
        public ICollection<ProductProductTag> ProductProductTags { get; set; }
        // Etiketleme ve Yorumlar
        public ICollection<ProductReview> Reviews { get; set; }      // Rich snippet için
        public ICollection<CustomerFavorite> Favorites { get; set; }

        // İlgili Ürünler (Self many-to-many)
        public ICollection<ProductRelated> RelatedProducts { get; set; }
        public ICollection<ProductRelated> RelatedByProducts { get; set; }

        // Yardımcı SEO Fonksiyonu
        public string GetSeoFriendlyUrl() => $"/products/{Slug}";


        [ConcurrencyCheck]
        public byte[] RowVersion { get; set; }
    }

}
