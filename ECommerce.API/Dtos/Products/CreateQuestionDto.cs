using System.ComponentModel.DataAnnotations;

namespace ECommerce.API.Dtos.Products
{
    public class CreateQuestionDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Question { get; set; } = string.Empty;
    }
}

