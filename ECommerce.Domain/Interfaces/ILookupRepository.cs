using ECommerce.Domain.Dtos;
using ECommerce.Service.Dtos.ProductAttributeDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    /// <summary>
    /// Lookup verileri için genel amaçlı arayüz.
    /// Dropdown ve liste verileri sağlamak için kullanılır.
    /// </summary>
    public interface ILookupRepository
    {
        Task<List<LookupDto>> SearchTagsAsync(string term);
        /// <summary>
        /// Tüm aktif kategorileri getirir (sadece Id ve Name alanları).
        /// </summary>
        Task<List<LookupDto>> GetCategoriesAsync();
        Task<List<LookupDto>> GetBlogCategoriesAsync();


        /// <summary>
        /// Tüm aktif ürün özelliklerini (attribute) getirir.
        /// </summary>
        Task<List<LookupPersonalizationDto>> GetProductAttributesAsync();

        /// <summary>
        /// Belirli bir ürün özelliğine ait değerleri getirir.
        /// </summary>
        /// <param name="productAttributeId">Ürün özelliğinin ID’si</param>
        Task<List<LookupDto>> GetProductAttributeValuesByAttributeIdAsync(int productAttributeId);

        /// <summary>
        /// ürün taglarını getirir
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<LookupDto>> GetAllActiveTagsAsync();
    }
}
