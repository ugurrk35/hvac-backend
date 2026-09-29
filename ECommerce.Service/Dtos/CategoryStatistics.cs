using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos
{
    public class CategoryStatistics
    {
        public int TotalActiveCategories { get; set; }
        public int TotalProducts { get; set; }
        public string TopCategoryByProducts { get; set; }
        public double AverageProductsPerCategory { get; set; }
    }
}
