using System.ComponentModel.DataAnnotations;

namespace App.Models;

public enum MemberType
{
    Free = 0,
    Standard = 1,
    Premium = 2,
}

public enum QualityImage
{
    Low = 1,
    Medium = 2,
    High = 3
}

public class MembershipDetailsModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    public MemberType MembershipType { get; set; }

    public int Price { get; set; }

    public int StorageLimitMB { get; set; }

    public bool HasAds { get; set; }

    public int MaxImageCount { get; set; }

    public QualityImage QualityImage { get; set; }

    public int PrioritySupport { get; set; }

    // All Features
    public bool ImageGeneration { get; set; }
    public bool ResolutionEnhancement { get; set; }
    public bool Unblur { get; set; }
    public bool ObjectRemoval { get; set; }
    public bool BackgroundBlur { get; set; }
    public bool ColorEnhancement { get; set; }
    public bool Denoise { get; set; }

    public virtual ICollection<MembershipsModel>? Memberships { get; set; }
}