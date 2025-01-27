using App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Areas.Membership.Controllers
{
    [Area("Membership")]
    [Route("/manage-membership/[action]")]
    public class MembershipMgController : Controller
    {
        private readonly ILogger<MembershipMgController> _logger;
        private readonly AppDbContext _dbContext;

        public MembershipMgController(
            ILogger<MembershipMgController> logger,
            AppDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        [TempData]
        public string? StatusMessage { get; set; }

        public IActionResult GetStatusMessage()
        {
            return PartialView("_StatusMessage");
        }

        [HttpGet("/manage-membership")]
        public async Task<IActionResult> Index()
        {
            var model = await _dbContext.MembershipDetails.ToListAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMembershipAsync(Dictionary<int, MembershipDetailsModel> memberships)
        {
            if (memberships == null || !memberships.Any())
            {
                StatusMessage = "Không có thông tin membership để cập nhập.";
                return Json(new { success = false});
            }

            foreach (var item in memberships)
            {
                var membership = await _dbContext.MembershipDetails.FindAsync(item.Key);
                if (membership != null)
                {
                    membership.Price = item.Value.Price;
                    membership.StorageLimitMB = item.Value.StorageLimitMB;
                    membership.HasAds = item.Value.HasAds;
                    membership.MaxImageCount = item.Value.MaxImageCount;
                    membership.QualityImage = item.Value.QualityImage;
                    membership.PrioritySupport = item.Value.PrioritySupport;
                }
            }
            await _dbContext.SaveChangesAsync();
            StatusMessage = "Đã cập nhập thông tin membership.";
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFeaturesAsync(Dictionary<int, Dictionary<string, bool>> features)
        {
            var allFeatures = new[]
            {
                "ImageGeneration",
                "ResolutionEnhancement",
                "Unblur",
                "ObjectRemoval",
                "BackgroundBlur",
                "ColorEnhancement",
                "Denoise"
            };

            if (features == null || !features.Any())
            {
                var memberships = await _dbContext.MembershipDetails.ToListAsync();
                foreach (var membership in memberships)
                {
                    foreach (var featureName in allFeatures)
                    {
                        typeof(MembershipDetailsModel).GetProperty(featureName)?.SetValue(membership, false);
                    }
                }
            }
            else
            {
                foreach (var membership in await _dbContext.MembershipDetails.ToListAsync())
                {
                    if (features.TryGetValue(membership.Id, out var updatedFeatures))
                    {
                        foreach (var featureName in allFeatures)
                        {
                            var value = updatedFeatures.ContainsKey(featureName) && updatedFeatures[featureName];
                            typeof(MembershipDetailsModel).GetProperty(featureName)?.SetValue(membership, value);
                        }
                    }
                    else
                    {
                        foreach (var featureName in allFeatures)
                        {
                            typeof(MembershipDetailsModel).GetProperty(featureName)?.SetValue(membership, false);
                        }
                    }
                }
            }

            await _dbContext.SaveChangesAsync();
            StatusMessage = "Đã cập nhật thông tin membership.";
            return Json(new { success = true });
        }
    }
}