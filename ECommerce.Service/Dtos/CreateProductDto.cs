using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos
{

    public class CreateProductDto
    {
        // Temel Ürün Bilgileri (Zorunlu)
        [Required(ErrorMessage = "Ürün adı zorunludur")]
        [StringLength(200, ErrorMessage = "Ürün adı en fazla 200 karakter olabilir")]
        public string Name { get; set; }

        [StringLength(250, ErrorMessage = "Slug en fazla 250 karakter olabilir")]
        public string? Slug { get; set; } // Otomatik oluşturulabilir

        [Required(ErrorMessage = "SKU zorunludur")]
        [StringLength(50, ErrorMessage = "SKU en fazla 50 karakter olabilir")]
        public string SKU { get; set; }

        [StringLength(500, ErrorMessage = "Kısa açıklama en fazla 500 karakter olabilir")]
        public string? ShortDescription { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Fiyat zorunludur")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Fiyat 0'dan büyük olmalıdır")]
        public decimal BasePrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "İndirimli fiyat negatif olamaz")]
        public decimal? DiscountPrice { get; set; }

        public bool IsPublished { get; set; } = true;

        [Required(ErrorMessage = "Stok miktarı zorunludur")]
        [Range(0, int.MaxValue, ErrorMessage = "Stok miktarı negatif olamaz")]
        public int Quantity { get; set; }

        // Kategori İlişkisi
        [Required(ErrorMessage = "Kategori seçimi zorunludur")]
        public int CategoryId { get; set; }

        // SEO Metadata (İsteğe bağlı ama önemli)
        [StringLength(60, ErrorMessage = "Meta title en fazla 60 karakter olmalıdır")]
        public string? MetaTitle { get; set; }

        [StringLength(160, ErrorMessage = "Meta description en fazla 160 karakter olmalıdır")]
        public string? MetaDescription { get; set; }

        [StringLength(200, ErrorMessage = "Meta keywords en fazla 200 karakter olmalıdır")]
        public string? MetaKeywords { get; set; }

        public string? CanonicalUrl { get; set; }

        // Open Graph (Social Media)
        [StringLength(60, ErrorMessage = "OG title en fazla 60 karakter olmalıdır")]
        public string? OgTitle { get; set; }

        [StringLength(160, ErrorMessage = "OG description en fazla 160 karakter olmalıdır")]
        public string? OgDescription { get; set; }

        [Url(ErrorMessage = "Geçerli bir URL formatında olmalıdır")]
        public string? OgImage { get; set; }

        public string TwitterCardType { get; set; } = "summary_large_image";

        // Yapılandırılmış Veri
        [StringLength(100, ErrorMessage = "Marka adı en fazla 100 karakter olabilir")]
        public string? Brand { get; set; }

        [StringLength(50, ErrorMessage = "GTIN en fazla 50 karakter olabilir")]
        public string? GTIN { get; set; }

        [StringLength(50, ErrorMessage = "MPN en fazla 50 karakter olabilir")]
        public string? MPN { get; set; }

        // Ürün Görselleri
        //public List<CreateProductImageDto>? ProductImages { get; set; }

        //// Ürün Özellikleri/Varyantları
        //public List<CreateProductAttributeCombinationDto>? ProductAttributeCombinations { get; set; }

        // Etiketler
        public List<int>? ProductTagIds { get; set; }
    }
}
