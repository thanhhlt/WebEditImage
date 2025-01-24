// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using App.Areas.Identity.Models.AccountViewModels;
using App.ExtendMethods;
using App.Models;
using App.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using UAParser;

namespace App.Areas.Identity.Controllers
{
    [Authorize]
    [Area("Identity")]
    [Route("/account/[action]")]
    public class AccountController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserStore<AppUser> _userStore;
        private readonly IUserEmailStore<AppUser> _emailStore;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            AppDbContext dbContext,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUserStore<AppUser> userStore,
            SignInManager<AppUser> signInManager,
            IEmailSender emailSender,
            IEmailTemplateService emailTemplateService,
            ILogger<AccountController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _roleManager = roleManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _emailSender = emailSender;
            _emailTemplateService = emailTemplateService;
            _logger = logger;
        }

        // GET: /login
        [HttpGet("/login/")]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /login
        [HttpPost("/login/")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
        {
            returnUrl ??= Url.Content("~/");
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.UserNameOrEmail, model.Password, model.RememberMe, lockoutOnFailure: true);                

                if ((!result.Succeeded) && ValidationEmail.IsValidEmail(model.UserNameOrEmail))
                {
                    var user = await _userManager.FindByEmailAsync(model.UserNameOrEmail);
                    if (user != null && user.UserName != null)
                    {
                        result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);
                    }
                } 

                if (result.Succeeded)
                {
                    _logger.LogInformation(1, "User logged in.");

                    //Save Browser Login Info
                    var user = await _userManager.FindByNameAsync(model.UserNameOrEmail);
                    if (user == null && ValidationEmail.IsValidEmail(model.UserNameOrEmail))
                    {
                        user = await _userManager.FindByEmailAsync(model.UserNameOrEmail);
                    }
                    if (user != null)
                    {
                        await SaveBrowserInfo(user.Id);
                    }

                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                   return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                }
                
                if (result.IsLockedOut)
                {
                    _logger.LogWarning(2, "Tài khoản bị khóa");
                    return View("Lockout");
                }
                else
                {
                    ModelState.AddModelError("Sai tài khoản hoặc mật khẩu.");
                    return View(model);
                }
            }
            return View(model);
        }

        // POST: /logout
        [HttpGet("/logout/")]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User đăng xuất");
            return RedirectToAction("Index", "Home", new {area = ""});
        }
        
        // GET: /account/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl)
        {
            returnUrl ??= Url.Content("~/");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
        
        // POST: /account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl)
        {
            returnUrl ??= Url.Content("~/");
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = new AppUser { 
                    UserName = model.UserName, 
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber
                    };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Đã tạo user mới.");
                    await _userManager.AddToRoleAsync(user, "Guest");

                    // Phát sinh token để xác nhận email
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    var callbackUrl = Url.ActionLink(
                        action: nameof(ConfirmEmail),
                        values: 
                            new { area = "Identity", 
                                  userId = user.Id, 
                                  code = code},
                        protocol: Request.Scheme);

                    if (callbackUrl != null)
                    {
                        var emailBodyTemplate = await _emailTemplateService.GetTemplateAsync("ConfirmEmail.html");
                        var emailBody = emailBodyTemplate.Replace("{{ConfirmationLink}}", HtmlEncoder.Default.Encode(callbackUrl))
                                                        .Replace("{{UserName}}", model.UserName);

                        await _emailSender.SendEmailAsync(model.Email, "Xác nhận địa chỉ email - PerfectPix", emailBody);
                    }

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return LocalRedirect(Url.Action(nameof(RegisterConfirmation)) ?? Url.Content("~/"));
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        await SaveBrowserInfo(user.Id);
                        return LocalRedirect(returnUrl);
                    }

                }

                ModelState.AddModelError(result);
            }
            return View(model);
        }
        
        // GET: /account/ConfirmEmail
        [HttpGet]
        [AllowAnonymous]
        public IActionResult RegisterConfirmation()
        {
            return View();
        }       

        // GET: /account/ConfirmEmail
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("ErrorConfirmEmail");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return View("ErrorConfirmEmail");
            }
            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Member");
                await _userManager.UpdateSecurityStampAsync(user);
                await _signInManager.RefreshSignInAsync(user);
            }
            return View(result.Succeeded ? "ConfirmEmail" : "ErrorConfirmEmail");
        }

        // POST: /account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl)
        {
            returnUrl ??= Url.Content("~/");
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        //GET: /externalloginfail
        [HttpGet("/externalloginfail")]
        public IActionResult ExternalLoginFail(string? returnUrl)
        {
            returnUrl ??= Url.Content("~/");
            return LocalRedirect(returnUrl);
        }

        // GET: /account/ExternalLoginCallback
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl, string? remoteError)
        {
            returnUrl ??= Url.Content("~/");
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi sử dụng dịch vụ ngoài: {remoteError}");
                return View(nameof(Login));
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (result.Succeeded)
            {
                await _signInManager.UpdateExternalAuthenticationTokensAsync(info);
                var userId = _userManager.GetUserId(User);
                if (!string.IsNullOrEmpty(userId))
                    await SaveBrowserInfo(userId);
                _logger.LogInformation(5, "User logged in with {Name} provider.", info.LoginProvider);
                return LocalRedirect(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl });
            }
            if (result.IsLockedOut)
            {
                return View("Lockout");
            }
            else
            {
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["ProviderDisplayName"] = info.ProviderDisplayName;
                
                var loginProvider = info.LoginProvider;
                var providerKey = info.ProviderKey;

                var existingLogin = await _userManager.FindByLoginAsync(loginProvider, providerKey);

                if (existingLogin != null)
                {
                    ModelState.AddModelError(string.Empty, $"Lỗi sử dụng dịch vụ ngoài: {remoteError}");
                    return View(nameof(Login));
                }

                // If the user does not have an account, then ask the user to create an account.
                else
                {
                    if (info.ProviderDisplayName == "Google")
                    {
                        if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
                        {
                            var Email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? "";
                            var existingUser = await _userManager.FindByEmailAsync(Email);

                            if (existingUser != null)
                            {
                                var linkResult = await _userManager.AddLoginAsync(existingUser, info);

                                if (linkResult.Succeeded)
                                {
                                    if (!existingUser.EmailConfirmed)
                                    {
                                        // Xác thực email tự động
                                        var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(existingUser);
                                        var resultConfirmEmail = await _userManager.ConfirmEmailAsync(existingUser, emailConfirmationToken);
                                        if (resultConfirmEmail.Succeeded)
                                        {
                                            await _userManager.AddToRoleAsync(existingUser, "Member");
                                            await _userManager.UpdateSecurityStampAsync(existingUser);
                                        }
                                    }
                                    await _signInManager.SignInAsync(existingUser, isPersistent: false);
                                    await SaveBrowserInfo(existingUser.Id);
                                    return LocalRedirect(returnUrl);
                                }
                                else
                                {
                                    ModelState.AddModelError(linkResult);
                                    return View(nameof(Login));
                                }
                            }
                            else
                            {
                                ViewData["Email"] = Email;
                                return View("RegisterWithExternalLogin");
                            }
                        }
                        ModelState.AddModelError(string.Empty, $"Lỗi sử dụng dịch vụ ngoài: {remoteError}");
                        return View(nameof(Login));
                    }

                    else if (info.ProviderDisplayName == "Facebook")
                    {
                        return View("RegisterWithExternalLogin");
                    }
                }
            }
            return View(nameof(Login));
        }

        // POST: /account/RegisterWithExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterWithExternalLogin(RegisterViewModel model, string returnUrl)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            // Get the information about the user from the external login provider
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ModelState.AddModelError("Lỗi lấy thông tin từ dịch vụ đăng nhập bên ngoài.");
                return View("ExternalLoginFailure");
            }
            if (info.ProviderDisplayName == "Google")
            {
                model.Email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? "";
                ModelState.Remove("Email");
            }

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, model.UserName, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, model.Email, CancellationToken.None);
                user.PhoneNumber = model.PhoneNumber;

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    await _userManager.AddToRoleAsync(user, "Guest");
                    if (result.Succeeded)
                    {
                        if (info.ProviderDisplayName == "Google")
                        {
                            // Xác thực email tự động
                            var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                            var resultConfirmEmail = await _userManager.ConfirmEmailAsync(user, emailConfirmationToken);

                            if (resultConfirmEmail.Succeeded)
                            {
                                await _userManager.AddToRoleAsync(user, "Member");
                                await _userManager.UpdateSecurityStampAsync(user);
                            }

                            _logger.LogInformation(6, "User created an account using {Name} provider.", info.LoginProvider);

                            await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                            await SaveBrowserInfo(user.Id);
                            return LocalRedirect(returnUrl);                        
                        }
                        else if (info.ProviderDisplayName == "Facebook")
                        {
                            // Phát sinh token để xác nhận email
                            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                            var callbackUrl = Url.ActionLink(
                                action: nameof(ConfirmEmail),
                                values: 
                                    new { area = "Identity", 
                                        userId = user.Id, 
                                        code = code},
                                protocol: Request.Scheme);

                            if (callbackUrl != null)
                            {
                                var emailBodyTemplate = await _emailTemplateService.GetTemplateAsync("ConfirmEmail.html");
                                var emailBody = emailBodyTemplate.Replace("{{ConfirmLink}}", HtmlEncoder.Default.Encode(callbackUrl));

                                await _emailSender.SendEmailAsync(model.Email, "Xác nhận địa chỉ email - PerfectPix", emailBody);
                            }

                            if (_userManager.Options.SignIn.RequireConfirmedAccount)
                            {
                                return LocalRedirect(Url.Action(nameof(RegisterConfirmation)) ?? Url.Content("~/"));
                            }
                            else
                            {
                                await _signInManager.SignInAsync(user, isPersistent: false);
                                await SaveBrowserInfo(user.Id);
                                return LocalRedirect(returnUrl);
                            }
                        }
                    }
                }
                ModelState.AddModelError(result);
            }
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["ProviderDisplayName"] = info.ProviderDisplayName;
            return View(model);
        }
        private AppUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<AppUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(AppUser)}'. " +
                    $"Ensure that '{nameof(AppUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the external login page in /Areas/Identity/Pages/Account/ExternalLogin.cshtml");
            }
        }
        private IUserEmailStore<AppUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<AppUser>)_userStore;
        }

        // GET: /account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.ActionLink(
                    action: nameof(ResetPassword),
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                if (callbackUrl != null)
                {
                    var emailBodyTemplate = await _emailTemplateService.GetTemplateAsync("ResetPassword.html");
                    var emailBody = emailBodyTemplate.Replace("{{UserName}}", user.UserName)
                                                    .Replace("{{ResetPasswordLink}}", HtmlEncoder.Default.Encode(callbackUrl));

                    await _emailSender.SendEmailAsync(model.Email, "Đặt lại mật khẩu.", emailBody);
                }

                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }
            return View(model);
        }

        // GET: /account/ForgotPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // GET: /account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        // POST: /account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
            }
            var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));

            var result = await _userManager.ResetPasswordAsync(user, code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
            }
            ModelState.AddModelError(result);
            return View();
        }

        // GET: /account/ResetPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }        

        // POST: /account/SendCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user != null)
            {
                // Generate the token and send it
                var code = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
                if (string.IsNullOrWhiteSpace(code))
                {
                    return View("Error");
                }

                var emailBodyTemplate = await _emailTemplateService.GetTemplateAsync("TwoFactorAuthentication.html");
                var emailBody = emailBodyTemplate.Replace("{{UserName}}", user.UserName)
                                                .Replace("{{TwoFactorCode}}", code);
                var to_email = await _userManager.GetEmailAsync(user);
                if (to_email != null)
                {
                    await _emailSender.SendEmailAsync(to_email, "Security Code", emailBody);
                }
            }

            return RedirectToAction(nameof(VerifyCode), new {ReturnUrl = returnUrl, RememberMe = rememberMe});
        }

        // GET: /account/VerifyCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyCode(string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        // POST: /account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            model.ReturnUrl ??= Url.Content("~/");
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes.
            // If a user enters incorrect codes for a specified amount of time then the user account
            // will be locked out for a specified amount of time.
            var result = await _signInManager.TwoFactorSignInAsync("Email", model.Code, model.RememberMe, model.RememberBrowser);
            if (result.Succeeded)
            {
                var userId = _userManager.GetUserId(User);
                if (!string.IsNullOrEmpty(userId))
                    await SaveBrowserInfo(userId);
                return LocalRedirect(model.ReturnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning(7, "User account locked out.");
                return View("Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid code.");
                return View(model);
            }
        }

        [Route("/access-denied")]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task SaveBrowserInfo (string userId)
        {
            var userAgent = Request.Headers["User-Agent"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                ipAddress = forwardedFor.Split(',')[0].Trim();
            }

            var parser = Parser.GetDefault();
            var client = parser.Parse(userAgent);

            var browserInfo = $"{client.UA.Family} {client.UA.Major}, OS: {client.OS.Family} {client.OS.Major}";

            if (_dbContext.LoggedBrowsers != null)
            {
                var existingBrowser = _dbContext.LoggedBrowsers.Where(b => b.UserId == userId && 
                                                                        b.BrowserInfo == browserInfo && 
                                                                        b.IpAddress == ipAddress).FirstOrDefault();

                var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var loginTimeInVietnam = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);

                if (existingBrowser != null)
                {
                    existingBrowser.LoginTime = new DateTimeOffset(loginTimeInVietnam);
                    _dbContext.LoggedBrowsers.Update(existingBrowser);
                }
                else
                {
                    var newBrowser = new LoggedBrowsersModel
                    {
                        UserId = userId,
                        BrowserInfo = browserInfo,
                        IpAddress = ipAddress,
                        LoginTime = new DateTimeOffset(loginTimeInVietnam)
                    };
                    _dbContext.LoggedBrowsers.Add(newBrowser);
                }

                await _dbContext.SaveChangesAsync();
            }
        }    
    }
}