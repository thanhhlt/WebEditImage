using Microsoft.AspNetCore.Identity;

namespace App.Models;

public enum Gender
{
        Male = 1,
        Female = 2,
        Unspecified = 3
}

public class AppUser : IdentityUser
{
        public DateTime BirthDate { get; set; } = DateTime.UtcNow;

        public Gender? Gender { get; set; }

        public string? AvatarPath { get; set; }

        public DateTimeOffset? ImgEditLockEnd { get; set; }

        public virtual ICollection<LoggedBrowsersModel>? LoggedBrowsers { get; set; }
        public virtual MembershipsModel? Membership { get; set; }
        public virtual ICollection<EditedImagesModel>? EditedImages { get; set; }
}