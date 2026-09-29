using System;

namespace ECommerce.Domain.Entity
{
    public class ProductQuestion : AuditableEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string AuthorName { get; set; }
        public string? AuthorEmail { get; set; }

        public string QuestionText { get; set; }
        public string? AnswerText { get; set; }

        public bool IsApproved { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}

