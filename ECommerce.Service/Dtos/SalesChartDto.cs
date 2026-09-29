using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos
{
    public class SalesChartDto
    {
        public List<SalesDataPoint> DailySales { get; set; } = new List<SalesDataPoint>();
        public List<SalesDataPoint> MonthlySales { get; set; } = new List<SalesDataPoint>();
        public List<CategorySalesDto> TopCategories { get; set; } = new List<CategorySalesDto>();
        public decimal TotalSales { get; set; }
        public string Period { get; set; } = string.Empty;
    }

    public class SalesDataPoint
    {
        public string Date { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int OrderCount { get; set; }
    }

    public class CategorySalesDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal SalesAmount { get; set; }
        public int ProductCount { get; set; }
        public double Percentage { get; set; }
    }
}