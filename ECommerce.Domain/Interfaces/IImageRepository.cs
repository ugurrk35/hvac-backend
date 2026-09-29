using ECommerce.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface IImageRepository : IRepository<Image>
    {
        Task<IEnumerable<Image>> GetImagesByExtensionAsync(string extension);
        Task<IEnumerable<Image>> GetImagesBySizeRangeAsync(int minSizeInBytes, int maxSizeInBytes);
        Task<Image> GetImageByUrlAsync(string url);
        Task<IEnumerable<Image>> GetImagesUploadedAfterAsync(DateTime uploadDate);
        Task<IEnumerable<Image>> GetImagesUploadedBeforeAsync(DateTime uploadDate);
        Task<IEnumerable<Image>> GetImagesByDimensionsAsync(int width, int height);
        Task<long> GetTotalImagesSizeAsync();
        Task<IEnumerable<Image>> GetUnusedImagesAsync();
        Task<IEnumerable<Image>> GetImagesByProductIdAsync(int productId);
        Task<bool> IsImageInUseAsync(int imageId);
    }
}
