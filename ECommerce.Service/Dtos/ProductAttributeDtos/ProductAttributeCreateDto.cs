using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductAttributeDtos
{
    public class ProductAttributeCreateDto
    {
        public string Name { get; set; }
        public bool IsPersonalizationText { get; set; }
        public string? TextPrompt { get; set; }
        public int? MaxLength { get; set; }
        public List<ProductAttributeValueCreateDto> ProductAttributeValues { get; set; }
    }
}
