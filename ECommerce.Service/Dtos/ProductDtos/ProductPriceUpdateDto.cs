using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductDtos
{
    public class ProductPriceUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? BasePrice { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? DiscountPrice { get; set; }
    }
}
