namespace App.Areas.Payment.Models;

public class PaymentView
{
    public int Id { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public int Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Now;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}

public class IndexViewModel
{
    public int totalPays { get; set; }
    public int countPages { get; set; }

    public int ITEMS_PER_PAGE { get; set; } = 10;

    public int currentPage { get; set; }

    public string MessageSearchResult { get; set; } = string.Empty;

    public List<PaymentView> Payments { get; set; } = new List<PaymentView>();
}