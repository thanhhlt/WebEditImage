#nullable disable

using App.Models;
using App.Areas.Payment.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace App.Areas.Payment.Controllers;

[Area("Payment")]
[Route("/manage-payment/[action]")]
public class PaymentManageController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;

    public PaymentManageController(
        AppDbContext dbContext,
        UserManager<AppUser> userManager
    )
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet("/manage-payment")]
    public async Task<IActionResult> Index([FromQuery(Name = "p")] int currentPage, string searchString)
    {
        ViewBag.SearchString = searchString;
        var model = new IndexViewModel();

        var payments = _dbContext.Payments.Include(p => p.User).AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {

            var paymentsSearch = payments;
            DateTime searchDate;
            decimal searchAmount;
            bool isDate = DateTime.TryParseExact(
                searchString,
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out searchDate);
            bool isAmount = decimal.TryParse(searchString, NumberStyles.Any, CultureInfo.InvariantCulture, out searchAmount);

            if (isDate)
            {
                var startOfDay = searchDate.Date;
                var endOfDay = searchDate.Date.AddDays(1);

                paymentsSearch = paymentsSearch.Where(p => p.PaymentDate >= startOfDay && p.PaymentDate < endOfDay);
            }
            else if (isAmount)
            {
                paymentsSearch = paymentsSearch.Where(p => p.Amount == searchAmount);
            }
            else
            {
                paymentsSearch = paymentsSearch.Where(p => p.TransactionId.Contains(searchString) ||
                                                        p.UserId.Contains(searchString) ||
                                                        p.User.UserName.Contains(searchString));
            }
            payments = paymentsSearch;
        }

        model.Payments = await payments.AsNoTracking()
                                .OrderByDescending(p => p.PaymentDate)
                                .Select(p => new PaymentView
                                {
                                    Id = p.Id,
                                    TransactionId = p.TransactionId,
                                    Amount = (Int32)p.Amount,
                                    PaymentDate = p.PaymentDate,
                                    UserId = p.UserId,
                                    UserName = p.User.UserName
                                }).ToListAsync();

        // Pagination
        if (model.Payments.Any())
        {
            model.currentPage = Math.Max(currentPage, 1);
            model.totalPays = model.Payments.Count();
            model.countPages = (int)Math.Ceiling((double)model.totalPays / model.ITEMS_PER_PAGE);

            if (model.currentPage > model.countPages)
                model.currentPage = model.countPages;

            model.Payments = model.Payments.Skip((model.currentPage - 1) * model.ITEMS_PER_PAGE)
                                            .Take(model.ITEMS_PER_PAGE).ToList();
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> PaymentInfoAsync(int id)
    {
        var payment = await _dbContext.Payments.AsNoTracking()
                                    .Include(p => p.User)
                                    .Where(p => p.Id == id)
                                    .FirstOrDefaultAsync();
        if (payment == null)
        {
            return NotFound("Không tìm thấy thông tin thanh toán.");
        }

        var model = new PaymentInfoModel
        {
            Id = payment.Id,
            TransactionId = payment.TransactionId,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate,
            Status = payment.Status,
            PaymentMethod = payment.PaymentMethod,
            Notes = payment.Notes,
            MembershipType = payment.MembershipType,
            UserId = payment.UserId,
            UserName = payment.User?.UserName ?? "N/A"
        };

        return View(model);
    }
}