#nullable disable

namespace App.Areas.Identity.Models.UserViewModels
{
    public class UserInfoModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public bool EmailConfirmed { get; set; }
        public string Gender { get; set; }
        public string MembershipType { get; set; }
        public DateTime BirthDate { get; set; }
        public DateTimeOffset? AccountLockEnd { get; set; }
        public DateTimeOffset? ImgEditLockEnd  { get; set; }
        public string AvatarPath { get; set; }
    }
}