using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductDtos
{
    public class UpdateStockDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public int QuantityChange { get; set; }

        public string Reason { get; set; }
    }
}
