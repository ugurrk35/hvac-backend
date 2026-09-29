namespace ECommerce.Domain.Entity
{
    public class WebVitalMetric : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
        public string? Path { get; set; }
        public string? VisitorId { get; set; }
    }
}
