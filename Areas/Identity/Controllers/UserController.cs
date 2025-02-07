// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using App.Areas.Identity.Models.UserViewModels;
using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace App.Areas.Identity.Controllers
{

    [Area("Identity")]
    [Route("/manage-user/[action]")]
    [Authorize(Policy = "CanManageUser")]
    public class UserController : Controller
    {
        private readonly ILogger<RoleController> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IDeleteUserService _deleteUser;
        private readonly IWebHostEnvironment _env;

        public UserController(
            ILogger<RoleController> logger,
            RoleManager<IdentityRole> roleManager,
            AppDbContext dbContext,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IDeleteUserService deleteUser,
            IWebHostEnvironment env
        )
        {
            _logger = logger;
            _roleManager = roleManager;
            _dbContext = dbContext;
            _userManager = userManager;
            _signInManager = signInManager;
            _deleteUser = deleteUser;
            _env = env;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public IActionResult GetStatusMessage()
        {
            return PartialView("_StatusMessage");
        }

        // GET: /manageUser
        [HttpGet("/manage-user")]
        public async Task<IActionResult> Index([FromQuery(Name = "p")] int currentPage, [Bind("SearchString")] UserListModel model)
        {
            model.currentPage = currentPage;

            var usersWithMemberships = _userManager.Users
                .GroupJoin(
                    _dbContext.Memberships,
                    u => u.Id,
                    m => m.UserId,
                    (user, memberships) => new { User = user, Memberships = memberships }
                )
                .SelectMany(
                    um => um.Memberships.DefaultIfEmpty(),
                    (um, membership) => new { um.User, Membership = membership }
                );

            var usersWithDetails = usersWithMemberships
                .GroupJoin(
                    _dbContext.MembershipDetails,
                    um => um.Membership.MembershipDetailsId,
                    md => md.Id,
                    (um, membershipDetails) => new { um.User, MembershipDetails = membershipDetails.FirstOrDefault() }
                )
                .AsQueryable();

            if (!string.IsNullOrEmpty(model.SearchString))
            {
                usersWithDetails = usersWithDetails.AsNoTracking()
                    .Where(x => x.User.Id.Contains(model.SearchString) ||
                                x.User.UserName.Contains(model.SearchString) ||
                                x.User.Email.Contains(model.SearchString) ||
                                x.User.PhoneNumber.Contains(model.SearchString) ||
                                (x.MembershipDetails != null && x.MembershipDetails.MembershipType.ToString().Contains(model.SearchString)));

                if (!await usersWithDetails.AnyAsync())
                {
                    model.MessageSearchResult = "Không tìm thấy tài khoản nào.";
                }
            }

            usersWithDetails = usersWithDetails.OrderBy(x => x.User.UserName);

            model.totalUsers = await usersWithDetails.CountAsync();
            model.countPages = (int)Math.Ceiling((double)model.totalUsers / model.ITEMS_PER_PAGE);

            if (model.currentPage < 1)
                model.currentPage = 1;
            if (model.currentPage > model.countPages)
                model.currentPage = model.countPages;

            var qrView = usersWithDetails
                .AsNoTracking()
                .Skip((model.currentPage - 1) * model.ITEMS_PER_PAGE)
                .Take(model.ITEMS_PER_PAGE)
                .Select(x => new UserViewModel()
                {
                    Id = x.User.Id,
                    UserName = x.User.UserName,
                    Email = x.User.Email,
                    PhoneNumber = x.User.PhoneNumber,
                    MembershipType = x.MembershipDetails != null ? x.MembershipDetails.MembershipType : MemberType.Free
                });

            model.users = await qrView.ToListAsync();

            return View(model);
        }

        //GET: /manageUser/ManageUser
        [HttpGet("/manage-user/{id}")]
        public async Task<IActionResult> ManageUserAsync(string id)
        {
            if (id == null)
            {
                return NotFound("Không tìm thấy thành viên.");
            }
            var user = await _userManager.Users.Where(u => u.Id == id)
                            .Include(u => u.EditedImages)
                            .FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound("Không tìm thấy thành viên.");
            }

            var membership = await _dbContext.Memberships.AsNoTracking()
                                                .Where(m => m.UserId == id)
                                                .Select(m => new { m.MembershipDetails.MembershipType, m.StartTime, m.EndTime })
                                                .FirstOrDefaultAsync();

            //Get user info
            var userInfo = new UserInfoModel()
            {
                Id = id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                Gender = user.Gender == Gender.Male ? "Nam"
                        : user.Gender == Gender.Female ? "Nữ"
                        : user.Gender == Gender.Unspecified ? "Không xác định"
                        : "",
                BirthDate = user.BirthDate,
                MembershipType = membership?.MembershipType == MemberType.Standard ? "Standard"
                                : membership?.MembershipType == MemberType.Premium ? "Premium"
                                : null,
                StartTime = membership?.StartTime ?? null,
                EndTime = membership?.EndTime ?? null,
                AccountLockEnd = user.LockoutEnd,
                ImgEditLockEnd = user.ImgEditLockEnd,
                AvatarPath = user.AvatarPath ?? "/images/no_avt.jpg"
            };

            //Get all MembershipType
            var allMembershipTypes = new List<string> { "Free", "Standard", "Premium" };

            //Get image info
            var images = user.EditedImages.Select(i => new ImageInfoModel()
            {
                Id = i.Id,
                ImageUrl = i.ImageUrl,
                ThumbUrl = i.ThumbUrl,
                ActionTaken = i.ActionTaken,
                EditedAt = i.EditedAt ?? default,
                ImageKBSize = i.ImageKBSize
            }).ToList();

            //Get role info
            var roles = from ur in _dbContext.UserRoles
                        join r in _dbContext.Roles on ur.RoleId equals r.Id
                        where ur.UserId == user.Id
                        select r;
            var userRoleNames = await roles.Select(r => r.Name).ToArrayAsync();
            var allRoleNames = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            // Get Claim
            var claims = await (from rc in _dbContext.RoleClaims
                                join r in roles on rc.RoleId equals r.Id
                                select new RoleClaimModel
                                {
                                    RoleName = r.Name,
                                    ClaimType = rc.ClaimType,
                                    ClaimValue = rc.ClaimValue
                                }).ToListAsync();

            var model = new ManageUserModel()
            {
                UserId = id,
                UserInfo = userInfo,
                UserRoleNames = userRoleNames,
                AllRoleNames = new SelectList(allRoleNames),
                AllMembershipTypes = new SelectList(allMembershipTypes),
                Images = images,
                Claims = claims
            };
            return View(model);
        }

        //POST: /manageUser/UpdateMembershipUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanUpdateMshipUser")]
        public async Task<IActionResult> UpdateMembershipUserAsync([Bind("UserId", "UserInfo")] ManageUserModel model)
        {
            if (string.IsNullOrEmpty(model.UserId))
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new { success = false });
            }
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                StatusMessage = "Error Không tìm thấy người dùng.";
                return Json(new { success = false });
            }

            var membershipType = model.UserInfo.MembershipType == "Standard" ? MemberType.Standard :
                                            model.UserInfo.MembershipType == "Premium" ? MemberType.Premium :
                                            MemberType.Free;
            var startTime = model.UserInfo.StartTime ?? null;
            var endTime = model.UserInfo.EndTime ?? null;

            var membership = await _dbContext.Memberships.Where(m => m.UserId == model.UserId).FirstOrDefaultAsync();
            if (membership == null)
            {
                if (membershipType == MemberType.Free)
                {
                    StatusMessage = "Đã cập nhật membership cho tài khoản.";
                    return Json(new { success = true });
                }
                membership = new MembershipsModel
                {
                    UserId = model.UserId,
                    MembershipDetailsId = _dbContext.MembershipDetails.Where(md => md.MembershipType == membershipType)
                                                                    .Select(md => md.Id).FirstOrDefault(),
                    StartTime = startTime,
                    EndTime = endTime,
                    User = user
                };
                await _dbContext.Memberships.AddAsync(membership);
            }
            else
            {
                if (membershipType == MemberType.Free)
                {
                    _dbContext.Memberships.Remove(membership);
                }
                else
                {
                    membership.MembershipDetailsId = _dbContext.MembershipDetails.Where(md => md.MembershipType == membershipType)
                                                                            .Select(md => md.Id).FirstOrDefault();
                    membership.StartTime = startTime;
                    membership.EndTime = endTime;
                }
            }
            await _dbContext.SaveChangesAsync();

            var userInfo = new UserInfoModel
            {
                MembershipType = model.UserInfo.MembershipType,
                StartTime = startTime,
                EndTime = endTime
            };
            StatusMessage = "Đã cập nhật membership cho tài khoản.";
            return PartialView("_MembershipInfo", userInfo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImageAsync(int imageId, string userId)
        {
            var image = await _dbContext.EditedImages
                                .Where(i => i.Id == imageId)
                                .FirstOrDefaultAsync();
            if (image == null)
            {
                StatusMessage = "Không tìm thấy ảnh.";
                return Json(new { success = false });
            }

            var imagePath = Path.Combine(_env.ContentRootPath, image.ImagePath);
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
            var thumbPath = imagePath.Insert(imagePath.LastIndexOf('/') + 1, "Thumbnails/");
            if (System.IO.File.Exists(thumbPath))
            {
                System.IO.File.Delete(thumbPath);
            }

            _dbContext.EditedImages.Remove(image);
            await _dbContext.SaveChangesAsync();

            var images = await _dbContext.EditedImages.AsNoTracking()
                                            .Where(i => i.UserId == userId)
                                            .Select(i => new ImageInfoModel()
                                            {
                                                Id = i.Id,
                                                ImageUrl = i.ImageUrl,
                                                ThumbUrl = i.ThumbUrl,
                                                ActionTaken = i.ActionTaken,
                                                EditedAt = i.EditedAt ?? default,
                                                ImageKBSize = i.ImageKBSize
                                            }).ToListAsync();
            ViewBag.UserId = userId;
            StatusMessage = $"Đã xoá ảnh có id: {imageId}";
            return PartialView("_ImageInfo", images);
        }

        //POST: /manageUser/UpdateRoleUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanManageRole")]
        public async Task<IActionResult> UpdateRoleUserAsync([Bind("UserId", "UserRoleNames")] ManageUserModel model)
        {
            if (string.IsNullOrEmpty(model.UserId))
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new { success = false });
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                StatusMessage = "Error Không tìm thấy người dùng.";
                return Json(new { success = false });
            }

            //Remove old role
            var currentRoles = await _userManager.GetRolesAsync(user);
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                StatusMessage = "Error Xóa vai trò hiện tại không thành công.";
                return Json(new { success = false });
            }

            // Add Role
            if (model.UserRoleNames != null)
            {
                var addResult = await _userManager.AddToRolesAsync(user, model.UserRoleNames);
                if (!addResult.Succeeded)
                {
                    StatusMessage = "Error Thêm vai trò mới không thành công.";
                    return Json(new { success = false });
                }
            }
            await _userManager.RemoveAuthenticationTokenAsync(user, "Default", "AccessToken");
            await _userManager.UpdateSecurityStampAsync(user);
            // await _signInManager.RefreshSignInAsync(user);

            var rolesView = from ur in _dbContext.UserRoles
                            join r in _dbContext.Roles on ur.RoleId equals r.Id
                            where ur.UserId == model.UserId
                            select r;
            List<RoleClaimModel> claims = await (from rc in _dbContext.RoleClaims
                                                 join r in rolesView on rc.RoleId equals r.Id
                                                 select new RoleClaimModel
                                                 {
                                                     RoleName = r.Name,
                                                     ClaimType = rc.ClaimType,
                                                     ClaimValue = rc.ClaimValue
                                                 }).ToListAsync();

            StatusMessage = "Đã cập nhật role cho tài khoản.";
            return PartialView("_RoleClaimUserTable", claims);
        }

        //POST: /manageUser/LockAccountOptions
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanLockUser")]
        public async Task<IActionResult> LockAccountOptionsAsync([Bind("UserId, UserInfo")] ManageUserModel model)
        {
            if (string.IsNullOrEmpty(model.UserId))
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new { success = false });
            }
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new { success = false });
            }

            user.LockoutEnd = model.UserInfo.AccountLockEnd ?? null;
            user.ImgEditLockEnd = model.UserInfo.ImgEditLockEnd ?? null;

            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var timeInVietnam = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);

            if (model.UserInfo.AccountLockEnd != null && model.UserInfo.AccountLockEnd > new DateTimeOffset(timeInVietnam))
            {
                user.AccessFailedCount = 0;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                StatusMessage = "Error Cập nhật thông tin khoá tài khoản thất bại với lỗi:";
                foreach (var error in result.Errors)
                {
                    StatusMessage += $"<br/>{error.Description}";
                }
                return Json(new { success = false });
            }

            if (user.LockoutEnabled && user.LockoutEnd > DateTime.UtcNow)
            {
                await _userManager.UpdateSecurityStampAsync(user);
            }

            UserInfoModel userInfo = new UserInfoModel()
            {
                AccountLockEnd = user.LockoutEnd,
                ImgEditLockEnd = user.ImgEditLockEnd,
            };

            StatusMessage = "Đã cập nhật thông tin khoá tài khoản";
            return PartialView("_LockInfo", userInfo);
        }

        //POST: /manageUser/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize("CanResetPassword")]
        public async Task<IActionResult> ResetPasswordAsync(string userId)
        {
            _logger.LogError(string.Empty, userId);
            if (string.IsNullOrEmpty(userId))
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new { success = false });
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new { success = false });
            }

            await _userManager.RemovePasswordAsync(user);
            var result = await _userManager.AddPasswordAsync(user, userId);
            if (!result.Succeeded)
            {
                StatusMessage = "Error Đặt lại mật khẩu thất bại.";
                return RedirectToAction(nameof(Index));
            }

            StatusMessage = "Đã đặt lại mật khẩu.";
            return Json(new { success = true });
        }

        //POST: /manageUser/DeleteAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize("CanDeleteUser")]
        public async Task<IActionResult> DeleteAccountAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new { success = false });
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new { success = false });
            }

            var result = await _deleteUser.DeleteUserAsync(user.Id);
            if (!result)
            {
                StatusMessage = "Error Xoá tài khoản thất bại.";
                return Json(new { success = false });
            }

            StatusMessage = "Đã xoá tài khoản.";
            return Json(new { success = true, redirect = Url.Action("Index") });
        }
    }
}
