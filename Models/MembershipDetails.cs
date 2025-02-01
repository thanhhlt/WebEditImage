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

    [Range(0, 100)]
    public int DiscountRate { get; set; }

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

    public int CalculateDiscountedPrice(int durationInMonths)
    {
        int basePrice = Price * durationInMonths;

        if (durationInMonths >= 24)
            DiscountRate = 40;
        else if (durationInMonths >= 12)
            DiscountRate = 30;
        else if (durationInMonths >= 6)
            DiscountRate = 20;
        else if (durationInMonths >= 3)
            DiscountRate = 10;
        else
            DiscountRate = 0;

        return basePrice - (basePrice * DiscountRate / 100);
    }
}