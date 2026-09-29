namespace ECommerce.Domain.Entity
{
    public class CustomerFavorite : AuditableEntity
    {
        public int UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
    }
}
