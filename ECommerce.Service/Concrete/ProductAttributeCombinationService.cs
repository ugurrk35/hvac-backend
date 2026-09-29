using ECommerce.Domain.Entity;
using ECommerce.Domain.Interfaces;
using ECommerce.Service.Abstract;
using ECommerce.Service.Concrete.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Concrete
{
    public class ProductAttributeCombinationService : Service<ProductAttributeCombination>, IProductAttributeCombinationService
    {
        private readonly IProductAttributeCombinationRepository _productAttributeCombinationRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ProductAttributeCombinationService(IProductAttributeCombinationRepository productAttributeCombinationRepository, IUnitOfWork unitOfWork)
            : base(productAttributeCombinationRepository, unitOfWork)
        {
            _productAttributeCombinationRepository = productAttributeCombinationRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<ProductAttributeCombination>> GetCombinationsByProductIdAsync(int productId)
        {
            return await _productAttributeCombinationRepository.GetCombinationsByProductIdAsync(productId);
        }

        public async Task<ProductAttributeCombination?> GetCombinationWithValuesAsync(int id)
        {
            return await _productAttributeCombinationRepository.GetCombinationWithValuesAsync(id);
        }

        public async Task<ProductAttributeCombination?> GetCombinationBySkuAsync(string sku)
        {
            return await _productAttributeCombinationRepository.GetCombinationBySkuAsync(sku);
        }

        public async Task<bool> IsSkuUniqueAsync(string sku, int? excludeId = null)
        {
            var existingCombination = await _productAttributeCombinationRepository.GetCombinationBySkuAsync(sku);

            if (existingCombination == null)
                return true;

            if (excludeId.HasValue && existingCombination.Id == excludeId.Value)
                return true;

            return false;
        }
        public async Task<List<string>> ValidateCombinationAsync(ProductAttributeCombination entity)
        {
            var errors = new List<string>();

            // SKU boş olmamalı
            if (string.IsNullOrWhiteSpace(entity.Sku))
                errors.Add("SKU is required.");

            // Fiyat negatif olamaz
            if (entity.Price < 0)
                errors.Add("Price cannot be negative.");

            // Stok miktarı negatif olamaz
            if (entity.Quantity < 0)
                errors.Add("Quantity cannot be negative.");

            // SKU benzersiz olmalı
            var isUnique = await IsSkuUniqueAsync(entity.Sku, entity.Id > 0 ? entity.Id : null);
            if (!isUnique)
                errors.Add("SKU must be unique.");

            // Ürün ID geçerli olmalı
            if (entity.ProductId <= 0)
                errors.Add("Valid ProductId is required.");

            // En az 1 özellik değeri seçilmeli
            if (entity.ProductAttributeCombinationValues == null || !entity.ProductAttributeCombinationValues.Any())
                errors.Add("At least one attribute value must be selected.");

            // İçerideki değerlerde boşluk kontrolü
            foreach (var value in entity.ProductAttributeCombinationValues)
            {
                if (value.ProductAttributeId <= 0)
                    errors.Add("Each attribute value must have a valid ProductAttributeId.");
            }

            return errors;
        }

    }
}
