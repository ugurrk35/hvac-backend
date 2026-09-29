using System;

namespace ECommerce.Domain.Entity
{
    public class DomainRoute : BaseEntity
    {
        public string Slug { get; set; }
        public string EntityType { get; set; } // "product" | "category"
        public int EntityId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

