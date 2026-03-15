using AutoMapper;
using Ecommerce.Application.DTOs.Inventory;
using Ecommerce.Application.DTOs.Products;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Core.Entities;
using Ecommerce.Core.Enums;
using Ecommerce.Core.Interfaces;

namespace Ecommerce.Application.Services.Implementations
{
    public class InventoryService : IInventoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProductRepository _productRepository;
        private readonly IInventoryLogRepository _inventoryLogRepository;
        private readonly IMapper _mapper;

        public InventoryService(IUnitOfWork unitOfWork, IProductRepository productRepository, IInventoryLogRepository inventoryLogRepository, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _productRepository = productRepository;
            _inventoryLogRepository = inventoryLogRepository;
            _mapper = mapper;
        }

        // Done
        public async Task<IEnumerable<InventoryLogDto>> GetInventoryLogsAsync(int productId)
        {
            var logs = await _inventoryLogRepository.GetLogsByProductIdAsync(productId);
           return _mapper.Map<IEnumerable<InventoryLogDto>>(logs);
        }

        // Done
        public async Task<IEnumerable<ProductDto>> GetLowStockProductsAsync(int threshold)
        {
            var products = await _productRepository.GetLowStockAsync(threshold);
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }

        // Done
        public async Task<int> GetProductStockAsync(int productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            return product?.StockQuantity ?? 0;
        }

        // Done
        public async Task<bool> IsProductAvailableAsync(int productId, int quantity)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            return product != null && product.StockQuantity >= quantity;
        }

        // Done
        public async Task UpdateStockAsync(int productId, int quantity, InventoryChangeType changeType, string description)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if(product == null)
                throw new ArgumentException("Product not found", nameof(productId));
            product.StockQuantity += quantity;
            product.UpdatedAt = DateTime.UtcNow;

            await _productRepository.UpdateAsync(product);

            var inventoryLog = new InventoryLog
            {
                ProductID = productId,
                ChangeType = changeType,
                QuantityChanged = quantity,
                NewStockLevel = product.StockQuantity,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };
            await _inventoryLogRepository.AddAsync(inventoryLog);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
