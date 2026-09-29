using System.Collections.Generic;

namespace ECommerce.API.Models
{
    public class FooterLinkDto
    {
        public string label { get; set; } = string.Empty;
        public string? url { get; set; }
        public string? pageSlug { get; set; }
        public bool isExternal { get; set; }
        public int sort { get; set; }
    }

    public class FooterColumnDto
    {
        public string title { get; set; } = string.Empty;
        public int sort { get; set; }
        public List<FooterLinkDto> links { get; set; } = new();
    }

    public class FooterConfigDto
    {
        public List<FooterColumnDto> columns { get; set; } = new();
        public bool showNewsletter { get; set; } = false;
        public List<FooterLinkDto> social { get; set; } = new();
    }
}

