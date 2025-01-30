using App.Areas.Membership.Models.MshipRegister;
using App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Areas.Membership.Controllers;

[Area("Membership")]
[Route("/register-membership/[action]")]
public class MshipRegisterController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;

    public MshipRegisterController(
        AppDbContext dbContext,
        UserManager<AppUser> userManager
    )
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet("/register-membership")]
    public async Task<IActionResult> Index()
    {
        var model = new IndexViewModel();
        model.MembershipDetails = await _dbContext.MembershipDetails.ToListAsync();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return View(model);
        }
        model.MemberType = await _dbContext.Memberships.AsNoTracking()
                                        .Where(m => m.UserId == user.Id)
                                        .Select(m => m.MembershipDetails != null ? m.MembershipDetails.MembershipType : MemberType.Free)
                                        .FirstOrDefaultAsync();
        return View(model);
    }
}