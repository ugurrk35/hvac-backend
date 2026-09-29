using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductDtos
{
    public class CreateProductAttributeCombinationValueDto
    {
        [Required]
        public int ProductAttributeId { get; set; }
        public int? ProductAttributeValueId { get; set; }
        //public string PersonalizationText { get; set; }
    }
}
