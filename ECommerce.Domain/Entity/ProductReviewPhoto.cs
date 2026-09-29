namespace ECommerce.Domain.Entity
{
    public class ProductReviewPhoto : AuditableEntity
    {
        public int ProductReviewId { get; set; }
        public ProductReview ProductReview { get; set; }

        public int ImageId { get; set; }
        public Image Image { get; set; }
    }
}

