namespace ECommerce.Domain.Entity
{
    public class CustomerAddress : AuditableEntity
    {
        public int UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public string Title { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Country { get; set; } = "Türkiye";
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string? Neighborhood { get; set; }
        public string AddressLine { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
        public bool IsDefault { get; set; }
    }
}
