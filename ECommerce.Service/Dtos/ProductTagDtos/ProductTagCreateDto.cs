using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductTagDtos
{
    public class ProductTagCreateDto
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
