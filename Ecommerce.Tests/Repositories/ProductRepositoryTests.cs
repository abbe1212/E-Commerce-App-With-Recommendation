using Xunit;
using FluentAssertions;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Repositories;
using Ecommerce.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Ecommerce.Tests.Repositories
{
    public class ProductRepositoryTests
    {
        private static DbContextOptions<AppDbContext> CreateOptions() =>
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

        [Fact]
        public async Task SearchAsync_ByName_ReturnsMatching()
        {
            // Arrange
            var options = CreateOptions();
            using (var context = new AppDbContext(options))
            {
                context.Categories.AddRange(
                    new Category { CategoryID = 1, Name = "Electronics" },
                    new Category { CategoryID = 2, Name = "Phones" }
                );
                context.Tags.Add(new Tag { TagID = 1, Name = "Premium" });
                context.Products.AddRange(
                    new Product { ProductID = 1, Name = "Laptop Pro", CategoryID = 1, IsAvailable = true, Price = 999, StockQuantity = 5, Description = "Test", ImageURL = "https://example.com/img.jpg" },
                    new Product { ProductID = 2, Name = "Gaming Laptop", CategoryID = 1, IsAvailable = true, Price = 1299, StockQuantity = 3, Description = "Test", ImageURL = "https://example.com/img.jpg" },
                    new Product { ProductID = 3, Name = "Phone", CategoryID = 2, IsAvailable = true, Price = 699, StockQuantity = 10, Description = "Test", ImageURL = "https://example.com/img.jpg" }
                );
                context.ProductTags.AddRange(
                    new ProductTag { ProductTagID = 1, ProductID = 1, TagID = 1 },
                    new ProductTag { ProductTagID = 2, ProductID = 2, TagID = 1 }
                );
                context.SaveChanges();
            }

            using (var context = new AppDbContext(options))
            {
                var repository = new ProductRepository(context);

                // Act
                var result = await repository.SearchAsync("Laptop");

                // Assert
                result.Should().HaveCount(2);
                result.Should().AllSatisfy(p => p.Name.ToLower().Should().Contain("laptop"));
            }
        }

        [Fact]
        public async Task SearchAsync_ByCategoryId_Filters()
        {
            // Arrange
            var options = CreateOptions();
            using (var context = new AppDbContext(options))
            {
                context.Categories.AddRange(
                    new Category { CategoryID = 1, Name = "Electronics" },
                    new Category { CategoryID = 2, Name = "Phones" }
                );
                context.Products.AddRange(
                    new Product { ProductID = 1, Name = "Laptop Pro", CategoryID = 1, IsAvailable = true, Price = 999, StockQuantity = 5, Description = "Test", ImageURL = "https://example.com/img.jpg" },
                    new Product { ProductID = 2, Name = "Gaming Laptop", CategoryID = 1, IsAvailable = true, Price = 1299, StockQuantity = 3, Description = "Test", ImageURL = "https://example.com/img.jpg" },
                    new Product { ProductID = 3, Name = "Phone", CategoryID = 2, IsAvailable = true, Price = 699, StockQuantity = 10, Description = "Test", ImageURL = "https://example.com/img.jpg" }
                );
                context.SaveChanges();
            }

            using (var context = new AppDbContext(options))
            {
                var repository = new ProductRepository(context);

                // Act
                var result = await repository.SearchAsync(null, categoryId: 1);

                // Assert
                result.Should().HaveCount(2);
                result.Should().AllSatisfy(p => p.CategoryID.Should().Be(1));
            }
        }

        [Fact]
        public async Task SearchPagedAsync_ReturnsCorrectPage()
        {
            // Arrange
            var options = CreateOptions();
            using (var context = new AppDbContext(options))
            {
                context.Categories.Add(new Category { CategoryID = 1, Name = "Electronics" });
                for (int i = 1; i <= 15; i++)
                {
                    context.Products.Add(new Product
                    {
                        ProductID = i,
                        Name = $"Product {i}",
                        CategoryID = 1,
                        IsAvailable = true,
                        Price = 100 + i,
                        StockQuantity = 10,
                        Description = "Test",
                        ImageURL = "https://example.com/img.jpg"
                    });
                }
                context.SaveChanges();
            }

            using (var context = new AppDbContext(options))
            {
                var repository = new ProductRepository(context);

                // Act
                var result = await repository.SearchPagedAsync(null, null, null, null, null, null, page: 2, pageSize: 5);

                // Assert
                result.Products.Should().HaveCount(5);
                result.TotalCount.Should().Be(15);
            }
        }

        [Fact]
        public async Task GetByIdsAsync_ReturnsMatchingProducts()
        {
            // Arrange
            var options = CreateOptions();
            using (var context = new AppDbContext(options))
            {
                context.Categories.Add(new Category { CategoryID = 1, Name = "Electronics" });
                for (int i = 1; i <= 5; i++)
                {
                    context.Products.Add(new Product
                    {
                        ProductID = i,
                        Name = $"Product {i}",
                        CategoryID = 1,
                        IsAvailable = true,
                        Price = 100 + i,
                        StockQuantity = 10,
                        Description = "Test",
                        ImageURL = "https://example.com/img.jpg"
                    });
                }
                context.SaveChanges();
            }

            using (var context = new AppDbContext(options))
            {
                var repository = new ProductRepository(context);

                // Act
                var result = await repository.GetByIdsAsync(new List<int> { 1, 3, 5 });

                // Assert
                result.Should().HaveCount(3);
                result.Select(p => p.ProductID).Should().BeEquivalentTo(new[] { 1, 3, 5 });
            }
        }
    }
}
