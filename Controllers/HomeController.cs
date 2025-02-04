#nullable disable

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using App.Models;
using App.Areas.Contact.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace App.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;

    public HomeController(
        ILogger<HomeController> logger,
        AppDbContext dbContext,
        UserManager<AppUser> userManager
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public class IndexViewModel
    {
        public List<MembershipDetailsModel> MembershipDetails { get; set; }
        public MemberType? MemberType { get; set; } 
        public SendContactModel Contact { get; set; }
    }

    public async Task<IActionResult> Index()
    {
        var model = new IndexViewModel
        {
            MembershipDetails = await _dbContext.MembershipDetails.ToListAsync()
        };
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

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
