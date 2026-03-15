using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable , IAsyncDisposable
    {
        // Repository Properties
        IUserRepository Users { get; }
        IProductRepository Products { get; }
        IOrderRepository Orders { get; }
        IInventoryLogRepository InventoryLogs { get; }
        ICartRepository Carts { get; }
        ICategoryRepository Categories { get; }
        IBrandRepository Brands { get; }
        IPaymentRepository Payments { get; }
        IShippingRepository Shippings { get; }
        IReviewRepository Reviews { get; }
        IPromoCodeRepository PromoCodes { get; }
        IWishlistRepository Wishlists { get; }
        IActivityLogRepository ActivityLogs { get; }
        
        // Transaction Methods
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
