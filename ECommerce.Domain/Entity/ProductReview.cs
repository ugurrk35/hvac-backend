namespace ECommerce.Domain.Entity
{
    public class ProductReview : AuditableEntity
    {
        // İlişkili Ürün
        public int ProductId { get; set; }
        public Product Product { get; set; }

        // Kullanıcı (anonim ya da kayıtlı olabilir)
        public string ReviewerName { get; set; }         // Gösterilecek ad
        public string ReviewerEmail { get; set; }        // Opsiyonel: e-posta (spam koruması önemli!)
        public string Title { get; set; }                // Yorum başlığı
        public string Content { get; set; }              // Yorum metni
        public int Rating { get; set; }                  // 1-5 arası puan

        public bool IsApproved { get; set; }             // Admin onayı

        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;

        public ICollection<ProductReviewPhoto>? Photos { get; set; }
    }
}

