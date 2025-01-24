// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel.DataAnnotations;

namespace App.Areas.Identity.Models.AccountViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "{0} không được bỏ trống.")]
        [EmailAddress(ErrorMessage="{0} sai định dạng.")]
        public string Email { get; set; }
    }
}
