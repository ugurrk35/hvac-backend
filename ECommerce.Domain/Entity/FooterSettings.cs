using System;

namespace ECommerce.Domain.Entity
{
    public class FooterSettings : BaseEntity
    {
        public string? ConfigJson { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}

