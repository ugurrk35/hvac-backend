using System.ComponentModel.DataAnnotations;

namespace ECommerce.Service.Dtos.ImageDtos
{
    public class UpdateImageDto
    {
        [Required]
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? AltText { get; set; }
        public string? Caption { get; set; }
        public string? Url { get; set; }
    }
}