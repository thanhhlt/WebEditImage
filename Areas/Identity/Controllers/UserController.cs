// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Globalization;
using App.Areas.Identity.Models.UserViewModels;
using App.Models;
using K4os.Compression.LZ4.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace App.Areas.Identity.Controllers
{

    // [Authorize(Roles = RoleName.Administrator)]
    [Area("Identity")]
    [Route("/manage-user/[action]")]
    public class UserController : Controller
    {
        private readonly ILogger<RoleController> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IDeleteUserService _deleteUser;

        public UserController(
            ILogger<RoleController> logger, 
            RoleManager<IdentityRole> roleManager, 
            AppDbContext dbContext, 
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IDeleteUserService deleteUser)
        {
            _logger = logger;
            _roleManager = roleManager;
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

        // GET: /manageUser
        [HttpGet("/manage-user")]
        public async Task<IActionResult> Index([FromQuery(Name = "p")] int currentPage, [Bind("SearchString")] UserListModel model)
        {
            model.currentPage = currentPage;

            var qr = _userManager.Users
                        .GroupJoin(
                            _dbContext.Memberships, 
                            u => u.Id, 
                            m => m.UserId, 
                            (user, memberships) => new { User = user, Membership = memberships.FirstOrDefault() }
                        )
                        .AsQueryable();
            if (!string.IsNullOrEmpty(model.SearchString))
            {
                var qrSearch = qr;
                qrSearch = qrSearch.AsNoTracking()
                                    .Where(x => x.User.Id.Contains(model.SearchString) ||
                                                x.User.UserName.Contains(model.SearchString) ||
                                                x.User.Email.Contains(model.SearchString) || 
                                                x.User.PhoneNumber.Contains(model.SearchString) ||
                                                x.Membership.MembershipType.ToString().Contains(model.SearchString));
                if (!qrSearch.Any())
                {
                    model.MessageSearchResult = "Không tìm thấy tài khoản nào.";
                }
                else
                {
                    qr = qrSearch;
                }
            }
            qr = qr.OrderBy(x => x.User.UserName);

            model.totalUsers = await qr.CountAsync();
            model.countPages = (int)Math.Ceiling((double)model.totalUsers / model.ITEMS_PER_PAGE);

            if (model.currentPage < 1)
                model.currentPage = 1;
            if (model.currentPage > model.countPages)
                model.currentPage = model.countPages;

            var qrView = qr.AsNoTracking()
                            .Skip((model.currentPage - 1) * model.ITEMS_PER_PAGE)
                            .Take(model.ITEMS_PER_PAGE)
                            .Select(x => new UserViewModel()
                            {
                                Id = x.User.Id,
                                UserName = x.User.UserName,
                                Email = x.User.Email,
                                PhoneNumber = x.User.PhoneNumber,
                                MembershipType = x.Membership != null ? x.Membership.MembershipType : Membership.Standard
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
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("Không tìm thấy thành viên.");
            }

            var membership = await _dbContext.Memberships.AsNoTracking()
                                                .Where(m => m.UserId == id)
                                                .Select(m => new {m.MembershipType, m.StartTime, m.EndTime})
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
                MembershipType = membership?.MembershipType == Membership.Standard ? "Standard"
                                : membership?.MembershipType == Membership.Premium ? "Premium"
                                : membership?.MembershipType == Membership.Professional ? "Professional"
                                : null,
                StartTime = membership?.StartTime ?? null,
                EndTime = membership?.EndTime ?? null,
                AccountLockEnd = user.LockoutEnd,
                ImgEditLockEnd = user.ImgEditLockEnd,
                AvatarPath = user.AvatarPath ?? "/images/no_avt.jpg"
            };

            //Get all MembershipType
            var allMembershipTypes = new List<string> {"None", "Standard", "Premium", "Professional"};
            
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
                Claims = claims
            };
            return View(model);
        }

        //POST: /manageUser/UpdateMembershipUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMembershipUserAsync([Bind("UserId", "UserInfo")]ManageUserModel model)
        {
            if (string.IsNullOrEmpty(model.UserId))
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new{success = false});
            }
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                StatusMessage = "Error Không tìm thấy người dùng.";
                return Json(new { success = false });
            }

            var membershipType = model.UserInfo.MembershipType == "Standard" ? Membership.Standard :
                                            model.UserInfo.MembershipType == "Premium" ? Membership.Premium :
                                            model.UserInfo.MembershipType == "Professional" ? Membership.Professional :
                                            Membership.None;
            var startTime = model.UserInfo.StartTime ?? null;
            var endTime = model.UserInfo.EndTime ?? null;

            var membership = await _dbContext.Memberships.Where(m => m.UserId == model.UserId).FirstOrDefaultAsync();
            if (membership == null)
            {
                if (membershipType == Membership.None)
                {
                    StatusMessage = "Đã cập nhật membership cho tài khoản.";
                    return Json(new { success = true });
                }
                membership = new MembershipsModel
                {
                    UserId = model.UserId,
                    MembershipType = membershipType,
                    StartTime = startTime,
                    EndTime = endTime,
                    User = user
                };
                await _dbContext.Memberships.AddAsync(membership);
            }
            else
            {
                if (membershipType == Membership.None)
                {
                    _dbContext.Remove(membership);
                }
                else
                {
                    membership.MembershipType = membershipType;
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

        //POST: /manageUser/UpdateRoleUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRoleUserAsync([Bind("UserId", "UserRoleNames")]ManageUserModel model)
        {
            if (string.IsNullOrEmpty(model.UserId))
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new{success = false});
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
        public async Task<IActionResult> LockAccountOptionsAsync([Bind("UserId, UserInfo")]ManageUserModel model)
        { 
            if (string.IsNullOrEmpty(model.UserId))
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new{success = false});
            }
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new{success = false});
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
                return Json(new {success = false});
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
        public async Task<IActionResult> ResetPasswordAsync(string userId)
        {
            _logger.LogError(string.Empty, userId);
            if (string.IsNullOrEmpty(userId))
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new{success = false});
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new{success = false});
            }

            await _userManager.RemovePasswordAsync(user);
            var result = await _userManager.AddPasswordAsync(user, userId);
            if (!result.Succeeded)
            {
                StatusMessage = "Error Đặt lại mật khẩu thất bại.";
                return RedirectToAction(nameof(Index));
            }

            StatusMessage = "Đã đặt lại mật khẩu.";
            return Json(new{success = true});
        }

        //POST: /manageUser/DeleteAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccountAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new{success = false});
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = " Error Không tìm thấy tài khoản.";
                return Json(new{success = false});
            }

            var result = await _deleteUser.DeleteUserAsync(user.Id);
            if (!result)
            {
                StatusMessage = "Error Xoá tài khoản thất bại.";
                return Json(new{success = false});
            }

            StatusMessage = "Đã xoá tài khoản.";
            return Json(new{success = true, redirect = Url.Action("Index")});
        }
    }
}
