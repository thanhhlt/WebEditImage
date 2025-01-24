// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel.DataAnnotations;

namespace App.Areas.Identity.Models.AccountViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "{0} không được bỏ trống.")]
        [EmailAddress(ErrorMessage = "{0} sai định dạng.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [DataType(DataType.Text)]
        [Display(Name = "Tên tài khoản")]
        [Required(ErrorMessage = "{0} không được bỏ trống.")]
        [StringLength(100, ErrorMessage = "{0} dài từ {2} đến {1} ký tự.", MinimumLength = 3)]
        public string UserName { get; set; }

        [Required(ErrorMessage = "{0} không được bỏ trống.")]
        [StringLength(50, ErrorMessage = "{0} dài từ {2} đến {1} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Sai mật khẩu xác nhận.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "{0} không được bỏ trống.")]
        [Display(Name = "Số điện thoại")]
        [RegularExpression(@"^0\d+$", ErrorMessage = "{0} sai định dạng.")]
        public string PhoneNumber { get; set; }
    }
}
