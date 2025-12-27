using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Core.Entities
{
    public class WishlistItem
    {
        public int WishlistItemID { get; set; }

        [Required]
        public int WishlistID { get; set; }
        public virtual Wishlist Wishlist { get; set; }

        [Required]
        public int ProductID { get; set; }
        public virtual Product Product { get; set; }
        
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}

