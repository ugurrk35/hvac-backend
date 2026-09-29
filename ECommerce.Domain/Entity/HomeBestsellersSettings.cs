using System;

namespace ECommerce.Domain.Entity
{
    public class HomeBestsellersSettings : BaseEntity
    {
        public string? Title { get; set; }
        public bool IsEnabled { get; set; }
        // Stored as JSON array of integers, e.g., "[1,2,3,4]"
        public string? ProductIdsJson { get; set; }
        public int MaxItems { get; set; } = 4;
        public int GridColumns { get; set; } = 4;

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
