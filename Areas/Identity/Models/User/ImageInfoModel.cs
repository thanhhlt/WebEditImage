#nullable disable

using App.Models;

namespace App.Areas.Identity.Models.UserViewModels;

public class ImageInfoModel
{
    public int Id { get; set; }
    public string ImageUrl { get; set; }
    public string ThumbUrl { get; set; }
    public ActionEdit ActionTaken { get; set; }
    public DateTime EditedAt { get; set; }
    public long ImageKBSize { get; set; }
}