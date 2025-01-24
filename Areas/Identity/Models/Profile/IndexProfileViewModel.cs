// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using App.Models;

namespace App.Areas.Identity.Models.ProfileViewModels
{
    public class IndexProfileViewModel
    {
        [Display(Name = "Tên tài khoản")]
        public string? UserName { get; set; }

        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Số điện thoại")]
        [RegularExpression(@"^0\d+$", ErrorMessage = "{0} sai định dạng.")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "{0} không được bỏ trống.")]
        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; } = DateTime.Now;

        [Display(Name = "Giới tính")]
        public Gender? Gender { get; set; }     

        public bool EmailConfirmed { get; set; }

        public string? FilePath { get; set; }

        [Display(Name = "Ảnh đại diện")]
        [FileExtensions(Extensions = "jpg,png,jpeg,webp,gif")]
        public IFormFile? ImageAvatar { get; set; }
    }
}
