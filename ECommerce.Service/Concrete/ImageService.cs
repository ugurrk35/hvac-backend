using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Service.Abstract;
using ECommerce.Service.Concrete.Base;
using ECommerce.Service.Dtos.ImageDtos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Concrete
{
    public class ImageService : Service<Image>, IImageService
    {
        private const long MaxUploadBytes = 5 * 1024 * 1024;
        private const long MaxImagePixels = 24_000_000;
        private readonly IImageRepository _imageRepository;
        private readonly ILogger<ImageService> _logger;
        private readonly string _uploadPath;
        private readonly IUnitOfWork _unitOfWork;


        public ImageService(IImageRepository imageRepository, IUnitOfWork unitOfWork,ILogger<ImageService> logger)
            : base(imageRepository,unitOfWork)
        {
            _imageRepository = imageRepository;
            _logger = logger;
            _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "images");

            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        // Override base methods if needed
        public override async Task DeleteAsync(Image entity)
        {
            try
            {
                // Fiziksel dosyayı sil
                var filePath = Path.Combine(_uploadPath, Path.GetFileName(entity.Url));
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                await base.DeleteAsync(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image with id {ImageId}", entity.Id);
                throw;
            }
        }

        public override async Task SoftDeleteAsync(Image entity)
        {
            // Soft delete işleminde dosyayı silmiyoruz, sadece entity'i soft delete yapıyoruz
            await base.SoftDeleteAsync(entity);
        }

        // Custom methods
        public async Task<Image> UploadImageAsync(IFormFile file, string title, string altText = null, string caption = null)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null");

            if (file.Length > MaxUploadBytes)
                throw new ArgumentException("File size cannot exceed 5 MB");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
                throw new ArgumentException("Invalid file extension");

            await using var input = file.OpenReadStream();
            var format = SixLabors.ImageSharp.Image.DetectFormat(input);
            input.Position = 0;
            using var image = SixLabors.ImageSharp.Image.Load(input);
            var detectedExtension = format.Name.ToUpperInvariant() switch
            {
                "JPEG" => ".jpg",
                "PNG" => ".png",
                "WEBP" => ".webp",
                _ => throw new ArgumentException("Unsupported image content")
            };

            if (image.Width <= 0 || image.Height <= 0 || (long)image.Width * image.Height > MaxImagePixels)
                throw new ArgumentException("Image dimensions are not allowed");

            var fileName = $"{Guid.NewGuid()}{detectedExtension}";
            var filePath = Path.Combine(_uploadPath, fileName);
            var url = $"/uploads/images/{fileName}";
            await using (var output = File.Create(filePath))
            {
                switch (detectedExtension)
                {
                    case ".jpg": await image.SaveAsync(output, new JpegEncoder { Quality = 90 }); break;
                    case ".png": await image.SaveAsync(output, new PngEncoder()); break;
                    case ".webp": await image.SaveAsync(output, new WebpEncoder { Quality = 90 }); break;
                }
            }

                var imageEntity = new Domain.Entity.Image
                {
                    Title = title,
                    AltText = altText,
                    Caption = caption,
                    Url = url,
                    Width = image.Width,
                    Height = image.Height,
                    FileExtension = detectedExtension,
                    SizeInBytes = checked((int)new FileInfo(filePath).Length),
                    UploadedAt = DateTime.UtcNow
                };

            return await AddAsync(imageEntity);
        }

        public async Task<IEnumerable<Image>> GetImagesByExtensionAsync(string extension)
        {
            return await _imageRepository.GetImagesByExtensionAsync(extension);
        }

        public async Task<IEnumerable<Image>> GetImagesBySizeRangeAsync(int minSize, int maxSize)
        {
            return await _imageRepository.GetImagesBySizeRangeAsync(minSize, maxSize);
        }

        public async Task<Image> GetImageByUrlAsync(string url)
        {
            return await _imageRepository.GetImageByUrlAsync(url);
        }

        public async Task<IEnumerable<Image>> GetImagesUploadedAfterAsync(DateTime uploadDate)
        {
            return await _imageRepository.GetImagesUploadedAfterAsync(uploadDate);
        }

        public async Task<IEnumerable<Image>> GetImagesByDimensionsAsync(int width, int height)
        {
            return await _imageRepository.GetImagesByDimensionsAsync(width, height);
        }

        public async Task<long> GetTotalImagesSizeAsync()
        {
            return await _imageRepository.GetTotalImagesSizeAsync();
        }

        public async Task<IEnumerable<Image>> GetUnusedImagesAsync()
        {
            return await _imageRepository.GetUnusedImagesAsync();
        }

        public async Task<bool> DeleteUnusedImagesAsync()
        {
            try
            {
                var unusedImages = await GetUnusedImagesAsync();

                foreach (var image in unusedImages)
                {
                    await DeleteAsync(image);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting unused images");
                return false;
            }
        }

        public async Task<IEnumerable<Image>> GetImagesByProductIdAsync(int productId)
        {
            return await _imageRepository.GetImagesByProductIdAsync(productId);
        }

        public async Task<bool> IsImageInUseAsync(int imageId)
        {
            return await _imageRepository.IsImageInUseAsync(imageId);
        }

        public async Task<bool> OptimizeImageAsync(int imageId)
        {
            try
            {
                var imageEntity = await GetByIdAsync(imageId);
                if (imageEntity == null) return false;

                var filePath = Path.Combine(_uploadPath, Path.GetFileName(imageEntity.Url));
                if (!File.Exists(filePath)) return false;

                using (var image = SixLabors.ImageSharp.Image.Load(filePath))
                {
                    // Resim optimizasyonu (kalite ayarı)
                    var encoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder()
                    {
                        Quality = 85
                    };

                    var backupPath = filePath + ".backup";
                    File.Move(filePath, backupPath);

                    // Yeni dosyayı oluştur ve encode ederek yaz
                    await using (var outputStream = File.Create(filePath))
                    {
                        await image.SaveAsync(outputStream, encoder);
                    }

                    // Yeni boyutu al ve güncelle
                    var newFileInfo = new FileInfo(filePath);
                    imageEntity.SizeInBytes = (int)newFileInfo.Length;

                    await UpdateAsync(imageEntity);

                    // Backup dosyasını sil
                    File.Delete(backupPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing image with id {ImageId}", imageId);
                return false;
            }
        }


        public async Task<Image> ResizeImageAsync(int imageId, int newWidth, int newHeight)
        {
            var imageEntity = await GetByIdAsync(imageId);
            if (imageEntity == null) return null;

            var filePath = Path.Combine(_uploadPath, Path.GetFileName(imageEntity.Url));
            if (!File.Exists(filePath)) return null;

            var newFileName = $"{Guid.NewGuid()}{imageEntity.FileExtension}";
            var newFilePath = Path.Combine(_uploadPath, newFileName);
            var newUrl = $"/uploads/images/{newFileName}";

            using (var image = SixLabors.ImageSharp.Image.Load(filePath))
            {
                image.Mutate(x => x.Resize(newWidth, newHeight));

                await using var stream = File.Create(newFilePath);
                await image.SaveAsync(stream, new JpegEncoder()); // veya başka encoder
            

            var newFileInfo = new FileInfo(newFilePath);

                var newImageEntity = new Image
                {
                    Title = $"{imageEntity.Title} (Resized)",
                    AltText = imageEntity.AltText,
                    Caption = imageEntity.Caption,
                    Url = newUrl,
                    Width = newWidth,
                    Height = newHeight,
                    FileExtension = imageEntity.FileExtension,
                    SizeInBytes = (int)newFileInfo.Length,
                    UploadedAt = DateTime.UtcNow
                };

                return await AddAsync(newImageEntity);
            }
        }

        public async Task<Image> UpdateImageAsync(UpdateImageDto dto)
        {
            var image = await GetByIdAsync(dto.Id);
            if (image == null) throw new Exception("Image not found");
            image.Title = dto.Title ?? image.Title;
            image.AltText = dto.AltText ?? image.AltText;
            image.Caption = dto.Caption ?? image.Caption;
            image.Url = dto.Url ?? image.Url;
            await UpdateAsync(image);
            return image;
        }

        public async Task<IEnumerable<Image>> SearchImagesAsync(string searchTerm)
        {
            var all = await GetAllAsync();
            return all.Where(img =>
                (!string.IsNullOrEmpty(img.Title) && img.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(img.AltText) && img.AltText.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(img.Caption) && img.Caption.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            );
        }
    }
}
