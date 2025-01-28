#nullable disable

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using App.Models;
using App.Areas.Contact.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _dbContext;

    public HomeController(
        ILogger<HomeController> logger,
        AppDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public class IndexViewModel
    {
        public List<MembershipDetailsModel> MembershipDetails { get; set; }
        public SendContactModel Contact { get; set; }
    }

    public async Task<IActionResult> Index()
    {
        var model = new IndexViewModel
        {
            MembershipDetails = await _dbContext.MembershipDetails.ToListAsync()
        };
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
