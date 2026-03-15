using AutoMapper;
using Ecommerce.Application.DTOs.Products;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Core.Interfaces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.Services.Implementations
{
    public class RecommendationService : IRecommendationService
    {
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IMapper _mapper;

        public RecommendationService(IProductRepository productRepository, IOrderRepository orderRepository, IMapper mapper)
        {
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _mapper = mapper;
        }

        // Done
        public async Task<IEnumerable<ProductDto>> GetFrequentlyBoughtTogetherAsync(int productId, int count = 5)
        {
            var ordersWithProduct = await _orderRepository.ListAsync(o => o.OrderItems.Any(oi => oi.ProductID == productId));
            var frequentlyBoughtProductIds = ordersWithProduct
                .SelectMany(o=>o.OrderItems)
                .Where(oi => oi.ProductID != productId)
                .GroupBy(oi => oi.ProductID)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(count);

            var products = await _productRepository.ListAsync(p => frequentlyBoughtProductIds.Contains(p.ProductID));
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }

        // Done
        public async Task<IEnumerable<ProductDto>> GetPersonalizedRecommendationsAsync(string userId)
        {
            var userOrders = await _orderRepository.GetOrdersByUserIdAsync(userId);

            if(!userOrders.Any())
                return _mapper.Map<IEnumerable<ProductDto>>(await _productRepository.GetTopSellingAsync(10));

            var purchasedProductsIds = userOrders
                .SelectMany(o => o.OrderItems)
                .Select(oi => oi.ProductID)
                .Distinct();

            var purchasedProducts = await _productRepository
                .ListAsync(p => purchasedProductsIds
                .Contains(p.ProductID));

            var purchasedCategoryIds = purchasedProducts
                .Select(p => p.CategoryID)
                .Distinct();

            var recommendedProducts = await _productRepository
                .ListAsync(p=>
                purchasedCategoryIds.Contains(p.CategoryID) &&
                !purchasedProductsIds.Contains(p.ProductID)
           );
            return _mapper.Map<IEnumerable<ProductDto>>(recommendedProducts.Take(10));
        }


        // Done
        public async Task<IEnumerable<ProductDto>> GetRelatedProductsAsync(int productId ,int count = 5)
        {
            var product = await _productRepository.GetProductWithDetailsAsync(productId);
            if (product == null)
                return Enumerable.Empty<ProductDto>();

            var relatedProducts = await _productRepository
                .GetByCategoryAsync(product.CategoryID);

            var filteredResult = relatedProducts
                .Where(p => p.ProductID != productId)
                .Take(count);

            return _mapper.Map<IEnumerable<ProductDto>>(filteredResult);
        }
    }
}
