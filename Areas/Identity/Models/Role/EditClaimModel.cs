#nullable disable

using System.ComponentModel.DataAnnotations;

namespace App.Areas.Identity.Models.RoleViewModels
{
  public class EditClaimModel
  {
    public int? ClaimId { get; set; }

    [Display(Name = "Tên claim")]
    [Required(ErrorMessage = "{0} không được bỏ trống.")]
    [StringLength(256, MinimumLength = 3, ErrorMessage = "{0} dài từ {2} đến {1} ký tự.")]
    public string ClaimType { get; set; }

    [Display(Name = "Giá trị")]
    [Required(ErrorMessage = "{0} không được bỏ trống.")]
    [StringLength(256, MinimumLength = 3, ErrorMessage = "{0} dài từ {2} đến {1} ký tự.")]
    public string ClaimValue { get; set; }
  }
}
