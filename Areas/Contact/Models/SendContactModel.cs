using System.ComponentModel.DataAnnotations;

namespace App.Areas.Contact.Models
{
    public class SendContactModel
    {
        [Required(ErrorMessage = "{0} không được bỏ trống.")]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "{0} dài từ {2} đến {1} ký tự.")]
        [Display(Name = "Tên của bạn")]
        public string Name { get; set; } = "";

        [EmailAddress(ErrorMessage = "{0} sai định dạng.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "{0} không được bỏ trống.")]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "{0} dài từ {2} đến {1} ký tự.")]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = "";

        [Required(ErrorMessage = "{0} không được bỏ trống.")]
        [StringLength(255, MinimumLength = 5, ErrorMessage = "{0} dài từ {2} đến {1} ký tự.")]
        [Display(Name = "Nội dung")]
        public string Content { get; set; } = "";
    }
}