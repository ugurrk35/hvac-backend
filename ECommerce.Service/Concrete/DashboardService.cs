using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Service.Abstract;
using ECommerce.Service.Dtos;
using ECommerce.Service.Dtos.OrderDtos;
using ECommerce.Service.Dtos.ProductDtos;
using ECommerce.Service.Mapping.Manual;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Concrete
{
    public class DashboardService : IDashboardService
    {
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IUserIdentityRepository _userRepository;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            IOrderService orderService,
            IProductService productService,
            ICategoryService categoryService,
            IUserIdentityRepository userRepository,
            ILogger<DashboardService> logger)
        {
            _orderService = orderService;
            _productService = productService;
            _categoryService = categoryService;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var monthStart = new DateTime(today.Year, today.Month, 1);

                // Get all orders
                var allOrders = await _orderService.GetAllAsync();
                var todayOrders = allOrders.Where(o => o.CreatedAt.Date == today).ToList();
                var monthOrders = allOrders.Where(o => o.CreatedAt >= monthStart).ToList();

                // Get product statistics
                var productStats = await _productService.GetProductStatisticsAsync();
                var allProducts = await _productService.GetAllAsync();
                var lowStockProducts = allProducts.Where(p => p.Quantity <= 10 && p.Quantity > 0).Count();
                var outOfStockProducts = allProducts.Where(p => p.Quantity == 0).Count();

                // Get all categories
                var allCategories = await _categoryService.GetAllAsync();

                // Get all users (customers)
                var allUsers = await _userRepository.GetAllAsync();

                // Calculate revenue
                var totalRevenue = allOrders.Sum(o => o.TotalAmount);
                var todayRevenue = todayOrders.Sum(o => o.TotalAmount);
                var monthlyRevenue = monthOrders.Sum(o => o.TotalAmount);

                // Calculate average order value
                var averageOrderValue = allOrders.Any() ? (double)(totalRevenue / allOrders.Count) : 0;

                // Count orders by status
                var pendingOrders = allOrders.Count(o => o.OrderStatus == OrderStatus.Pending);
                var processingOrders = allOrders.Count(o => o.OrderStatus == OrderStatus.Processing);
                var completedOrders = allOrders.Count(o => o.OrderStatus == OrderStatus.Delivered);
                var cancelledOrders = allOrders.Count(o => o.OrderStatus == OrderStatus.Canceled);

                return new DashboardStatisticsDto
                {
                    TotalOrders = allOrders.Count,
                    TotalProducts = productStats.GetValueOrDefault("TotalProducts", 0),
                    TotalCustomers = allUsers.Count,
                    TotalCategories = allCategories.Count,

                    TotalRevenue = totalRevenue,
                    TodayRevenue = todayRevenue,
                    MonthlyRevenue = monthlyRevenue,

                    PendingOrders = pendingOrders,
                    ProcessingOrders = processingOrders,
                    CompletedOrders = completedOrders,
                    CancelledOrders = cancelledOrders,

                    LowStockProducts = lowStockProducts,
                    OutOfStockProducts = outOfStockProducts,
                    PublishedProducts = productStats.GetValueOrDefault("PublishedProducts", 0),

                    AverageOrderValue = averageOrderValue,
                    OrdersToday = todayOrders.Count,
                    OrdersThisMonth = monthOrders.Count,

                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating dashboard statistics");
                throw;
            }
        }

        public async Task<List<OrderDto>> GetRecentOrdersAsync(int count = 10)
        {
            try
            {
                var recentOrders = await _orderService.GetRecentOrdersAsync(count);
                return recentOrders.Select(order => order.ToDto()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent orders");
                throw;
            }
        }

        public async Task<SalesChartDto> GetSalesChartAsync(int days = 30)
        {
            try
            {
                var endDate = DateTime.UtcNow.Date;
                var startDate = endDate.AddDays(-days);

                var orders = await _orderService.GetOrdersBetweenDatesAsync(startDate, endDate);

                var dailySales = orders
                    .GroupBy(o => o.CreatedAt.Date)
                    .Select(g => new SalesDataPoint
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"),
                        Amount = g.Sum(o => o.TotalAmount),
                        OrderCount = g.Count()
                    })
                    .OrderBy(s => s.Date)
                    .ToList();

                // Fill missing dates with zero sales
                var allDates = Enumerable.Range(0, days)
                    .Select(i => startDate.AddDays(i))
                    .ToList();

                var completeDailySales = allDates.Select(date =>
                {
                    var existing = dailySales.FirstOrDefault(s => s.Date == date.ToString("yyyy-MM-dd"));
                    return existing ?? new SalesDataPoint
                    {
                        Date = date.ToString("yyyy-MM-dd"),
                        Amount = 0,
                        OrderCount = 0
                    };
                }).ToList();

                var totalSales = orders.Sum(o => o.TotalAmount);

                return new SalesChartDto
                {
                    DailySales = completeDailySales,
                    TotalSales = totalSales,
                    Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sales chart");
                throw;
            }
        }

        public async Task<List<ProductListDto>> GetTopProductsAsync(int count = 10)
        {
            try
            {
                // This is a simplified implementation
                // In a real scenario, you'd calculate based on sales data
                var allProducts = await _productService.GetAllAsync();
                var topProducts = allProducts
                    .Where(p => !p.IsDeleted && p.IsPublished)
                    .OrderByDescending(p => p.CreatedAt) // Simplified: by newest
                    .Take(count)
                    .ToList();

                return topProducts.Select(product => product.ToListDto()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top products");
                throw;
            }
        }

        public async Task<List<ProductListDto>> GetLowStockAlertsAsync(int threshold = 10)
        {
            try
            {
                var allProducts = await _productService.GetAllAsync();
                var lowStockProducts = allProducts
                    .Where(p => !p.IsDeleted && p.IsPublished && p.Quantity <= threshold && p.Quantity > 0)
                    .OrderBy(p => p.Quantity)
                    .ToList();

                return lowStockProducts.Select(product => product.ToListDto()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock alerts");
                throw;
            }
        }

        public async Task<Dictionary<string, decimal>> GetRevenueByPeriodAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var orders = await _orderService.GetOrdersBetweenDatesAsync(startDate, endDate);
                
                return new Dictionary<string, decimal>
                {
                    ["TotalRevenue"] = orders.Sum(o => o.TotalAmount),
                    ["AverageOrderValue"] = orders.Any() ? orders.Average(o => o.TotalAmount) : 0,
                    ["OrderCount"] = orders.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating revenue by period");
                throw;
            }
        }

        public async Task<List<CategorySalesDto>> GetTopCategoriesBySalesAsync(int count = 5)
        {
            try
            {
                // This is a simplified implementation
                // In a real scenario, you'd join with order data to get actual sales
                var categories = await _categoryService.GetAllAsync();
                var allProducts = await _productService.GetAllAsync();

                var categorySales = categories.Select(c => new CategorySalesDto
                {
                    CategoryName = c.Name,
                    ProductCount = allProducts.Count(p => p.CategoryId == c.Id && !p.IsDeleted),
                    SalesAmount = 0, // Would need order item data to calculate actual sales
                    Percentage = 0
                }).OrderByDescending(c => c.ProductCount)
                .Take(count)
                .ToList();

                return categorySales;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top categories by sales");
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetOrderStatusDistributionAsync()
        {
            try
            {
                var allOrders = await _orderService.GetAllAsync();
                
                return new Dictionary<string, int>
                {
                    ["Pending"] = allOrders.Count(o => o.OrderStatus == OrderStatus.Pending),
                    ["Processing"] = allOrders.Count(o => o.OrderStatus == OrderStatus.Processing),
                    ["Shipped"] = allOrders.Count(o => o.OrderStatus == OrderStatus.Shipped),
                    ["Delivered"] = allOrders.Count(o => o.OrderStatus == OrderStatus.Delivered),
                    ["Canceled"] = allOrders.Count(o => o.OrderStatus == OrderStatus.Canceled),
                    ["Returned"] = allOrders.Count(o => o.OrderStatus == OrderStatus.Returned)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order status distribution");
                throw;
            }
        }
    }
}
