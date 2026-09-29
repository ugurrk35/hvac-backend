using System.ComponentModel.DataAnnotations;

namespace ECommerce.API.Dtos.Products
{
    public class CreateReviewDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1,5)]
        public int Rating { get; set; }

        [MaxLength(150)]
        public string? Title { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public List<int>? ImageIds { get; set; }
    }
}
