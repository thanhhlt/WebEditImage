#nullable disable

using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Areas.Identity.Models.UserViewModels
{
    public class ManageUserModel
    {
        public string UserId { get; set; }
        public UserInfoModel UserInfo { get; set; }
        public SelectList AllRoleNames { get; set; }
        public string[] UserRoleNames { get; set; }
        public SelectList AllMembershipTypes { get; set; }
        public List<RoleClaimModel> Claims { get; set; } = new List<RoleClaimModel>();
    }
}