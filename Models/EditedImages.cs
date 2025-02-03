using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace App.Models;

public enum ActionEdit
{
    None = 0,
    ImageGeneration = 1,
    ResolutionEnht = 2,
    Unblur = 3,
    ObjectRemoval = 4,
    BackgroundBlur = 5,
    ColorEnht = 6,
    Denoise = 7
}

public class EditedImagesModel
{
    [Key]
    public int Id { get; set; }

    [Required]    
    public required string ImagePath { get; set; }

    public ActionEdit ActionTaken { get; set; }

    public DateTime? EditedAt { get; set; }

    public long ImageKBSize { get; set; }

    [ForeignKey(nameof(User))]
    [Required]
    public required string UserId { get; set;}

    public virtual AppUser? User { get; set; }

    [NotMapped]
    public string ImageUrl => ImagePath.Replace("Images/", "imgs/");
    [NotMapped]
    public string ThumbUrl
    {
        get
        {
            string newPath = ImagePath.Replace("Images/", "imgs/");
            int lastSlashIndex = newPath.LastIndexOf('/'); 

            return newPath.Insert(lastSlashIndex + 1, "Thumbnails/");
        }
    }
}