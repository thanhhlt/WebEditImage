#nullable disable

using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using App.Areas.Identity.Models.OptionViewModels;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace App.Areas.Identity.Controllers
{
    [Authorize]
    [Area("Identity")]
    [Route("/account/settings/[action]")]
    public class OptionController : Controller
    {
        private readonly ILogger<ProfileController> _logger;
        private readonly AppDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IDeleteUserService _deleteUser;

        public OptionController(
            ILogger<ProfileController> logger,
            AppDbContext dbContext,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IDeleteUserService deleteUser)
        {
            _logger = logger;
            _dbContext = dbContext;
            _userManager = userManager;
            _signInManager = signInManager;
            _deleteUser = deleteUser;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public IActionResult GetStatusMessage()
        {
            return PartialView("_StatusMessage");
        }

        //GET: /account/settings
        [HttpGet("/account/settings")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return NotFound("Không tìm thấy tài khoản.");
            }

            var currentLogins = await _userManager.GetLoginsAsync(user);
            var otherLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync())
                            .Where(auth => currentLogins.All(ul => auth.Name != ul.LoginProvider)).ToList();

            IndexOptionViewModel indexViewModel = new IndexOptionViewModel()
            {
                CurrentLogins = currentLogins,
                OtherLogins = otherLogins,
                TwoFactor = await _userManager.GetTwoFactorEnabledAsync(user),
                LoggedBrowsers = await _dbContext.LoggedBrowsers.Where(l => l.UserId == user.Id)
                                                                .OrderByDescending(l => l.LoginTime)
                                                                .Take(5).ToListAsync(),
            };

            return View(indexViewModel);
        }

        //POST: /option/RemoveExternalLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveExternalLoginAsync(IndexOptionViewModel model)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return NotFound("Không tìm thấy tài khoản.");
            }

            var loginProvider = model.RemoveLoginViewModel.LoginProvider;
            var providerKey = model.RemoveLoginViewModel.ProviderKey;
            var result = await _userManager.RemoveLoginAsync(user, loginProvider, providerKey);
            if (!result.Succeeded)
            {
                StatusMessage = $"Error Gỡ bỏ liên kết với {loginProvider} thất bại.";
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage =  $"Đã gỡ bỏ liên kết với {loginProvider}.";

            return RedirectToAction(nameof(Index));
        }

        //POST: /option//LinkExternalLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkExternalLoginAsync(string provider)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Request a redirect to the external login provider to link a login for the current user
            var redirectUrl = Url.Action("LinkExternalLoginCallback");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, _userManager.GetUserId(User));
            return new ChallengeResult(provider, properties);
        }

        //GET: /option/LinkExternalLoginCallback
        [HttpGet]
        public async Task<IActionResult> LinkExternalLoginCallbackAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound(" Error Không tìm thấy tài khoản.");
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var info = await _signInManager.GetExternalLoginInfoAsync(userId);
            if (info == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (info.LoginProvider == "Google" && await _userManager.GetEmailAsync(user) != email)
            {
                StatusMessage = "Error Tài khoản Google liên kết phải có Email trùng với Email đã đăng ký tài khoản trên Website.";
                return RedirectToAction(nameof(Index));
            }

            var providerDisplayName = info.ProviderDisplayName;
            if (info == null)
            {
                StatusMessage = $"Error Lỗi liên kết với {providerDisplayName}";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.AddLoginAsync(user, info);
            if (!result.Succeeded)
            {
                StatusMessage = $"Error Liên kết tài khoản với {providerDisplayName} có lỗi:";
                foreach (var error in result.Errors)
                {  
                    StatusMessage += $"<br/>{error.Description}";
                }
                return RedirectToAction(nameof(Index));
            }

            if (providerDisplayName  == "Google")
            {
                // Xác thực email tự động
                var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var resultConfirmEmail = await _userManager.ConfirmEmailAsync(user, emailConfirmationToken);

                if (resultConfirmEmail.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Member");
                    await _userManager.UpdateSecurityStampAsync(user);
                    await _signInManager.RefreshSignInAsync(user);
                }
            }

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            StatusMessage = $"Đã liên kết tài khoản với {providerDisplayName}.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /option/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanUseTwoFactorAuth")]
        public async Task<IActionResult> EnableTwoFactorAuthentication()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound(" Error Không tìm thấy tài khoản.");
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction(nameof(Index));
        }

        // POST: /option/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanUseTwoFactorAuth")]
        public async Task<IActionResult> DisableTwoFactorAuthentication()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound(" Error Không tìm thấy tài khoản.");
            }

            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction(nameof(Index));
        }

        //POST: /option/LogoutAll
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogoutAllAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound(" Error Không tìm thấy tài khoản.");
            }

            await _userManager.UpdateSecurityStampAsync(user);
            HttpContext.Response.Cookies.Delete(".AspNetCore.Identity.TwoFactorRememberMe");
            foreach (var cookie in HttpContext.Request.Cookies.Keys)
            {
                HttpContext.Response.Cookies.Delete(cookie);
            }
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        //POST: /option/DeleteAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(IndexOptionViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound(" Error Không tìm thấy tài khoản.");
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.DeleteAccountViewmodel.Password);
            if (!isPasswordValid)
            {
                StatusMessage = "Error Sai mật khẩu xác nhận.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _deleteUser.DeleteUserAsync(user.Id);
            if (!result)
            {
                StatusMessage = "Error Xoá tài khoản thất bại.";
                return RedirectToAction(nameof(Index));
            }

            await _signInManager.SignOutAsync();
            return Redirect("~/");
        }
    }
}