namespace ECommerce.Domain.Entity
{
    public class ProductTag:AuditableEntity
    {

        public string Name { get; set; }                // Örn: “organik”, “el yapımı”, “indirimli”
        public string Slug { get; set; }                // SEO dostu URL: “organik”
        public ICollection<ProductProductTag> ProductProductTags { get; set; }

    }
}