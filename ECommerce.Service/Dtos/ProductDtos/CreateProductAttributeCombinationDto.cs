using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductDtos
{
    public class CreateProductAttributeCombinationDto
    {
        [Required]
        [StringLength(100)]
        public string Sku { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }
        // Kombinasyon fiyatı
        //public ProductPriceDto ProductPrice { get; set; }
        public List<CreateProductAttributeCombinationValueDto> AttributeValues { get; set; } = new();
    }
}
