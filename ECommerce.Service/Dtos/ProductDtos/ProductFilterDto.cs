using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ProductDtos
{
    public class ProductFilterDto
    {
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? InStock { get; set; }
        public string? SearchTerm { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public string? Material { get; set; }
        public bool? Personalizable { get; set; }
        public List<int> TagIds { get; set; } = new();
        public string? Brand { get; set; }
        public bool? IsPublished { get; set; }
        public string SortBy { get; set; } = "name"; // name, price, date, popularity
        public string SortOrder { get; set; } = "asc"; // asc, desc
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

}
