using App.Models;

namespace App.Areas.Payment.Models;

public class PaymentInfoModel : PaymentsModel
{
    public string UserName { get; set; } = string.Empty;
}