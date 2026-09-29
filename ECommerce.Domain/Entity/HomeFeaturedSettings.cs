using System;

namespace ECommerce.Domain.Entity
{
    public class HomeFeaturedSettings : BaseEntity
    {
        public string? Title { get; set; }
        public bool IsEnabled { get; set; }
        public string? ProductIdsJson { get; set; }
        public int MaxItems { get; set; } = 4;
        public int GridColumns { get; set; } = 4;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
