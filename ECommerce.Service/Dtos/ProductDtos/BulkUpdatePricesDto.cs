using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductDtos
{
    public class BulkUpdatePricesDto
    {
        [Required]
        public List<ProductPriceUpdateDto> Products { get; set; } = new();
    }
}
