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
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(ApplicationDbContext context) : base(context)
        {
        }

        public override async Task<IReadOnlyList<Product>> GetAllAsync()
        {
            return await _dbContext.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                    .ThenInclude(pi => pi.Image)
                .ToListAsync();
        }
        public async Task<List<Product>> GetPublishedProductsByCollectionAsync(string collection)
        {
            var query = _dbContext.Products
                .Include(p => p.ProductImages)
                .ThenInclude(p=>p.Image)
                .Where(p => p.IsPublished && !p.IsDeleted);

            if (!string.IsNullOrEmpty(collection) && collection != "all")
                query = query.Where(p => p.Category.Name == collection);

            return await query
                .OrderByDescending(p => p.Id)
                .Take(10)
                .ToListAsync();
        }
        public async Task<Product?> GetByIdForUpdateAsync(int id)
        {
            if (_dbContext.Database.IsSqlServer())
            {
                FormattableString sql = $"SELECT * FROM Products WITH (UPDLOCK, ROWLOCK) WHERE Id = {id}";
                return await _dbContext.Products
                    .FromSqlInterpolated(sql)
                    .AsTracking()
                    .SingleOrDefaultAsync();
            }

            if (_dbContext.Database.IsNpgsql())
            {
                FormattableString sql = $"SELECT * FROM \"Products\" WHERE \"Id\" = {id} FOR UPDATE";
                return await _dbContext.Products
                    .FromSqlInterpolated(sql)
                    .AsTracking()
                    .SingleOrDefaultAsync();
            }

            return await _dbContext.Products
                .Where(p => p.Id == id)
                .AsTracking()
                .SingleOrDefaultAsync();
        }
        // Ürün ismine göre arama (kısmi veya tam eşleşme)
        public async Task<IEnumerable<Product>> GetAllByIdsAsync(List<int> ids)
        {
            return await _dbContext.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
        }
        public async Task<List<Product>> GetProductsByIdsOrCategoryAsync(List<int> productIds, int? categoryId)
        {
            var query = _dbContext.Products
                .Where(product => !product.IsDeleted)
                .AsQueryable();

            // Kategori bazlı filtre
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            // Belirli ürünler bazlı filtre
            if (productIds != null && productIds.Any())
            {
                productIds = productIds.Where(id => id > 0).ToList();
                if (productIds != null && productIds.Any())
                {
                    query = query.Where(p => productIds.Contains(p.Id));

                }
            }
            // Varyantları dahil et
            query = query
                .Include(p => p.ProductAttributeCombination)
                    .ThenInclude(c => c.ProductAttributeCombinationValues);

            return await query.ToListAsync();
        }
        public virtual async Task<IReadOnlyList<Product>> SearchByNameAsync(string searchTerm)
        {
            var pattern = $"%{searchTerm.Trim()}%";
            return await _dbContext.Products
                .Where(p => !p.IsDeleted && EF.Functions.ILike(p.Name, pattern))
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .ToListAsync();
        }

        // Kategoriye göre ürün listeleme
        public virtual async Task<IReadOnlyList<Product>> GetByCategoryIdAsync(int categoryId)
        {
            return await _dbContext.Products
                .Where(p => p.CategoryId == categoryId && p.Quantity > 0)
                .Include(p => p.ProductImages)
                .ThenInclude (p => p.Image)
                .ToListAsync();
        }

        // Fiyat aralığına göre ürün listeleme
        public virtual async Task<IReadOnlyList<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice)
        {
            return await _dbContext.Products
                .Where(p => p.BasePrice >= minPrice && p.BasePrice <= maxPrice && p.Quantity > 0)
                .Include(p => p.ProductImages)
                .ToListAsync();
        }

        // Stokta olan ürünleri getir
        public virtual async Task<IReadOnlyList<Product>> GetInStockProductsAsync()
        {
            return await _dbContext.Products
                .Where(p => p.Quantity > 0)
                .Include(p => p.ProductImages)
                .ToListAsync();
        }

        // Popüler ürünleri (örneğin en çok sipariş edilen ürünler)
        public virtual async Task<IReadOnlyList<Product>> GetPopularProductsAsync(int topCount)
        {
            return await _dbContext.Products
                .Where(p => !p.IsDeleted) // Sadece silinmemiş ürünler önyüzde kullanlacak
                .Include(p => p.OrderItems)
                .OrderByDescending(p => p.OrderItems.Count)
                .Take(topCount)
                .Include(p => p.ProductImages)
                .ToListAsync();
        }

        // Ürün detayları, ilişkili tüm verilerle
        public virtual async Task<Product> GetProductDetailsAsync(int productId)
        {
            return await _dbContext.Products
                .Include(p => p.Category)
                .Include(p => p.ProductProductTags)
                    .ThenInclude(pi => pi.ProductTag)
                .Include(p => p.ProductImages)
                    .ThenInclude(pi => pi.Image)
                .Include(p => p.OrderItems)
                .Include(p => p.CartItems)
                .Include(p => p.ProductAttributeCombination.Where(pac => pac.IsActive && !pac.IsDeleted))
                    .ThenInclude(pac => pac.ProductAttributeCombinationValues.Where(pacv => pacv.IsActive && !pacv.IsDeleted))
                        .ThenInclude(pacv => pacv.ProductAttributeValue)
                            .ThenInclude(pav => pav.ProductAttribute)
                .Include(p => p.Reviews.Where(r => r.IsApproved))
                    .ThenInclude(r => r.Photos)
                        .ThenInclude(ph => ph.Image)
                .FirstOrDefaultAsync(p => p.Id == productId);
        }


        // Ürün stoğunu güncelle (stok azalma/ekleme için)
        public virtual async Task UpdateStockAsync(int productId, int quantityChange)
        {
            var product = await _dbContext.Products.FindAsync(productId);
            if (product == null)
                throw new ArgumentException("Ürün bulunamadı");

            product.Quantity += quantityChange;

            if (product.Quantity < 0)
                product.Quantity = 0;

            _dbContext.Products.Update(product);
            await _dbContext.SaveChangesAsync();
        }

        // Çoklu filtreleme örneği: kategori, fiyat aralığı, stok durumu, arama
        public virtual async Task<IReadOnlyList<Product>> GetFilteredProductsAsync(int? categoryId, decimal? minPrice, decimal? maxPrice, bool? inStock, string searchTerm)
        {
            var query = _dbContext.Products
                .Where(product => !product.IsDeleted)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.BasePrice >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.BasePrice <= maxPrice.Value);

            if (inStock.HasValue)
            {
                if (inStock.Value)
                    query = query.Where(p => p.Quantity > 0);
                else
                    query = query.Where(p => p.Quantity <= 0);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var pattern = $"%{searchTerm.Trim()}%";
                query = query.Where(product =>
                    EF.Functions.ILike(product.Name, pattern) ||
                    EF.Functions.ILike(product.SKU, pattern) ||
                    EF.Functions.ILike(product.Slug, pattern) ||
                    EF.Functions.ILike(product.ShortDescription, pattern) ||
                    EF.Functions.ILike(product.Description, pattern));
            }

            return await query
                .Include(p => p.ProductImages)
                .ThenInclude(po=>po.Image)
                .Include(p => p.Category)
                .Include(p => p.ProductAttributeCombination)
                .ThenInclude(combination => combination.ProductAttributeCombinationValues)
                .ThenInclude(value => value.ProductAttributeValue)
                .Include(p => p.ProductAttributeCombination)
                .ThenInclude(combination => combination.ProductAttributeCombinationValues)
                .ThenInclude(value => value.ProductAttribute)
                .Include(p => p.Reviews)
                .Include(p => p.ProductProductTags)
                .ThenInclude(productTag => productTag.ProductTag)
                .AsSplitQuery()
                .ToListAsync();
        }
        //public async Task<Product> GetBySlugAsync(string slug)
        //{
        //    return await _dbContext.Products
        //        .Include(p => p.Reviews)
        //        .Include(p => p.Category)
        //        .Include(p => p.ProductProductTags)
        //            .ThenInclude(pt => pt.ProductTag)
        //        .Include(p => p.ProductImages)
        //            .ThenInclude(pi => pi.Image)
        //        //.Include(p => p.OrderItems)
        //        //.Include(p => p.CartItems)
        //        .Include(p => p.ProductAttributeCombination)
        //            .ThenInclude(pac => pac.ProductAttributeCombinationValues)
        //                .ThenInclude(pacv => pacv.ProductAttribute)
        //                    .ThenInclude(pa => pa.ProductAttributeValues)
        //        .Include(p => p.ProductAttributeCombination)
        //            .ThenInclude(pac => pac.ProductAttributeCombinationValues)
        //                .ThenInclude(pacv => pacv.ProductAttributeValue)
        //        .Where(p => !p.IsDeleted)
        //        .FirstOrDefaultAsync(p => p.Slug == slug);
        //}
        //public async Task<Product> GetBySlugAsync(string slug)
        //{
        //    return await _dbContext.Products
        //        .AsNoTracking()
        //        .AsSplitQuery()
        //        .Where(p => !p.IsDeleted && p.Slug == slug)
        //        .Include(p => p.Reviews)
        //        .Include(p => p.Category)
        //        .Include(p => p.ProductProductTags).ThenInclude(pt => pt.ProductTag)
        //        .Include(p => p.ProductImages).ThenInclude(pi => pi.Image)
        //        .Include(p => p.ProductAttributeCombination)
        //            .ThenInclude(pac => pac.ProductAttributeCombinationValues)
        //                .ThenInclude(pacv => pacv.ProductAttributeValue)
        //        .FirstOrDefaultAsync();
        //}

        public async Task DeactivateByProductIdAsync(int productId)
        {
            await _dbContext.ProductAttributeCombinations
                .Where(x => x.ProductId == productId)
                .ExecuteUpdateAsync(x =>
                    x.SetProperty(p => p.IsActive, false)
                     .SetProperty(p => p.IsDeleted, true));
        }
        public async Task<Product> GetBySlugAsync(string slug)
        {
            return await _dbContext.Products
                .AsNoTracking()
                .AsSplitQuery()
                .Where(p => !p.IsDeleted && p.Slug == slug)

                .Include(p => p.Category)

                .Include(p => p.ProductImages)
                    .ThenInclude(pi => pi.Image)

                .Include(p => p.ProductProductTags)
                    .ThenInclude(pt => pt.ProductTag)

                .Include(p => p.ProductAttributeCombination.Where(pc => pc.IsActive && !pc.IsDeleted))
                    .ThenInclude(pc => pc.ProductAttributeCombinationValues.Where(v => v.IsActive && !v.IsDeleted))
                        .ThenInclude(v => v.ProductAttribute)

                .Include(p => p.ProductAttributeCombination.Where(pc => pc.IsActive && !pc.IsDeleted))
                    .ThenInclude(pc => pc.ProductAttributeCombinationValues.Where(v => v.IsActive && !v.IsDeleted))
                        .ThenInclude(v => v.ProductAttributeValue)

                .Include(p => p.Reviews.Where(r => r.IsApproved))
                    .ThenInclude(r => r.Photos)
                        .ThenInclude(ph => ph.Image)

                .FirstOrDefaultAsync();
        }


        public virtual async Task<bool> IsSlugUniqueAsync(string slug, int? productIdToExclude = null)
        {
            var query = _dbContext.Products.AsQueryable();

            if (productIdToExclude.HasValue)
                query = query.Where(p => p.Id != productIdToExclude.Value);

            return !await query.AnyAsync(p => p.Slug == slug && !p.IsDeleted);
        }
    }
}
