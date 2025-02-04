using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.Security.Requirements;

public class AppAuthorizationHandler : IAuthorizationHandler
{
    private readonly ILogger<AppAuthorizationHandler> _logger;
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _dbContext;

    public AppAuthorizationHandler(
        ILogger<AppAuthorizationHandler> logger,
        UserManager<AppUser> userManager,
        AppDbContext dbContext
    )
    {
        _logger = logger;
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        var pendingRequirements = context.PendingRequirements.ToList();
        foreach(var requirement in pendingRequirements)
        {
            if (requirement is FeatureAccessRequirement)
            {
                if (await CanAccessFeature(context.User, context.Resource, (FeatureAccessRequirement)requirement))
                {
                    context.Succeed(requirement);
                }
            }
            if (requirement is ImageSaveRequirement)
            {
                if (await CanSaveImage(context.User, context.Resource, (ImageSaveRequirement)requirement))
                {
                    context.Succeed(requirement);
                }
            }
            if (requirement is ImageQuotaRequirement)
            {
                if (await CanProcessImage(context.User, context.Resource, (ImageQuotaRequirement)requirement))
                {
                    context.Succeed(requirement);
                }
            }
        }
    }

    private async Task<bool> CanAccessFeature(ClaimsPrincipal user, object? resource, FeatureAccessRequirement requirement)
    {
        if (resource is not string task)
        {
            _logger.LogWarning("Resource không hợp lệ hoặc không có giá trị.");
            return false;
        }

        ActionEdit actionTaken = task.ToLower() switch
        {
            "resolution-enht" => ActionEdit.ResolutionEnht,
            "unblur" => ActionEdit.Unblur,
            "object-remove" => ActionEdit.ObjectRemoval,
            "background-blur" => ActionEdit.BackgroundBlur,
            "denoise" => ActionEdit.Denoise,
            "color-enht" => ActionEdit.ColorEnht,
            _ => ActionEdit.None
        };

        if (actionTaken == ActionEdit.None)
        {
            _logger.LogWarning("Tính năng yêu cầu không hợp lệ: {Task}", task);
            return false;
        }

        var appUser = await _userManager.GetUserAsync(user);
        if (appUser == null) return false;
        if (!appUser.EmailConfirmed) return false;
        
        var membershipDetails = await _dbContext.Memberships.AsNoTracking()
                                            .Where(m => m.UserId == appUser.Id)
                                            .Select(m => m.MembershipDetails)
                                            .FirstOrDefaultAsync();

        if (membershipDetails == null)
        {
            membershipDetails = await _dbContext.MembershipDetails.AsNoTracking()
                                                    .Where(md => md.MembershipType == MemberType.Free)
                                                    .FirstOrDefaultAsync();
        }
        if (membershipDetails == null)
        {
            _logger.LogWarning("Lỗi lấy thông tin xác thực.");
            return false;
        }

        return actionTaken switch
        {
            ActionEdit.ResolutionEnht => membershipDetails.ResolutionEnhancement,
            ActionEdit.Unblur => membershipDetails.Unblur,
            ActionEdit.ObjectRemoval => membershipDetails.ObjectRemoval,
            ActionEdit.BackgroundBlur => membershipDetails.BackgroundBlur,
            ActionEdit.Denoise => membershipDetails.Denoise,
            ActionEdit.ColorEnht => membershipDetails.ColorEnhancement,
            _ => false
        };
    }

    private async Task<bool> CanSaveImage(ClaimsPrincipal user, object? resource, ImageSaveRequirement requirement)
    {
        var appUser = await _userManager.GetUserAsync(user);
        if (appUser == null) return false;
        if (!appUser.EmailConfirmed) return false;

        int usedStorage = (int)((await _dbContext.EditedImages
                                            .Where(i => i.UserId == appUser.Id)
                                            .SumAsync(i => (long?)i.ImageKBSize) ?? 0) / 1024);

        int? storageLimitMB = await _dbContext.Memberships.AsNoTracking()
                                            .Where(m => m.UserId == appUser.Id)
                                            .Select(m => m.MembershipDetails != null ? (int?)m.MembershipDetails.StorageLimitMB : null)
                                            .FirstOrDefaultAsync();
        if (storageLimitMB == null)
        {
            storageLimitMB = await _dbContext.MembershipDetails.AsNoTracking()
                                                    .Where(md => md.MembershipType == MemberType.Free)
                                                    .Select(md => md.StorageLimitMB)
                                                    .FirstOrDefaultAsync();
        }

        return storageLimitMB > usedStorage;
    }

    private async Task<bool> CanProcessImage(ClaimsPrincipal user, object? resource, ImageQuotaRequirement requirement)
    {
        var appUser = await _userManager.GetUserAsync(user);
        if (appUser == null) return false;
        if (!appUser.EmailConfirmed) return false;

        DateTime today = DateTime.Now.Date;
        int usedImageQuota = await _dbContext.EditedImages.AsNoTracking()
                                                .Where(i => i.UserId == appUser.Id && i.EditedAt >= today)
                                                .CountAsync();

        int? maxImageCount = await _dbContext.Memberships.AsNoTracking()
                                            .Where(m => m.UserId == appUser.Id)
                                            .Select(m => m.MembershipDetails != null ? (int?)m.MembershipDetails.MaxImageCount : null)
                                            .FirstOrDefaultAsync();
        if (maxImageCount == null)
        {
            maxImageCount = await _dbContext.MembershipDetails.AsNoTracking()
                                                    .Where(md => md.MembershipType == MemberType.Free)
                                                    .Select(md => md.MaxImageCount)
                                                    .FirstOrDefaultAsync();
        }

        return usedImageQuota < maxImageCount;
    }
}