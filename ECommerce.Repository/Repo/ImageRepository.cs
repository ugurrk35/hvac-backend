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
    public class ImageRepository : GenericRepository<Image>, IImageRepository
    {
        public ImageRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Image>> GetImagesByExtensionAsync(string extension)
        {
            return await _dbContext.Images
                .Where(i => i.FileExtension.ToLower() == extension.ToLower())
                .OrderBy(i => i.Title)
                .ToListAsync();
        }

        public async Task<IEnumerable<Image>> GetImagesBySizeRangeAsync(int minSizeInBytes, int maxSizeInBytes)
        {
            return await _dbContext.Images
                .Where(i => i.SizeInBytes >= minSizeInBytes && i.SizeInBytes <= maxSizeInBytes)
                .OrderBy(i => i.SizeInBytes)
            .ToListAsync();
        }

        public async Task<Image> GetImageByUrlAsync(string url)
        {
            return await _dbContext.Images
                .FirstOrDefaultAsync(i => i.Url == url);
        }

        public async Task<IEnumerable<Image>> GetImagesUploadedAfterAsync(DateTime uploadDate)
        {
            return await _dbContext.Images
                .Where(i => i.UploadedAt > uploadDate)
                .OrderByDescending(i => i.UploadedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Image>> GetImagesUploadedBeforeAsync(DateTime uploadDate)
        {
            return await _dbContext.Images
                .Where(i => i.UploadedAt < uploadDate)
                .OrderByDescending(i => i.UploadedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Image>> GetImagesByDimensionsAsync(int width, int height)
        {
            return await _dbContext.Images
                .Where(i => i.Width == width && i.Height == height)
                .OrderBy(i => i.Title)
            .ToListAsync();
        }

        public async Task<long> GetTotalImagesSizeAsync()
        {
            return await _dbContext.Images
                .SumAsync(i => (long)i.SizeInBytes);
        }

        public async Task<IEnumerable<Image>> GetUnusedImagesAsync()
        {
            return await _dbContext.Images
                .Where(i => !i.ProductImages.Any() &&
                           !i.BlogPostImage.Any() &&
                           !i.SeoFriendlyImage.Any())
                .OrderBy(i => i.UploadedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Image>> GetImagesByProductIdAsync(int productId)
        {
            return await _dbContext.Images
                .Include(i => i.ProductImages)
                .Where(i => i.ProductImages.Any(pi => pi.ProductId == productId))
                .OrderBy(i => i.ProductImages.First(pi => pi.ProductId == productId).SortOrder)
                .ToListAsync();
        }

        public async Task<bool> IsImageInUseAsync(int imageId)
        {
            return await _dbContext.Images
                .AnyAsync(i => i.Id == imageId &&
                              (i.ProductImages.Any() ||
                               i.BlogPostImage.Any() ||
                               i.SeoFriendlyImage.Any()));
        }
    }
}
