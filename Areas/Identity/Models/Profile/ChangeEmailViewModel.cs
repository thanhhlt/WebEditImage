// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel.DataAnnotations;

namespace App.Areas.Identity.Models.ProfileViewModels
{
    public class ChangeEmailViewModel
    {
        [Required(ErrorMessage = "{0} không được bỏ trống.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nhập mật khẩu hiện tại để xác thực.")]
        public string Password { get; set; }

        [Display(Name = "Email hiện tại")]
        public string OldEmail { get; set; }

        [Required(ErrorMessage = "{0} không được bỏ trống.")]
        [EmailAddress(ErrorMessage = "{0} sai định dạng.")]
        [Display(Name = "Email mới")]
        public string NewEmail { get; set; }
    }
}
