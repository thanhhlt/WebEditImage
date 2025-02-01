using System.Threading.Tasks;
using App.Areas.Membership.Models.MshipRegister;
using App.Libraries;
using App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace App.Areas.Membership.Controllers;

[Area("Membership")]
[Route("/register-membership/[action]")]
public class MshipRegisterController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;
    private readonly IVnPayService _vnPayService;
    private readonly IConfiguration _configuration;

    public MshipRegisterController(
        AppDbContext dbContext,
        UserManager<AppUser> userManager,
        IVnPayService vnPayService,
        IConfiguration configuration
    )
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _vnPayService = vnPayService;
        _configuration = configuration;
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

    [HttpGet]
    public async Task<IActionResult> PaymentMshipAsync(string membershipType)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("Không có tài khoản nào đang đăng nhập.");
        }
        var memberTypeUser = await _dbContext.Memberships.AsNoTracking()
                                                .Where(m => m.UserId == user.Id)
                                                .Select(m => m.MembershipDetails != null ? m.MembershipDetails.MembershipType : (MemberType?)null)
                                                .FirstOrDefaultAsync() ?? MemberType.Free;
        var memberTypeRegister = (MemberType)Enum.Parse(typeof(MemberType), membershipType);
        var membershipPrice = await _dbContext.MembershipDetails.AsNoTracking()
                                                .Where(md => md.MembershipType == memberTypeRegister)
                                                .Select(md => md.Price).FirstOrDefaultAsync();
        TempData["MembershipType"] = membershipType;
        ViewBag.MembershipPrice = membershipPrice;
        if (memberTypeUser > memberTypeRegister)
        {
            return Forbid();
        }
        PaymentResultViewModel? model = null;
        if (TempData["PaymentResult"] is string jsonData)
        {
            model = JsonConvert.DeserializeObject<PaymentResultViewModel>(jsonData);
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GenerateVNPayQRCodeAsync(string membershipType, int duration)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("Không có tài khoản nào đang đăng nhập.");
        }

        var amount = await GetAmountPaymentAsync(membershipType, duration);
        if (amount == 0)
        {
            return Json(new { message = "Giá tiền phải lớn hơn 0" });
        }

        string paymentUrl = GeneratePaymentUrl(amount);
        return Json (new {url = paymentUrl, price = amount});
    }
    private string GeneratePaymentUrl(decimal amount)
    {
        var paymentInformation = new PaymentInformationModel() {
            OrderType = "other",
            Amount = amount,
            OrderDescription = "Thanh toan membership tren PerfectPix.art."
        };
        var paymentUrl = _vnPayService.CreatePaymentUrl(paymentInformation, HttpContext);

        return paymentUrl;
    }
    private async Task<int> GetAmountPaymentAsync(string membershipType, int duration)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return 0;
        }
        var memberTypeUser = await _dbContext.Memberships.AsNoTracking()
                                                .Where(m => m.UserId == user.Id)
                                                .Select(m => m.MembershipDetails != null ? m.MembershipDetails.MembershipType : (MemberType?)null)
                                                .FirstOrDefaultAsync() ?? MemberType.Free;
        var memberTypeRegister = (MemberType)Enum.Parse(typeof(MemberType), membershipType);
        if (memberTypeUser > memberTypeRegister)
        {
            return 0;
        }

        var membershipPrice = await _dbContext.MembershipDetails.AsNoTracking()
                                                .Where(md => md.MembershipType == memberTypeRegister)
                                                .Select(md => md.Price).FirstOrDefaultAsync();
        var membershipDetailsModel = new MembershipDetailsModel();
        membershipDetailsModel.Price = membershipPrice;
        var amountPayment = membershipDetailsModel.CalculateDiscountedPrice(duration);

        return amountPayment;
    }

    [HttpGet]
    public async Task<IActionResult> PaymentCallbackAsync()
    {
        var model = new PaymentResultViewModel();
        if (Request.Query.Count == 0)
        {
            model.Success = false;
            model.Message = "Dữ liệu không hợp lệ";
            model.OrderId = "N/A";
            model.Amount = 0;
            model.BankCode = "N/A";
            TempData["PaymentResult"] = JsonConvert.SerializeObject(model);
            return RedirectToAction("PaymentMship", new { membershipType = TempData["MembershipType"] });
        }

        string vnp_HashSecret = _configuration["VNPay:HashSecret"] ?? "";
        var vnpayData = Request.Query;
        VnPayLibrary vnpay = new VnPayLibrary();

        foreach (var key in vnpayData.Keys)
        {
            if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
            {
                vnpay.AddResponseData(key, vnpayData[key]!);
            }
        }

        long orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef"));
        long vnpayTranId = Convert.ToInt64(vnpay.GetResponseData("vnp_TransactionNo"));
        string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
        string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
        string vnp_SecureHash = Request.Query["vnp_SecureHash"]!;
        long vnp_Amount = Convert.ToInt64(vnpay.GetResponseData("vnp_Amount")) / 100;
        string bankCode = Request.Query["vnp_BankCode"]!;

        model.OrderId = orderId.ToString();
        model.Amount = vnp_Amount;
        model.BankCode = bankCode;

        bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
        if (!checkSignature)
        {
            model.Success = false;
            model.Message = "Xác thực chữ ký không hợp lệ";
            TempData["PaymentResult"] = JsonConvert.SerializeObject(model);
            return RedirectToAction("PaymentMship", new { membershipType = TempData["MembershipType"] });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            model.Success = false;
            model.Message = "Không tìm thấy tài khoản";
            TempData["PaymentResult"] = JsonConvert.SerializeObject(model);
            return RedirectToAction("PaymentMship", new { membershipType = TempData["MembershipType"] });
        }

        PaymentStatus paymentStatus = vnp_TransactionStatus switch
        {
            "00" => PaymentStatus.Completed,
            "01" or "05" or "06" => PaymentStatus.Pending,
            "02" or "07" or "09" => PaymentStatus.Failed,
            "04" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Failed
        };
        string statusNote = $"VNPay Status Code: {vnp_TransactionStatus}";
        bool isPaymentSuccess = paymentStatus == PaymentStatus.Completed;
        var payment = new PaymentsModel
        {
            TransactionId = vnpayTranId.ToString(),
            Amount = vnp_Amount,
            PaymentDate = DateTime.Now,
            Status = paymentStatus,
            PaymentMethod = PaymentMethod.VNPay,
            MembershipType = (MemberType)Enum.Parse(typeof(MemberType), TempData["MembershipType"]?.ToString() ?? "Free"),
            UserId = user.Id,
            Notes = $"VNPay OrderId: {orderId} | {statusNote}"
        };
        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync();

        if (isPaymentSuccess)
        {
            await UpdateUserMembership(user.Id, TempData["MembershipType"]?.ToString() ?? "Free", vnp_Amount);
            model.Success = true;
            model.Message = "Membership đã được cập nhật!";
        }
        else
        {
            model.Success = false;
            model.Message = $"Lỗi: {statusNote}";
        }

        TempData["PaymentResult"] = JsonConvert.SerializeObject(model);
        return RedirectToAction("PaymentMship", new { membershipType = TempData["MembershipType"] });
    }

    private async Task UpdateUserMembership(string userId, string membershipTypeStr, decimal amountPaid)
    {
        var membershipType = Enum.Parse<MemberType>(membershipTypeStr);
        var membershipDetail = await _dbContext.MembershipDetails.AsNoTracking()
                                    .Where(md => md.MembershipType == membershipType)
                                    .FirstOrDefaultAsync();

        if (membershipDetail == null)
            return;

        int monthsPurchased = 0;
        for (int i = 1; i <= 24; i++)
        {
            if (membershipDetail.CalculateDiscountedPrice(i) == amountPaid)
            {
                monthsPurchased = i;
                break;
            }
        }

        if (monthsPurchased == 0)
            return;

        var userMembership = await _dbContext.Memberships
                                .FirstOrDefaultAsync(m => m.UserId == userId);

        if (userMembership == null)
        {
            userMembership = new MembershipsModel
            {
                UserId = userId,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddMonths(monthsPurchased),
                MembershipDetailsId = membershipDetail.Id
            };
            _dbContext.Memberships.Add(userMembership);
        }
        else
        {
            if (userMembership.EndTime >= DateTime.Now)
            {
                userMembership.EndTime = userMembership.EndTime.Value.AddMonths(monthsPurchased);
            }
            else
            {
                userMembership.StartTime = DateTime.Now;
                userMembership.EndTime = DateTime.Now.AddMonths(monthsPurchased);
            }
            userMembership.MembershipDetailsId = membershipDetail.Id;
        }

        await _dbContext.SaveChangesAsync();
    }
}