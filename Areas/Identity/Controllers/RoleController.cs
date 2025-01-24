// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Security.Claims;
using App.Areas.Identity.Models.RoleViewModels;
using App.ExtendMethods;
using App.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Areas.Identity.Controllers
{

    [Area("Identity")]
    [Route("/manage-role/[action]")]
    public class RoleController : Controller
    {
        private readonly ILogger<RoleController> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;

        public RoleController(
            ILogger<RoleController> logger, 
            RoleManager<IdentityRole> roleManager, 
            AppDbContext dbContext, 
            UserManager<AppUser> userManager)
        {
            _logger = logger;
            _roleManager = roleManager;
            _dbContext = dbContext;
            _userManager = userManager;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public IActionResult GetStatusMessage()
        {
            return PartialView("_StatusMessage");
        }
        public IActionResult GetClaimsTableBody(List<EditClaimModel> model)
        {
            return PartialView("_ClaimsTableBody", model);
        }

        // GET: /role
        [HttpGet("/manage-role")]
        public async Task<IActionResult> Index()
        {            
           var r = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
           RoleModel model = new RoleModel();
           foreach (var _r in r)
           {
               var claims = await _roleManager.GetClaimsAsync(_r);
               var claimsString = claims.Select(c => c.Type  + "=" + c.Value);

               ClaimsRole claimsRole = new ClaimsRole()
               {
                    Id = _r.Id,
                    Name = _r.Name,
                    Claims = claimsString.ToArray()
               };
               model.ClaimsRoles.Add(claimsRole);
           }

            return View(model);
        }
        
        // POST: /role/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAsync(RoleModel model)
        {
            if  (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Index));
            }

            var newRole = new IdentityRole(model.CreateRoleModel.Name);
            var result = await _roleManager.CreateAsync(newRole);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(result);
                return RedirectToAction(nameof(Index));
            }
            StatusMessage = $"Bạn vừa tạo role mới: {newRole}";
            return RedirectToAction(nameof(Index));
        }     
        
        // POST: /role/Delete/roleid
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAsync(RoleModel model)
        {
            var idRole = model.IdRoleDelete;
            if (idRole == null) return NotFound("Không tìm thấy role");
            var role = await _roleManager.FindByIdAsync(idRole);
            if  (role == null) return NotFound("Không tìm thấy role");
             
            var result = await _roleManager.DeleteAsync(role);

            if (result.Succeeded)
            {
                StatusMessage = $"Đã xóa: {role.Name}";
            }
            else
            {
                ModelState.AddModelError(result);
            }
            return RedirectToAction(nameof(Index));
        }     

        // GET: /role/Edit/roleid
        [HttpGet("{roleid}")]
        public async Task<IActionResult> EditAsync(string roleid)
        {
            if (roleid == null) return NotFound("Không tìm thấy role");
            var role = await _roleManager.FindByIdAsync(roleid);
            if (role == null)
            {
                return NotFound("Không tìm thấy role");
            }

            EditRoleModel model = new EditRoleModel();
            model.Name = role.Name;
            List<IdentityRoleClaim<string>> claims = await _dbContext.RoleClaims.Where(rc => rc.RoleId == role.Id).ToListAsync();
            foreach (var rc in claims)
            {
                EditClaimModel claim = new EditClaimModel()
                {
                    ClaimId = rc.Id,
                    ClaimType = rc.ClaimType,
                    ClaimValue = rc.ClaimValue
                };
                model.Claims.Add(claim);
            }
            model.RoleId = roleid;
            ModelState.Clear();
            return View(model);
        }

        //POST: /role/RenameRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenameRoleAsync([Bind("RoleId", "Name")]EditRoleModel model)
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                StatusMessage = $"Error Đổi tên role thất bại.<br/> {string.Join("<br/>", errorMessages)}";
                return Json(new {success = false});
            }

            var roleId = model.RoleId;
            if (roleId == null)
            {
                StatusMessage = " Error Không tìm thấy role.";
                return Json(new {success = false});
            }
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                StatusMessage = "Error Không tìm thấy role.";
                return Json(new {success = false});
            }

            role.Name = model.Name;
            var result  = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                StatusMessage = "Error Đổi tên role thất bại.";
                return Json(new{success = false});
            }
            StatusMessage = "Đã đổi tên role.";
            return Json(new {success = true, name = role.Name});
        }

        // POST: /role/AddRoleClaim/roleid
        [HttpPost]  
        [ValidateAntiForgeryToken]      
        public async Task<IActionResult> AddRoleClaimAsync([Bind("RoleId", "Claim")]EditRoleModel model)
        {
            ModelState.Remove("Name");
            if (!ModelState.IsValid)
            {
                var errorMessages = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                StatusMessage = $"Error Thêm claim thất bại.<br/> {string.Join("<br/>", errorMessages)}";
                return Json(new {success = false});
            }

            var roleId = model.RoleId;
            if (roleId == null)
            {
                StatusMessage = " Error Không tìm thấy role.";
                return Json(new {success = false});
            }
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                StatusMessage = "Error Không tìm thấy role.";
                return Json(new {success = false});
            }
            var tmp = await _roleManager.GetClaimsAsync(role);

            if ((await _roleManager.GetClaimsAsync(role)).Any(c => c.Type == model.Claim.ClaimType && c.Value == model.Claim.ClaimValue))
            {
                StatusMessage = "Error Claim này đã có trong role.";
                return Json(new {success = false});
            }

            var newClaim = new Claim(model.Claim.ClaimType, model.Claim.ClaimValue);
            var result = await _roleManager.AddClaimAsync(role, newClaim);
            
            if (!result.Succeeded)
            {
                StatusMessage = "Error Thêm claim thất bại với lỗi:";
                foreach (var error in result.Errors)
                {  
                    StatusMessage += $"<br/>{error.Description}";
                }
                return Json(new {success = false});
            }
            
            StatusMessage = "Đã thêm claim mới";          
            return PartialView("_ClaimsTableBody", await GetRoleClaimsAsync(role));
        }

        //POST: /role/DeleteRoleClaim
        [HttpPost]
        // [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRoleClaimAsync(int claimId)
        {
            var claim = _dbContext.RoleClaims.Where(c => c.Id == claimId).FirstOrDefault();
            if (claim == null)
            {
                StatusMessage = " Error Không tìm thấy claim.";
                return Json(new {success = false});
            }
            var role = await _roleManager.FindByIdAsync(claim.RoleId);
            if (role == null)
            {
                StatusMessage = "Error Không tìm thấy role.";
                return Json(new {success = false});
            }

            var result = await _roleManager.RemoveClaimAsync(role, new Claim(claim.ClaimType, claim.ClaimValue));           
            if (!result.Succeeded)
            {
                StatusMessage = "Error Xoá claim thất bại với lỗi:";
                foreach (var error in result.Errors)
                {  
                    StatusMessage += $"<br/>{error.Description}";
                }
                return Json(new {success = false});
            }
            
            StatusMessage = "Đã xoá claim.";           
            return PartialView("_ClaimsTableBody", await GetRoleClaimsAsync(role));
        }

        //POST: /role/EditRoleClaim
        [HttpPost]
        public async Task<IActionResult> EditRoleClaimAsync(int claimId, string claimType, string claimValue)
        {
            if (claimType == null || claimValue == null)
            {
                StatusMessage = "Error Tên claim và giá trị không được bỏ trống.";
                return Json(new {success = false});
            }
            if (claimType.Length < 3 || claimType.Length > 256 || claimValue.Length < 3 || claimValue.Length > 256)
            {
                StatusMessage = "Error Tên claim và giá trị dài từ 3 đến 256 ký tự.";
                return Json(new {success = false});
            }

            var claim = _dbContext.RoleClaims.Where(c => c.Id == claimId).FirstOrDefault();
            if (claim == null)
            {
                StatusMessage = " Error Không tìm thấy claim.";
                return Json(new {success = false});
            }
            var role = await _roleManager.FindByIdAsync(claim.RoleId);
            if (role == null)
            {
                StatusMessage = " Error Không tìm thấy role.";
                return Json(new {success = false});
            }

            if (_dbContext.RoleClaims.Any(c => c.RoleId == role.Id && c.ClaimType == claimType && c.ClaimValue == claimValue && c.Id != claim.Id))
            {
                StatusMessage = "Error Claim này đã có trong role";
                return Json(new {success = false});
            }
 
            claim.ClaimType = claimType;
            claim.ClaimValue = claimValue;
            
            await _dbContext.SaveChangesAsync();
            
            StatusMessage = "Đã cập nhật claim.";           
            return PartialView("_ClaimsTableBody", await GetRoleClaimsAsync(role));
        }

        //POST: /role/ReloadRoleClaim
        [HttpGet]
        public async Task<IActionResult> ReloadRoleClaimAsync(int claimId)
        {
            var claim = _dbContext.RoleClaims.Where(c => c.Id == claimId).FirstOrDefault();
            if (claim == null)
            {
                StatusMessage = " Error Không tìm thấy claim.";
                return Json(new {success = false});
            }
            var role = await _roleManager.FindByIdAsync(claim.RoleId);
            if (role == null)
            {
                StatusMessage = " Error Không tìm thấy role.";
                return Json(new {success = false});
            }
         
            return PartialView("_ClaimsTableBody", await GetRoleClaimsAsync(role));
        }

        private async Task<EditRoleModel> GetRoleClaimsAsync(IdentityRole role)
        {
            EditRoleModel roleClaims = new EditRoleModel();
            roleClaims.RoleId = role.Id;
            roleClaims.Name = role.Name;
            roleClaims.Claim = new EditClaimModel();
            var claims = await _dbContext.RoleClaims.Where(rc => rc.RoleId == role.Id).ToListAsync();
            foreach (var rc in claims)
            {
                EditClaimModel claim = new EditClaimModel()
                {
                    ClaimId = rc.Id,
                    ClaimType = rc.ClaimType,
                    ClaimValue = rc.ClaimValue
                };
                roleClaims.Claims.Add(claim);
            }
            return roleClaims;
        }
    }
}
