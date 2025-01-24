#nullable disable

using System.ComponentModel.DataAnnotations;

namespace App.Areas.Identity.Models.OptionViewModels
{
    public class DeleteAccountViewmodel
    {
        [Required(ErrorMessage = "{0} không được bỏ trống.")]
        [StringLength(50, ErrorMessage = "{0} dài từ {2} đến {1} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }
    }
}