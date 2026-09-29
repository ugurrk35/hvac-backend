using ECommerce.Domain.Dtos;
using ECommerce.Domain.Interfaces;
using ECommerce.Service.Abstract.Base;
using ECommerce.Service.Dtos.ProductAttributeDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Concrete.Base
{
    /// <summary>
    /// Lookup verileri için iş mantığını yöneten servis.
    /// </summary>
    public class LookupService : ILookupService
    {
        private readonly ILookupRepository _lookupRepository;

        public LookupService(ILookupRepository lookupRepository)
        {
            _lookupRepository = lookupRepository;
        }

        public async Task<List<LookupDto>> BlogCategory()
        {
            return await _lookupRepository.GetBlogCategoriesAsync();

        }

        public async Task<List<LookupDto>> GetCategoriesAsync()
        {
            return await _lookupRepository.GetCategoriesAsync();
        }

        public async Task<List<LookupPersonalizationDto>> GetProductAttributesAsync()
        {
            return await _lookupRepository.GetProductAttributesAsync();
        }

        public async Task<List<LookupDto>> GetProductAttributeValuesByAttributeIdAsync(int productAttributeId)
        {
            return await _lookupRepository.GetProductAttributeValuesByAttributeIdAsync(productAttributeId);
        }

        /// <summary>
        /// Etiketlerde arama yapar (en az 3 harf girilmiş olmalı).
        /// </summary>
        public async Task<List<LookupDto>> SearchTagsAsync(string term)
        {
            return await _lookupRepository.SearchTagsAsync(term);
        }
    }
}
