using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductAttributeDtos
{
    public class ProductAttributeValueUpdateDto
    {
        public int Id { get; set; }
        public string Value { get; set; }
        public decimal PriceModifier { get; set; }
    }
}
