using Ecommerce.Core.Entities;
using Ecommerce.Core.Interfaces;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Infrastructure.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        // Done
        public async Task<IEnumerable<Product>> GetAvailableProductsAsync()
        {
            return await _context.Products
                .Where(p => p.IsAvailable)
                .ToListAsync();
        }

        // Done
        public async Task<IEnumerable<Product>> GetByAnyTagsAsync(IEnumerable<int> tagIds)
        {
            var tagIdList = tagIds.ToList();
            return await _context.Products
                .Where(p => p.ProductTags.Any(pt => tagIdList.Contains(pt.TagID)))
                .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
                .ToListAsync();
        }

        // Done
        public async Task<IEnumerable<Product>> GetByBrandAsync(int brandId)
        {
            return await _context.Products
                .Where(p=>p.BrandID == brandId)
                .Include(p => p.Brand)
                .ToListAsync();
        }

        // Done
        public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId)
        {
            return await _context.Products
                .Where(p => p.CategoryID == categoryId)
                .Include(p => p.Category)
                .ToListAsync();
        }

        // Done
        public  async Task<IEnumerable<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice)
        {
            return await _context.Products
                .Where(p => p.Price >= minPrice && p.Price <= maxPrice)
                .ToListAsync();
        }

        // Done
        public async Task<IEnumerable<Product>> GetByTagAsync(int tagId)
        {
            return await _context.Products
               .Where(p => p.ProductTags.Any(pt => pt.TagID == tagId))
               .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
               .ToListAsync();
        }

        // Done
        public async Task<IEnumerable<Product>> GetFeaturedProductsAsync(int count)
        {
            return await _context.Products
               .Where(p => p.IsAvailable && p.IsFeatured)
               .OrderByDescending(p => p.CreatedAt)
               .Take(count)
               .Include(p => p.Category)
               .Include(p => p.Brand)
               .Include(p => p.ProductImages)
               .ToListAsync();
        }

        // Done
        public async Task<IEnumerable<Product>> GetLowStockAsync(int threshold)
        {
            return await _context.Products
                .Where(p => p.StockQuantity <= threshold)
                .ToListAsync();
        }

        // Done
        public async Task<Product?> GetProductWithDetailsAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)           // تحميل الفئة المرتبطة
                .Include(p => p.Brand)              // تحميل الماركة المرتبطة
                .Include(p => p.Reviews)            // تحميل قائمة التقييمات
                .Include(p => p.ProductTags)        // تحميل جدول الربط للـ Tags
                    .ThenInclude(pt => pt.Tag)      // ثم تحميل الـ Tag نفسه
                .Include(p => p.ProductImages)      // تحميل صور المنتج
                .Include(p => p.ProductVariants)    // تحميل الـ Variants (Size, Color)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.ProductID == id);
        }

        // Done
        public async Task<int> GetStockQuantityAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            return product?.StockQuantity ?? 0;
        }

        // Done
        public async Task<IEnumerable<Product>> GetTopSellingAsync(int count)
        {
            return await _context.Products
                   .Join(
                       _context.OrderItems,
                       product => product.ProductID,
                       orderItem => orderItem.ProductID,
                       (product, orderItem) => new { Product = product, Quantity = orderItem.Quantity }
                   )
                   .GroupBy(x => x.Product.ProductID)
                   .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(x => x.Quantity) })
                   .OrderByDescending(x => x.TotalQuantity)
                   .Take(count)
                   .Join(_context.Products, x => x.ProductId, p => p.ProductID, (x, p) => p)
                   .ToListAsync();
        }

        // Done
        public async Task<bool> IsInStockAsync(int productId, int quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            return product != null && product.StockQuantity >= quantity;
        }

        // Done
        public async Task<IEnumerable<Product>> SearchAsync(string searchTerm, int? categoryId = null, int? brandId = null)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p =>
                p.Name.Contains(searchTerm) ||
                (p.Description != null && p.Description.Contains(searchTerm)) ||
                p.ProductTags.Any(pt => pt.Tag.Name.Contains(searchTerm))
                );
            }

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryID == categoryId.Value);
            

            if(brandId.HasValue)
                query = query.Where(p=>p.BrandID == brandId.Value);

            return await query
                 .Include(p => p.Category)
                 .Include(p => p.Brand)
                 .Include(p => p.ProductTags)
                 .ThenInclude(pt => pt.Tag)
                 .ToListAsync();
        }

        /// <summary>
        /// Server-side paginated search. ALL filtering/sorting/paging done in SQL.
        /// </summary>
        public async Task<(IEnumerable<Product> Products, int TotalCount)> SearchPagedAsync(
            string? searchTerm, int? categoryId, int? brandId,
            decimal? minPrice, decimal? maxPrice, string? sortBy, int page, int pageSize)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(p =>
                    p.Name.Contains(searchTerm) ||
                    (p.Description != null && p.Description.Contains(searchTerm)));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryID == categoryId.Value);

            if (brandId.HasValue)
                query = query.Where(p => p.BrandID == brandId.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            // Count BEFORE paging (single COUNT(*) query)
            var totalCount = await query.CountAsync();

            // Apply sort
            query = sortBy?.ToLower() switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                "rating" => query.OrderByDescending(p => p.Reviews.Average(r => (double?)r.Rating) ?? 0),
                _ => query.OrderBy(p => p.Name)
            };

            // OFFSET/FETCH — SQL does the paging
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (products, totalCount);
        }

        /// <summary>
        /// Returns products matching IDs — single WHERE IN query.
        /// </summary>
        public async Task<IEnumerable<Product>> GetByIdsAsync(IEnumerable<int> ids)
        {
            return await _context.Products
                .Where(p => ids.Contains(p.ProductID))
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .ToListAsync();
        }

        /// <summary>
        /// Returns latest N products sorted by CreatedAt DESC.
        /// </summary>
        public async Task<IEnumerable<Product>> GetLatestAsync(int count)
        {
            return await _context.Products
                .Where(p => p.IsAvailable)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .ToListAsync();
        }

        /// <summary>
        /// Returns products on sale (DiscountPercentage > 0).
        /// </summary>
        public async Task<IEnumerable<Product>> GetOnSaleAsync(int count)
        {
            return await _context.Products
                .Where(p => p.IsAvailable && p.DiscountPercentage > 0)
                .OrderByDescending(p => p.DiscountPercentage)
                .Take(count)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .ToListAsync();
        }
    }
}
