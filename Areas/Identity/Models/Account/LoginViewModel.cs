// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel.DataAnnotations;

namespace App.Areas.Identity.Models.AccountViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "{0} không được bỏ trống.")]
        [Display(Name = "Email hoặc tài khoản")]
        public string UserNameOrEmail { get; set; }


        [Required(ErrorMessage = "{0} không được bỏ trống.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Display(Name = "Nhớ thông tin")]
        public bool RememberMe { get; set; }
    }
}
