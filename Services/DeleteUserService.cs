#nullable disable

using App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public interface IDeleteUserService
{
    Task<bool> DeleteUserAsync(string userId);
}

public class DeleteUserService : IDeleteUserService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _env;

    public DeleteUserService(
        UserManager<AppUser> userManager, 
        AppDbContext dbContext,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _env = env;
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        var avatarPath = user.AvatarPath;
        if (!string.IsNullOrEmpty(avatarPath))
        {
            if (avatarPath.StartsWith("/imgs/"))
            {
                var filePath = Path.Combine(_env.ContentRootPath, "Images/" + avatarPath.Substring(6));
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }
        var userImageFolder = Path.Combine(_env.ContentRootPath, "Images", "Edited", user.Id);

        if (Directory.Exists(userImageFolder))
        {
            try
            {
                Directory.Delete(userImageFolder, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi xóa thư mục ảnh của người dùng: {ex.Message}");
            }
        }

        var deletedUser = await _userManager.FindByNameAsync("DeletedUser");
        var deletedUserId = string.Empty;
        if (deletedUser == null)
        {
            var id = Guid.NewGuid().ToString();
            await _userManager.CreateAsync(new AppUser
            {
                UserName = "DeletedUser",
                Email = "deleted@perfectpix.art",
                Id = id
            });
            deletedUserId = id;
        }
        else
        {
            deletedUserId = deletedUser.Id;
        }

        var userPayments = await _dbContext.Payments
            .Where(p => p.UserId == userId)
            .ToListAsync();

        foreach (var payment in userPayments)
        {
            payment.UserId = deletedUserId;
        }

        _dbContext.Payments.UpdateRange(userPayments);
        await _dbContext.SaveChangesAsync();

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded;
    }
}
