using ECommerce.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class ProductAttributeCombinationValue:AuditableEntity
    {
        public int ProductAttributeCombinationId { get; set; }
        public ProductAttributeCombination ProductAttributeCombination { get; set; }
        public int? ProductAttributeValueId { get; set; }
        public ProductAttributeValue? ProductAttributeValue { get; set; }
        public string? PersonalizationText { get; set; } // ? eklendi
        public int ProductAttributeId { get; set; }
        public ProductAttribute ProductAttribute { get; set; }

    }
}
