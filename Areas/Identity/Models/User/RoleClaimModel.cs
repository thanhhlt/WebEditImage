#nullable disable

using System.ComponentModel.DataAnnotations;

namespace App.Areas.Identity.Models.UserViewModels
{
  public class RoleClaimModel
  {
    public string RoleName { get; set; }
    public string ClaimType { get; set; }
    public string ClaimValue { get; set; }
  }
}