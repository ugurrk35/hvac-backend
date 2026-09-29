using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entity
{
    public class ProductRelated : AuditableEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int RelatedProductId { get; set; }
        public Product RelatedProduct { get; set; }

        public int SortOrder { get; set; }
        public CrossSellRecommendationType RecommendationType { get; set; } = CrossSellRecommendationType.FrequentlyBoughtTogether;
        public bool ShowOnProductPage { get; set; } = true;
        public bool ShowInCart { get; set; } = true;
    }
    public enum CrossSellRecommendationType { FrequentlyBoughtTogether = 0, SimilarProducts = 1, FreeShippingCompleter = 2 }
}
