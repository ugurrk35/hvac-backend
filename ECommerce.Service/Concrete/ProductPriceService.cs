using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Service.Abstract;
using ECommerce.Service.Dtos.ProductDtos;
using ECommerce.Service.Mapping.Manual;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Service.Concrete
{
    public class ProductPriceService : IProductPriceService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductPriceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ProductPriceDto> GetByIdAsync(int id)
        {
            var entity = await _unitOfWork.ProductPriceRepository.GetByIdAsync(id);
            return entity.ToDto();
        }

        public async Task<IReadOnlyList<ProductPriceDto>> GetAllAsync()
        {
            var entities = await _unitOfWork.ProductPriceRepository.GetAllAsync();
            return entities.Select(entity => entity.ToDto()).ToList();
        }

        public async Task<ProductPriceDto> CreateAsync(ProductPriceDto dto)
        {
            var entity = dto.ToEntity();
            entity.CreatedAt = DateTime.UtcNow;
            entity.CreatedBy = "System";

            var added = await _unitOfWork.ProductPriceRepository.AddAsync(entity);
            await _unitOfWork.CompleteAsync();
            return added.ToDto();
        }

        public async Task UpdateAsync(int id, ProductPriceDto dto)
        {
            var entity = await _unitOfWork.ProductPriceRepository.GetByIdAsync(id);
            if (entity == null) throw new Exception("ProductPrice not found");

            dto.ApplyTo(entity);
            entity.LastModifiedAt = DateTime.UtcNow;
            entity.LastModifiedBy = "System";

            await _unitOfWork.ProductPriceRepository.UpdateAsync(entity);
            await _unitOfWork.CompleteAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _unitOfWork.ProductPriceRepository.GetByIdAsync(id);
            if (entity == null) throw new Exception("ProductPrice not found");

            await _unitOfWork.ProductPriceRepository.DeleteAsync(entity);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<(IReadOnlyList<ProductPriceDto> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, ProductPriceFilterDto filter)
        {
            var query = _unitOfWork.ProductPriceRepository.Query();

            if (filter.ProductId.HasValue)
                query = query.Where(x => x.ProductId == filter.ProductId.Value);
            if (filter.ProductAttributeCombinationId.HasValue)
                query = query.Where(x => x.ProductAttributeCombinationId == filter.ProductAttributeCombinationId.Value);
            if (filter.CustomerGroupId.HasValue)
                query = query.Where(x => x.CustomerGroupId == filter.CustomerGroupId.Value);
            if (filter.MinPrice.HasValue)
                query = query.Where(x => x.Price >= filter.MinPrice.Value);
            if (filter.MaxPrice.HasValue)
                query = query.Where(x => x.Price <= filter.MaxPrice.Value);
            if (filter.StartDate.HasValue)
                query = query.Where(x => x.StartDate >= filter.StartDate.Value);
            if (filter.EndDate.HasValue)
                query = query.Where(x => x.EndDate <= filter.EndDate.Value);

            var total = query.Count();
            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return (items.Select(item => item.ToDto()).ToList(), total);
        }

        public async Task BulkUpsertProductPricesAsync(BulkProductPriceDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var now = DateTime.UtcNow;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. Ürünleri filtrele: kategori ve/veya ürün bazlı
                var products = await _unitOfWork.ProductRepository.GetProductsByIdsOrCategoryAsync(dto.ProductIds, dto.CategoryId);

                foreach (var product in products)
                {
                    // 2. Varyantları filtrele
                    var combinations = product.ProductAttributeCombination?.ToList() ?? new List<ProductAttributeCombination>();

                    if (dto.ProductAttributeCombinationId.HasValue)
                        combinations = combinations
                            .Where(c => c != null && c.Id == dto.ProductAttributeCombinationId.Value)
                            .ToList();

                    if (!combinations.Any()) combinations.Add(null);

                    foreach (var combination in combinations)
                    {
                        // 3. Mevcut fiyatları al (OnlyActivePrices aktifse)
                        ProductPrice existingPrice = null;
                        if (dto.OnlyActivePrices)
                        {
                            existingPrice = await _unitOfWork.ProductPriceRepository.GetLatestPriceAsync(
                                product.Id,
                                combination?.Id,
                                dto.CustomerGroupId
                            );
                        }

                        // 4. Yeni fiyat hesapla
                        decimal newPrice = dto.Price;
                        decimal? newDiscount = dto.DiscountPrice;

                        if (dto.PercentageChange.HasValue && existingPrice != null)
                        {
                            newPrice = existingPrice.Price * (1 + dto.PercentageChange.Value / 100);
                            if (existingPrice.DiscountPrice.HasValue && dto.DiscountPrice.HasValue)
                                newDiscount = existingPrice.DiscountPrice * (1 + dto.PercentageChange.Value / 100);
                        }

                        // 5. Mevcut fiyat geçersiz kıl
                        if (existingPrice != null)
                        {
                            existingPrice.EndDate = now;
                            existingPrice.LastModifiedAt = now;
                            existingPrice.LastModifiedBy = "System";
                            await _unitOfWork.ProductPriceRepository.UpdateAsync(existingPrice);
                        }

                        // 6. Yeni fiyat ekle
                        var priceEntity = new ProductPrice
                        {
                            ProductId = product.Id,
                            ProductAttributeCombinationId = combination?.Id,
                            CustomerGroupId = dto.CustomerGroupId,
                            Price = newPrice,
                            DiscountPrice = newDiscount,
                            Currency = dto.Currency ?? "TRY",
                            StartDate = dto.StartDate ?? now,
                            EndDate = dto.EndDate,
                            CreatedAt = now,
                            CreatedBy = "System"
                        };

                        await _unitOfWork.ProductPriceRepository.AddAsync(priceEntity);
                    }
                }

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

    }
}
