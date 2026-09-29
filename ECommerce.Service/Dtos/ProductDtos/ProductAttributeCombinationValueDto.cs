using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductDtos
{
    public class ProductAttributeCombinationValueDto
    {
        public int Id { get; set; }
        public int ProductAttributeId { get; set; }
        public string AttributeName { get; set; }
        public int? ProductAttributeValueId { get; set; }
        public string AttributeValue { get; set; }
        public string PersonalizationText { get; set; }
    }
}
