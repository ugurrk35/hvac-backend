using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Dtos.ImageDtos
{
    public class ImageDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string AltText { get; set; }
        public string Caption { get; set; }
        public string Url { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string FileExtension { get; set; }
        public int SizeInBytes { get; set; }
    }
}
