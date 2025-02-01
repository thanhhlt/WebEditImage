#nullable disable

namespace App.Areas.Membership.Models.MshipRegister;

public class PaymentResultViewModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string BankCode { get; set; } = string.Empty;
}