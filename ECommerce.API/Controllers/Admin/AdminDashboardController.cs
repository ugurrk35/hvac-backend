using ECommerce.Service.Abstract;
using ECommerce.Service.Dtos;
using ECommerce.Service.Dtos.OrderDtos;
using ECommerce.Service.Dtos.ProductDtos;
using ECommerce.Service.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ECommerce.API.Controllers.Admin
{
    /// <summary>
    /// (Admin) Yönetim paneli için özet istatistikler, son siparişler, en çok satan ürünler ve satış grafikleri.
    /// </summary>
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<AdminDashboardController> _logger;

        public AdminDashboardController(
            IDashboardService dashboardService,
            ILogger<AdminDashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Dashboard genel istatistiklerini getirir
        /// </summary>
        [HttpGet("statistics")]
        [ResponseCache(Duration = 300)] // 5 dakika cache
        [ProducesResponseType(typeof(DataResponse<DashboardStatisticsDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<DataResponse<DashboardStatisticsDto>>> GetDashboardStatistics()
        {
            try
            {
                var statistics = await _dashboardService.GetDashboardStatisticsAsync();
                return Ok(DataResponse<DashboardStatisticsDto>.CreateSuccess(statistics, "Dashboard istatistikleri başarıyla getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard istatistikleri getirilirken hata oluştu");
                return StatusCode(500, BaseResponse.CreateFailure("İstatistikler getirilemedi"));
            }
        }

        /// <summary>
        /// Son siparişleri getirir
        /// </summary>
        [HttpGet("recent-orders")]
        [ProducesResponseType(typeof(DataResponse<List<OrderDto>>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<DataResponse<List<OrderDto>>>> GetRecentOrders([FromQuery] int count = 10)
        {
            try
            {
                if (count <= 0 || count > 100)
                    return BadRequest(BaseResponse.CreateFailure("Count değeri 1-100 arasında olmalıdır"));

                var orders = await _dashboardService.GetRecentOrdersAsync(count);
                return Ok(DataResponse<List<OrderDto>>.CreateSuccess(orders, $"Son {count} sipariş başarıyla getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Son siparişler getirilirken hata oluştu");
                return StatusCode(500, BaseResponse.CreateFailure("Siparişler getirilemedi"));
            }
        }

        /// <summary>
        /// Satış grafiği verilerini getirir
        /// </summary>
        [HttpGet("sales-chart")]
        [ResponseCache(Duration = 600)] // 10 dakika cache
        [ProducesResponseType(typeof(DataResponse<SalesChartDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<DataResponse<SalesChartDto>>> GetSalesChart([FromQuery] int days = 30)
        {
            try
            {
                if (days <= 0 || days > 365)
                    return BadRequest(BaseResponse.CreateFailure("Days değeri 1-365 arasında olmalıdır"));

                var salesChart = await _dashboardService.GetSalesChartAsync(days);
                return Ok(DataResponse<SalesChartDto>.CreateSuccess(salesChart, $"Son {days} günlük satış verileri başarıyla getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satış grafiği verileri getirilirken hata oluştu");
                return StatusCode(500, BaseResponse.CreateFailure("Satış verileri getirilemedi"));
            }
        }

        /// <summary>
        /// En çok satan ürünleri getirir
        /// </summary>
        [HttpGet("top-products-get")]
        //[ResponseCache(Duration = 900)] // 15 dakika cache
        //[ProducesResponseType(typeof(DataResponse<List<ProductListDto>>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<DataResponse<List<ProductListDto>>>> GetTopProducts([FromQuery] int count = 10)
        {
            try
            {
                if (count <= 0 || count > 50)
                    return BadRequest(BaseResponse.CreateFailure("Count değeri 1-50 arasında olmalıdır"));

                var topProducts = await _dashboardService.GetTopProductsAsync(count);
                return Ok(DataResponse<List<ProductListDto>>.CreateSuccess(topProducts, $"En çok satan {count} ürün başarıyla getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "En çok satan ürünler getirilirken hata oluştu");
                return StatusCode(500, BaseResponse.CreateFailure("Ürün verileri getirilemedi"));
            }
        }

        /// <summary>
        /// Düşük stok uyarılarını getirir
        /// </summary>
        [HttpGet("low-stock-alerts")]
        [ProducesResponseType(typeof(DataResponse<List<ProductListDto>>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<DataResponse<List<ProductListDto>>>> GetLowStockAlerts([FromQuery] int threshold = 10)
        {
            try
            {
                if (threshold < 0 || threshold > 100)
                    return BadRequest(BaseResponse.CreateFailure("Threshold değeri 0-100 arasında olmalıdır"));

                var lowStockProducts = await _dashboardService.GetLowStockAlertsAsync(threshold);
                return Ok(DataResponse<List<ProductListDto>>.CreateSuccess(lowStockProducts, $"Stok seviyesi {threshold} ve altındaki ürünler başarıyla getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Düşük stok uyarıları getirilirken hata oluştu");
                return StatusCode(500, BaseResponse.CreateFailure("Stok verileri getirilemedi"));
            }
        }

        /// <summary>
        /// Belirli tarih aralığındaki gelir verilerini getirir
        /// </summary>
        [HttpGet("revenue")]
        [ResponseCache(Duration = 1800)] // 30 dakika cache
        [ProducesResponseType(typeof(DataResponse<Dictionary<string, decimal>>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<DataResponse<Dictionary<string, decimal>>>> GetRevenue(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                if (start >= end)
                    return BadRequest(BaseResponse.CreateFailure("Başlangıç tarihi bitiş tarihinden önce olmalıdır"));

                if ((end - start).TotalDays > 365)
                    return BadRequest(BaseResponse.CreateFailure("Tarih aralığı 365 günden fazla olamaz"));

                var revenue = await _dashboardService.GetRevenueByPeriodAsync(start, end);
                return Ok(DataResponse<Dictionary<string, decimal>>.CreateSuccess(revenue, "Gelir verileri başarıyla getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gelir verileri getirilirken hata oluştu");
                return StatusCode(500, BaseResponse.CreateFailure("Gelir verileri getirilemedi"));
            }
        }

        /// <summary>
        /// En çok satan kategorileri getirir
        /// </summary>
        [HttpGet("top-categories")]
        [ResponseCache(Duration = 1800)] // 30 dakika cache
        [ProducesResponseType(typeof(DataResponse<List<CategorySalesDto>>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<DataResponse<List<CategorySalesDto>>>> GetTopCategories([FromQuery] int count = 5)
        {
            try
            {
                if (count <= 0 || count > 20)
                    return BadRequest(BaseResponse.CreateFailure("Count değeri 1-20 arasında olmalıdır"));

                var topCategories = await _dashboardService.GetTopCategoriesBySalesAsync(count);
                return Ok(DataResponse<List<CategorySalesDto>>.CreateSuccess(topCategories, $"En çok satan {count} kategori başarıyla getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "En çok satan kategoriler getirilirken hata oluştu");
                return StatusCode(500, BaseResponse.CreateFailure("Kategori verileri getirilemedi"));
            }
        }

        /// <summary>
        /// Sipariş durumu dağılımını getirir
        /// </summary>
        [HttpGet("order-status-distribution")]
        [ResponseCache(Duration = 300)] // 5 dakika cache
        [ProducesResponseType(typeof(DataResponse<Dictionary<string, int>>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<DataResponse<Dictionary<string, int>>>> GetOrderStatusDistribution()
        {
            try
            {
                var distribution = await _dashboardService.GetOrderStatusDistributionAsync();
                return Ok(DataResponse<Dictionary<string, int>>.CreateSuccess(distribution, "Sipariş durumu dağılımı başarıyla getirildi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş durumu dağılımı getirilirken hata oluştu");
                return StatusCode(500, BaseResponse.CreateFailure("Sipariş durumu verileri getirilemedi"));
            }
        }
    }
}
