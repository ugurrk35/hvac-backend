using ECommerce.Domain.Entity;
using ECommerce.Service.Abstract.Base;
using ECommerce.Service.Dtos.ImageDtos;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Abstract
{
    public interface IImageService : IService<Image>
    {
        Task<Image> UploadImageAsync(IFormFile file, string title, string altText = null, string caption = null);
        Task<IEnumerable<Image>> GetImagesByExtensionAsync(string extension);
        Task<IEnumerable<Image>> GetImagesBySizeRangeAsync(int minSize, int maxSize);
        Task<Image> GetImageByUrlAsync(string url);
        Task<IEnumerable<Image>> GetImagesUploadedAfterAsync(DateTime uploadDate);
        Task<IEnumerable<Image>> GetImagesByDimensionsAsync(int width, int height);
        Task<long> GetTotalImagesSizeAsync();
        Task<IEnumerable<Image>> GetUnusedImagesAsync();
        Task<bool> DeleteUnusedImagesAsync();
        Task<IEnumerable<Image>> GetImagesByProductIdAsync(int productId);
        Task<bool> IsImageInUseAsync(int imageId);
        Task<bool> OptimizeImageAsync(int imageId);
        Task<Image> ResizeImageAsync(int imageId, int newWidth, int newHeight);
        Task<Image> UpdateImageAsync(UpdateImageDto dto);
        Task<IEnumerable<Image>> SearchImagesAsync(string searchTerm);
    }
}
