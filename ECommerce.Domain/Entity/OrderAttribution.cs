namespace ECommerce.Domain.Entity
{
    public class OrderAttribution : AuditableEntity
    {
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        public string? VisitorId { get; set; }
        public string? Source { get; set; }
        public string? Medium { get; set; }
        public string? Campaign { get; set; }
        public string? Content { get; set; }
        public string? Term { get; set; }
        public string? LandingPath { get; set; }
    }
}
