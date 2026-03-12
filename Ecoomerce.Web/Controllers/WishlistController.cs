using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecoomerce.Web.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Wishlist", new { area = "Profile" });
        }
    }
}
