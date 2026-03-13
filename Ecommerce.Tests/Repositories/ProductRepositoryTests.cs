using Xunit;
using FluentAssertions;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Repositories;
using Ecommerce.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Tests.Repositories
{
    public class ProductRepositoryTests
    {
        private static DbContextOptions<AppDbContext> CreateOptions() =>
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        [Fact]
        public async Task SearchAsync_ByName_ReturnsMatching()
        {
            // Arrange
            var options = CreateOptions();
            using (var context = new AppDbContext(options))
            {
                context.Products.AddRange(
                    new Product { ProductID = 1, Name = "Laptop Pro", CategoryID = 1, IsAvailable = true, Price = 999, StockQuantity = 5 },
                    new Product { ProductID = 2, Name = "Gaming Laptop", CategoryID = 1, IsAvailable = true, Price = 1299, StockQuantity = 3 },
                    new Product { ProductID = 3, Name = "Phone", CategoryID = 2, IsAvailable = true, Price = 699, StockQuantity = 10 }
                );
                context.SaveChanges();
            }

            using (var context = new AppDbContext(options))
            {
                var repository = new ProductRepository(context);

                // Act
                var result = await repository.SearchAsync("laptop");

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
                context.Products.AddRange(
                    new Product { ProductID = 1, Name = "Laptop Pro", CategoryID = 1, IsAvailable = true, Price = 999, StockQuantity = 5 },
                    new Product { ProductID = 2, Name = "Gaming Laptop", CategoryID = 1, IsAvailable = true, Price = 1299, StockQuantity = 3 },
                    new Product { ProductID = 3, Name = "Phone", CategoryID = 2, IsAvailable = true, Price = 699, StockQuantity = 10 }
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
                for (int i = 1; i <= 15; i++)
                {
                    context.Products.Add(new Product
                    {
                        ProductID = i,
                        Name = $"Product {i}",
                        CategoryID = 1,
                        IsAvailable = true,
                        Price = 100 + i,
                        StockQuantity = 10
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
                for (int i = 1; i <= 5; i++)
                {
                    context.Products.Add(new Product
                    {
                        ProductID = i,
                        Name = $"Product {i}",
                        CategoryID = 1,
                        IsAvailable = true,
                        Price = 100 + i,
                        StockQuantity = 10
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
