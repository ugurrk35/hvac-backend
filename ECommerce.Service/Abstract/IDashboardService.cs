using ECommerce.Service.Dtos;
using ECommerce.Service.Dtos.OrderDtos;
using ECommerce.Service.Dtos.ProductDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Abstract
{
    public interface IDashboardService
    {
        Task<DashboardStatisticsDto> GetDashboardStatisticsAsync();
        Task<List<OrderDto>> GetRecentOrdersAsync(int count = 10);
        Task<SalesChartDto> GetSalesChartAsync(int days = 30);
        Task<List<ProductListDto>> GetTopProductsAsync(int count = 10);
        Task<List<ProductListDto>> GetLowStockAlertsAsync(int threshold = 10);
        Task<Dictionary<string, decimal>> GetRevenueByPeriodAsync(DateTime startDate, DateTime endDate);
        Task<List<CategorySalesDto>> GetTopCategoriesBySalesAsync(int count = 5);
        Task<Dictionary<string, int>> GetOrderStatusDistributionAsync();
    }
}