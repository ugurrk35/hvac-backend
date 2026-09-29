using ECommerce.Domain.Dtos;
using ECommerce.Service.Dtos.ProductAttributeDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Abstract.Base
{
    /// <summary>
    /// Lookup verilerini sağlayan servis arayüzü.
    /// </summary>
    public interface ILookupService
    {
        Task<List<LookupDto>> GetCategoriesAsync();
        Task<List<LookupPersonalizationDto>> GetProductAttributesAsync();
        Task<List<LookupDto>> GetProductAttributeValuesByAttributeIdAsync(int productAttributeId);
        Task<List<LookupDto>> SearchTagsAsync(string term);
        Task<List<LookupDto>> BlogCategory();

    }
}
