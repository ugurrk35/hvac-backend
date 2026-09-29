namespace ECommerce.API.Dtos.Products
{
    public class ProductReviewDto
    {
        public string ReviewerName { get; set; }
        public string ReviewerEmail { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int Rating { get; set; }
        public bool IsApproved { get; set; }
        public DateTime ReviewDate { get; set; }
        public List<ProductReviewImageDto> Images { get; set; } = new();
    }

    public class ProductReviewImageDto
    {
        public string Url { get; set; }
        public string Alt { get; set; }
    }
}
