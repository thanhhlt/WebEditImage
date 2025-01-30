using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Models;

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Cancelled
}

public enum PaymentMethod
{
    [Display(Name = "Thẻ tín dụng/ghi nợ")]
    CreditCard,
    [Display(Name = "Chuyển khoản ngân hàng")]
    BankTransfer,
    [Display(Name = "Ví điện tử MoMo")]
    MoMo,
    [Display(Name = "Ví điện tử VNPay")]
    VNPay
}

public class PaymentsModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string TransactionId { get; set; } = string.Empty;

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; } = DateTime.Now;

    [Required]
    public PaymentStatus Status { get; set; }

    [Required]
    public PaymentMethod PaymentMethod { get; set; }

    public string? Notes { get; set; }

    [Required]
    public MemberType MembershipType { get; set; }

    [Required]
    [ForeignKey(nameof(User))]
    public string UserId { get; set; } = string.Empty;

    public virtual AppUser? User { get; set; }
}