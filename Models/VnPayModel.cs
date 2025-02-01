#nullable disable

namespace App.Models;

public class PaymentInformationModel
{
    public string OrderType { get; set; }
    public decimal Amount { get; set; }
    public string OrderDescription { get; set; }
}

public class PaymentResponseModel
{
    public string OrderId { get; set; }
    public string TransactionId { get; set; }
    public bool Success { get; set; }
    public string VnPayResponseCode { get; set; }
}