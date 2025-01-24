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

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded;
    }
}
