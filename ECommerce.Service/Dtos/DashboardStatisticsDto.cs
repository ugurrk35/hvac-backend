using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos
{
    public class DashboardStatisticsDto
    {
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalCategories { get; set; }
        
        public decimal TotalRevenue { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int PublishedProducts { get; set; }
        
        public double AverageOrderValue { get; set; }
        public int OrdersToday { get; set; }
        public int OrdersThisMonth { get; set; }
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}