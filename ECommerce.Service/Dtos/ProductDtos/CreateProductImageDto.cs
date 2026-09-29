using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductDtos
{
    public class CreateProductImageDto
    {
        [Required]
        public int ImageId { get; set; }
        public int SortOrder { get; set; }
    }
}
