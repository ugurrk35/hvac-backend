using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Repository.Repo
{
    public class ProductTagRepository : GenericRepository<ProductTag>, IProductTagRepository
    {
        public ProductTagRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<ProductTag?> GetTagWithProductsByIdAsync(int id)
        {
            return await _dbContext.ProductTags
                .Include(t => t.ProductProductTags)
                    .ThenInclude(pt => pt.Product)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        }
        public async Task<List<ProductTag>> SearchAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 3)
                return new List<ProductTag>();

            return await _dbContext.ProductTags
                .Where(t => !t.IsDeleted && t.Name.Contains(term))
                .OrderBy(t => t.Name)
                .Take(20)
                .ToListAsync();
        }
    }
}
