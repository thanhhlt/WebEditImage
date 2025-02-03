using App.Models;
using App.Areas.ImageTools.Models.ManageIMG;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace App.Areas.ImageTools.Controllers;

[Area("ImageTools")]
[Route("manage-img/[action]")]
public class ManageIMGController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public ManageIMGController(
        AppDbContext dbContext,
        UserManager<AppUser> userManager,
        IWebHostEnvironment env
    )
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _env = env;
    }

    public async Task<IActionResult> Index([FromQuery(Name = "p")] int currentPage, ActionEdit? actionType)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("Không tìm thấy tài khoản.");
        }

        var images = await _dbContext.EditedImages.AsNoTracking()
                                .Where(i => i.UserId == user.Id)
                                .Select(i => new ImageView
                                {
                                    Id = i.Id,
                                    ImagePath = i.ImageUrl,
                                    ThumbPath = i.ThumbUrl,
                                    ActionTaken = i.ActionTaken
                                }).ToListAsync();
        if (actionType.HasValue && actionType.Value != ActionEdit.None)
        {
            images = images.Where(img => img.ActionTaken == actionType.Value).ToList();
        }

        var model = new IndexViewModel();
        model.FilterAction = actionType;
        // Pagination
        if (images.Any())
        {
            model.currentPage = Math.Max(currentPage, 1);
            model.totalImages = images.Count();
            model.countPages = (int)Math.Ceiling((double)model.totalImages / model.ITEMS_PER_PAGE);

            if (model.currentPage > model.countPages)
                model.currentPage = model.countPages;

            model.Images = images.Skip((model.currentPage - 1) * model.ITEMS_PER_PAGE)
                                .Take(model.ITEMS_PER_PAGE).ToList();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImageAsync(int imageId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("Không tìm thấy tài khoản.");
        }

        var image = await _dbContext.EditedImages
                            .Where(i => i.Id == imageId)
                            .FirstOrDefaultAsync();
        if (image == null)
        {
            return NotFound("Không tìm thấy ảnh.");
        }
        if (user.Id != image.UserId)
        {
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
        return Json(new { success = true });
    }
}