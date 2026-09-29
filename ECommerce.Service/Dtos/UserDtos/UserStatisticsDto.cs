using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.UserDtos
{
    public class UserStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int BlockedUsers { get; set; }
        
        public int EmailConfirmedUsers { get; set; }
        public int EmailUnconfirmedUsers { get; set; }
        
        public int UsersRegisteredToday { get; set; }
        public int UsersRegisteredThisWeek { get; set; }
        public int UsersRegisteredThisMonth { get; set; }
        
        public Dictionary<string, int> UsersByRole { get; set; } = new();
        
        public DateTime LastRegistrationDate { get; set; }
        public DateTime LastLoginDate { get; set; }
        
        public double AverageUsersPerDay { get; set; }
        public int MostActiveUsersCount { get; set; }
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}