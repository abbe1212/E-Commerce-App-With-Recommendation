using Ecommerce.Core.Entities;
using Ecommerce.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Core.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        /// Gets a specific number of featured products.
        Task<IEnumerable<Product>> GetFeaturedProductsAsync(int count);

        /// Searches products by name, description, tags. Can filter by category/brand.
        Task<IEnumerable<Product>> SearchAsync(string searchTerm, int? categoryId = null, int? brandId = null);

        /// Gets all products belonging to a specific category.
        Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId);

        /// Gets all products belonging to a specific brand.
        Task<IEnumerable<Product>> GetByBrandAsync(int brandId);

        /// Gets the top-selling products (based on order history, might need join).
        /// Implementation might involve a service or direct query.
        Task<IEnumerable<Product>> GetTopSellingAsync(int count);

        /// Gets products whose stock is at or below a given threshold.
        Task<IEnumerable<Product>> GetLowStockAsync(int threshold);

        /// Checks if a specific quantity of a product is available.
        Task<bool> IsInStockAsync(int productId, int quantity);

        /// Gets the current stock level for a product.
        Task<int> GetStockQuantityAsync(int productId);

        /// Gets products within a specific price range.
        Task<IEnumerable<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);

        /// Gets all products that are currently marked as available.
        Task<IEnumerable<Product>> GetAvailableProductsAsync();

        /// Gets products associated with a specific tag.
        Task<IEnumerable<Product>> GetByTagAsync(int tagId);

        /// Gets products associated with any of the specified tags.
        Task<IEnumerable<Product>> GetByAnyTagsAsync(IEnumerable<int> tagIds);

        /// Gets a single product by its ID, including related details like Category, Brand, and Reviews.
        Task<Product?> GetProductWithDetailsAsync(int id);

        /// <summary>
        /// Server-side paginated search. ALL filtering/sorting/paging done in SQL.
        /// Returns the products for the requested page AND the total matching count.
        /// </summary>
        Task<(IEnumerable<Product> Products, int TotalCount)> SearchPagedAsync(
            string? searchTerm,
            int? categoryId,
            int? brandId,
            decimal? minPrice,
            decimal? maxPrice,
            string? sortBy,
            int page,
            int pageSize);

        /// <summary>
        /// Returns products matching IDs — single WHERE IN query (used by Details page recently-viewed).
        /// </summary>
        Task<IEnumerable<Product>> GetByIdsAsync(IEnumerable<int> ids);

        /// <summary>
        /// Returns latest N products sorted by CreatedAt DESC — no full-table load.
        /// </summary>
        Task<IEnumerable<Product>> GetLatestAsync(int count);

        /// <summary>
        /// Returns products on sale (DiscountPercentage > 0) — server-side filtered.
        /// </summary>
        Task<IEnumerable<Product>> GetOnSaleAsync(int count);
    }
}
