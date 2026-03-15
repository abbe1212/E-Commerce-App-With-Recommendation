using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ecommerce.Application.DTOs.Products;

namespace Ecommerce.Application.Services.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllProductsAsync();
        Task<ProductDto> GetProductByIdAsync(int productId);
        Task<IEnumerable<ProductDto>> GetFeaturedProductsAsync(int cunt);
        Task<IEnumerable<ProductDto>> SearchProductAsync(string searchTerm,int? categoryId =null);
        Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(int categoryId);
        Task<IEnumerable<ProductDto>> GetTopSellingProductsAsync(int count);

        /// <summary>
        /// Server-side paginated search with filtering, sorting, and paging.
        /// </summary>
        Task<(IEnumerable<ProductDto> Products, int TotalCount)> SearchPagedAsync(
            string? searchTerm, int? categoryId, int? brandId,
            decimal? minPrice, decimal? maxPrice, string? sortBy, int page, int pageSize);

        /// <summary>
        /// Gets latest N products.
        /// </summary>
        Task<IEnumerable<ProductDto>> GetLatestProductsAsync(int count);

        /// <summary>
        /// Gets products on sale.
        /// </summary>
        Task<IEnumerable<ProductDto>> GetOnSaleProductsAsync(int count);

        /// <summary>
        /// Gets products by list of IDs (single query).
        /// </summary>
        Task<IEnumerable<ProductDto>> GetByIdsAsync(IEnumerable<int> ids);
    }
}
