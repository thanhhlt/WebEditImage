#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace App.Areas.Identity.Models.RoleViewModels
{
  public class EditRoleModel
    {
        public string RoleId { get; set; }

        [Display(Name = "Tên của role")]
        [Required(ErrorMessage = "{0} không được bỏ trống.")]
        [StringLength(256, MinimumLength = 3, ErrorMessage = "{0} dài từ {2} đến {1} ký tự.")]
        public string Name { get; set; }

        public List<EditClaimModel> Claims { get; set; } = new List<EditClaimModel>();

        public EditClaimModel Claim { get; set; }
    }
}
