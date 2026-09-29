using ECommerce.Domain.Dtos;
using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Repository.Data;
using ECommerce.Service.Dtos.ProductAttributeDtos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Repository.Repo
{
    /// <summary>
    /// Lookup verileri için repository implementasyonu.
    /// Dropdown listeleri için ID ve Name döner.
    /// </summary>
    public class LookupRepository : ILookupRepository
    {
        private readonly ApplicationDbContext _context;

        public LookupRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tüm aktif kategorileri döner (Id ve Name alanları).
        /// </summary>
        public async Task<List<LookupDto>> GetCategoriesAsync()
        {
            return await _context.Categories
                .Where(c => c.IsActive)
                .Select(c => new LookupDto
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();
        }

        /// <summary>
        /// Tüm aktif ürün özelliklerini döner (örneğin Renk, Beden).
        /// </summary>
        public async Task<List<LookupPersonalizationDto>> GetProductAttributesAsync()
        {
            return await _context.ProductAttributes
                //.Where(p => p.IsActive)
                .Select(p => new LookupPersonalizationDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    IsPersonalization = p.IsPersonalizationText // true ise kişiselleştirme özelliği

                })
                .ToListAsync();
        }

        /// <summary>
        /// Belirli bir ürün özelliğine ait aktif değerleri döner
        /// (örneğin "Renk" için "Kırmızı", "Mavi").
        /// </summary>
        public async Task<List<LookupDto>> GetProductAttributeValuesByAttributeIdAsync(int productAttributeId)
        {
            return await _context.ProductAttributeValues
                .Where(p => p.ProductAttributeId == productAttributeId)
                .Select(p => new LookupDto
                {
                    Id = p.Id,
                    Name = p.Value
                })
                .ToListAsync();
        }
        public async Task<IEnumerable<LookupDto>> GetAllActiveTagsAsync()
        {
            return await _context.ProductTags
                .Where(t => !t.IsDeleted && t.IsActive)
                .Select(p => new LookupDto
                {
                    Id = p.Id,
                    Name = p.Name
                })
                .ToListAsync();
        }
        /// <summary>
        /// Arama terimine göre aktif tag (etiket) döner — en az 3 karakter şartıyla.
        /// </summary>
        public async Task<List<LookupDto>> SearchTagsAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 3)
                return new List<LookupDto>();

            return await _context.ProductTags
                .Where(t => !t.IsDeleted && t.IsActive && t.Name.Contains(term))
                .OrderBy(t => t.Name)
                .Take(20)
                .Select(t => new LookupDto
                {
                    Id = t.Id,
                    Name = t.Name
                })
                .ToListAsync();
        }

        public async Task<List<LookupDto>> GetBlogCategoriesAsync()
        {
            return await _context.BlogCategories
                .Where(c => c.IsActive)
                .Select(c => new LookupDto
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();
        }
    }
}
