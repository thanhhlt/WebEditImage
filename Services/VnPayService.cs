#nullable disable

using App.Models;
using App.Libraries;

public interface IVnPayService
{
    string CreatePaymentUrl(PaymentInformationModel model, HttpContext context);
}

public class VnPayService : IVnPayService
{
    private readonly IConfiguration _configuration;

    public VnPayService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreatePaymentUrl(PaymentInformationModel model, HttpContext context)
    {
        var vnpay = new VnPayLibrary();

        string vnp_TmnCode = _configuration["Vnpay:TmnCode"];
        string vnp_HashSecret = _configuration["Vnpay:HashSecret"];
        string vnp_BaseUrl = _configuration["Vnpay:BaseUrl"];
        string vnp_ReturnUrl = _configuration["Vnpay:ReturnUrl"];
        
        var timeNow = DateTime.UtcNow.AddHours(7);
        string txnRef = DateTime.Now.Ticks.ToString();
        
        vnpay.AddRequestData("vnp_Version", "2.1.0");
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
        vnpay.AddRequestData("vnp_Amount", ((int)model.Amount * 100).ToString());
        vnpay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_CurrCode", "VND");
        vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(context));
        vnpay.AddRequestData("vnp_Locale", "vn");
        vnpay.AddRequestData("vnp_OrderInfo", model.OrderDescription);
        vnpay.AddRequestData("vnp_OrderType", model.OrderType);
        vnpay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl);
        vnpay.AddRequestData("vnp_TxnRef", txnRef);

        return vnpay.CreateRequestUrl(vnp_BaseUrl, vnp_HashSecret);
    }

}