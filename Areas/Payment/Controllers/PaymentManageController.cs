using App.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.Areas.Payment.Controllers;

[Area("Payment")]
[Route("/manage-payment/[action]")]
public class PaymentManageController : Controller
{
    private readonly AppDbContext _dbContext;

    public PaymentManageController(
        AppDbContext dbContext
    )
    {
        _dbContext = dbContext;
    }

    public IActionResult Index()
    {
        return View();
    }
}