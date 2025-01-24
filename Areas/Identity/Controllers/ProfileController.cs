#nullable disable

using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using App.Areas.Identity.Models.ProfileViewModels;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace App.Areas.Identity.Controllers
{
    [Authorize]
    [Area("Identity")]
    [Route("/account/edit-profile/[action]")]
    public class ProfileController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            AppDbContext dbContext,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<AppUser> signInManager,
            IEmailSender emailSender,
            IEmailTemplateService emailTemplateService,
            IWebHostEnvironment environment,
            ILogger<ProfileController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _emailTemplateService = emailTemplateService;
            _environment = environment;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public IActionResult GetStatusMessage()
        {
            return PartialView("_StatusMessage");
        }

        // GET: /account/edit-profile
        [HttpGet("/account/edit-profile")]
        public async Task<IActionResult> Index()
        {
           var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return NotFound("Không tìm thấy tài khoản.");
            }

           IndexProfileViewModel model = new IndexProfileViewModel()
           {
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                BirthDate = user.BirthDate,
                Gender = user.Gender,
                EmailConfirmed = user.EmailConfirmed,
                FilePath = user.AvatarPath ?? "/images/no_avt.jpg"
           };

           return View(model);
        }

        // POST: /account/edit-profile/ChangeProfileInfo
        [HttpPost]
        public async Task<IActionResult> ChangeProfileInfo(IndexProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            if (user == null)
            {
                StatusMessage = "Error Không tìm thấy tài khaoản.";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                user.PhoneNumber = model.PhoneNumber;
                user.BirthDate = model.BirthDate;
                user.Gender = model.Gender;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    StatusMessage = "Cập nhật thông tin cá nhân thành công.";
                    return RedirectToAction("Index");
                }
            }
            var errorMessages = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            StatusMessage = $"Error Cập nhật thông tin cá nhân thất bại.<br/> {string.Join("<br/>", errorMessages)}";
            _logger.LogWarning(StatusMessage);
            return RedirectToAction("Index");
        }

        //POST: /account/edit-profile/UpdateAvatar
        [HttpPost]
        public async Task<IActionResult> UpdateAvatarAsync([Bind("ImageAvatar")]IndexProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return NotFound("Không tìm thấy tài khoản.");
            }
            if (model.ImageAvatar != null && model.ImageAvatar.Length > 0)
            {
                var extensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                var fileExtension = Path.GetExtension(model.ImageAvatar.FileName).ToLower();

                if (!extensions.Contains(fileExtension))
                {
                    StatusMessage = "Error Chỉ cho phép ảnh .jpg, .jpeg, .png, .webp, .gif";
                    return RedirectToAction(nameof(Index));
                }

                string directoryPath = Path.Combine(_environment.ContentRootPath, "Images/Avatar");
                var filePath = Path.Combine(directoryPath, user.Id + fileExtension);

                var existingFiles = Directory.GetFiles(directoryPath)
                                            .Where(f => Path.GetFileNameWithoutExtension(f) == user.Id.ToString())
                                            .ToList();
                foreach (var file in existingFiles)
                {
                    System.IO.File.Delete(file);
                }

                using (var image = await Image.LoadAsync(model.ImageAvatar.OpenReadStream()))
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Crop,
                        Size = new Size(150, 150)
                    }));

                    await image.SaveAsync(filePath);
                }

                filePath = "/imgs/Avatar/" + user.Id + fileExtension;
                user.AvatarPath = filePath;
                await _dbContext.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            StatusMessage = "Error Không có ảnh.";
            return RedirectToAction(nameof(Index));
        }

        //GET: /account/ResendEmailConfirm
        [HttpGet("/account/ResendEmailConfirm")]
        public async Task<IActionResult> ResendEmailConfirm()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            if (user == null)
            {
                StatusMessage = "Error Không tìm thấy tài khaoản.";
                return RedirectToAction("Index");
            }

            try {
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var callbackUrl = Url.ActionLink(
                    action: nameof(ConfirmEmail),
                    values: 
                        new { area = "Identity",
                            controller = "Account",
                            userId = user.Id, 
                            code = code},
                    protocol: Request.Scheme);

                if (callbackUrl != null)
                {
                    var emailBodyTemplate = await _emailTemplateService.GetTemplateAsync("ConfirmEmail.html");
                    var emailBody = emailBodyTemplate.Replace("{{ConfirmationLink}}", HtmlEncoder.Default.Encode(callbackUrl))
                                                    .Replace("{{UserName}}", user.UserName);

                    await _emailSender.SendEmailAsync(user.Email, "Xác nhận địa chỉ email - PerfectPix", emailBody);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                return Json(new{success = false, message = "Lỗi gửi email xác nhận."});
            }
            
            return Json(new{success = true, message = "Đã gửi email xác nhận."});
        }
        
        //GET: /account/edit-profile/ChangeEmail
        [HttpGet]
        public async Task<IActionResult> ChangeEmail(string NewEmail)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            ChangeEmailViewModel changeEmailViewModel = new ChangeEmailViewModel()
            {
                OldEmail = await _userManager.GetEmailAsync(user),
            };

            return View(changeEmailViewModel);
        }

        //POST: /account/edit-profile/ChangeEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmailAsync(ChangeEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                StatusMessage = $"Error Thay đổi Email thất bại.<br/> {string.Join("<br/>", errorMessages)}";
                return Json(new {success = false});
            }

            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                StatusMessage = "Error Không tìm thấy tài khoản";
                return Json(new {success = false});
            }

            var logins = await _userManager.GetLoginsAsync(user);
            foreach(var login in logins)
            {
                if (login.ProviderDisplayName == "Google")
                {
                    StatusMessage = "Error Gỡ bỏ liên kết tài khoản với Google trước khi thay đổi Email";
                    return Json(new { success = false, redirect = Url.Action("Index", "Option") });
                }
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordValid)
            {
                StatusMessage = "Error Sai mật khẩu xác nhận.";
                return Json(new {success = false});
            }
            
            var oldEmail = await _userManager.GetEmailAsync(user);
            if (model.NewEmail == oldEmail)
            {
                StatusMessage = "Error Email mới trùng với Email hiện tại.";
                return Json(new {success = false});
            }

            if (await _userManager.FindByEmailAsync(model.NewEmail) != null)
            {
                StatusMessage = "Error Đã tồn tại tài khoản có Email này.";
                return Json(new {success = false});
            }

            user.Email = model.NewEmail;
            user.EmailConfirmed = false;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                StatusMessage = "Error Thay đổi Email thất bại với lỗi:";
                foreach (var error in result.Errors)
                {  
                    StatusMessage += $"<br/>{error.Description}";
                }
                return Json(new { success = false, redirect = Url.Action("Index") });   
            }

            //Remove Role
            var roleExist = await _roleManager.RoleExistsAsync("Member");
            if (roleExist)
            {
                await _userManager.RemoveFromRoleAsync(user, "Member");
            }

            // Gửi qua email mới email xác nhận
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.ActionLink(
                action: nameof(ConfirmEmail),
                values: 
                    new { area = "Identity",
                        controller = "Account",
                        userId = user.Id, 
                        code = code},
                protocol: Request.Scheme);

            if (callbackUrl != null)
            {
                var emailBodyTemplate = await _emailTemplateService.GetTemplateAsync("ConfirmEmail.html");
                var emailBody = emailBodyTemplate.Replace("{{ConfirmationLink}}", HtmlEncoder.Default.Encode(callbackUrl))
                                                .Replace("{{UserName}}", user.UserName);

                await _emailSender.SendEmailAsync(model.NewEmail, "Xác nhận địa chỉ email - PerfectPix", emailBody);
            }

            await _userManager.UpdateSecurityStampAsync(user);
            StatusMessage = "Thay đổi Email thành công. Kiểm tra Email mới để xác thực.";
            await _signInManager.RefreshSignInAsync(user);

            return Json(new { success = true, redirect = Url.Action("Index") });
        }

        //GET: /account/edit-profile/ChangePassword
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        //POST: /account/edit-profile/ChangePassword
        [HttpPost]
        public async Task<IActionResult> ChangePasswordAsync(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                StatusMessage = $"Error Thay đổi mật khẩu thất bại.<br/> {string.Join("<br/>", errorMessages)}";
                return Json(new {success = false});
            }

            if (model.OldPassword == model.NewPassword)
            {
                StatusMessage = "Error Mật khẩu mới phải khác mật khẩu hiện tại.";
                return Json(new {success = false});
            }

            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                StatusMessage = "Error Không tìm thấy tài khoản.";
                return Json(new {success = false});
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                StatusMessage = "Error Thay đổi mật khẩu thất bại với lỗi:";
                foreach (var error in result.Errors)
                {  
                    StatusMessage += $"<br/>{error.Description}";
                }
                return Json(new {success = false});
            }
            await _userManager.UpdateSecurityStampAsync(user);
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Thay đổi mật khẩu thành công.";

            return Json(new {success = true, redirect = Url.Action("Index")});
        }
    }
}