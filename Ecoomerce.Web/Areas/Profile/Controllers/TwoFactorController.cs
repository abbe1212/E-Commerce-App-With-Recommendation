using Ecommerce.Application.ViewModels.User___Account;
using Ecommerce.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Encodings.Web;

namespace Ecoomerce.Web.Areas.Profile.Controllers
{
    [Area("Profile")]
    [Authorize]
    [Route("Profile/[controller]")]
    public class TwoFactorController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UrlEncoder _urlEncoder;
        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        public TwoFactorController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            UrlEncoder urlEncoder)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _urlEncoder = urlEncoder;
        }

        // GET: Profile/TwoFactor
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new TwoFactorSettingsViewModel
            {
                IsTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
                HasAuthenticator = await _userManager.GetAuthenticatorKeyAsync(user) != null,
                RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user)
            };

            return View(model);
        }

        // GET: Profile/TwoFactor/EnableAuthenticator
        [HttpGet]
        [Route("EnableAuthenticator")]
        public async Task<IActionResult> EnableAuthenticator()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            await LoadSharedKeyAndQrCodeUriAsync(user);
            var model = new EnableTwoFactorViewModel
            {
                SharedKey = ViewData["SharedKey"]?.ToString(),
                AuthenticatorUri = ViewData["AuthenticatorUri"]?.ToString()
            };

            return View(model);
        }

        // POST: Profile/TwoFactor/EnableAuthenticator
        [HttpPost]
        [Route("EnableAuthenticator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableAuthenticator(VerifyTwoFactorViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await LoadSharedKeyAndQrCodeUriAsync(user);
                return View(new EnableTwoFactorViewModel
                {
                    SharedKey = ViewData["SharedKey"]?.ToString(),
                    AuthenticatorUri = ViewData["AuthenticatorUri"]?.ToString()
                });
            }

            // Verify the code
            var verificationCode = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
            var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!is2faTokenValid)
            {
                ModelState.AddModelError("Code", "Invalid verification code.");
                await LoadSharedKeyAndQrCodeUriAsync(user);
                return View(new EnableTwoFactorViewModel
                {
                    SharedKey = ViewData["SharedKey"]?.ToString(),
                    AuthenticatorUri = ViewData["AuthenticatorUri"]?.ToString()
                });
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            TempData["SuccessMessage"] = "Two-factor authentication has been enabled!";

            // Generate recovery codes
            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            TempData["RecoveryCodes"] = recoveryCodes?.ToArray();

            return RedirectToAction(nameof(ShowRecoveryCodes));
        }

        // GET: Profile/TwoFactor/ShowRecoveryCodes
        [HttpGet]
        [Route("ShowRecoveryCodes")]
        public IActionResult ShowRecoveryCodes()
        {
            var recoveryCodes = TempData["RecoveryCodes"] as string[];
            if (recoveryCodes == null)
            {
                return RedirectToAction(nameof(Index));
            }

            return View(recoveryCodes);
        }

        // POST: Profile/TwoFactor/Disable
        [HttpPost]
        [Route("Disable")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            await _userManager.SetTwoFactorEnabledAsync(user, false);
            TempData["SuccessMessage"] = "Two-factor authentication has been disabled.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Profile/TwoFactor/ResetAuthenticator
        [HttpPost]
        [Route("ResetAuthenticator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAuthenticator()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _userManager.ResetAuthenticatorKeyAsync(user);
            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] = "Authenticator app key has been reset. You will need to reconfigure your authenticator app.";
            return RedirectToAction(nameof(EnableAuthenticator));
        }

        private async Task LoadSharedKeyAndQrCodeUriAsync(ApplicationUser user)
        {
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            ViewData["SharedKey"] = FormatKey(unformattedKey!);
            ViewData["AuthenticatorUri"] = GenerateQrCodeUri(user.Email!, unformattedKey!);
        }

        private string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
                currentPosition += 4;
            }
            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.AsSpan(currentPosition));
            }
            return result.ToString().ToLowerInvariant();
        }

        private string GenerateQrCodeUri(string email, string unformattedKey)
        {
            return string.Format(
                AuthenticatorUriFormat,
                _urlEncoder.Encode("Trendify - Ecommerce"),
                _urlEncoder.Encode(email),
                unformattedKey);
        }
    }
}
