using App.Models;

namespace App.Areas.ImageTools.Models.ManageIMG;

public class ImageView
{
    public int Id { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string ThumbPath { get; set; } = string.Empty;
    public ActionEdit ActionTaken { get; set; }
}
public class IndexViewModel
{
    //Pagging
    public int totalImages { get; set; }
    public int countPages { get; set; }

    public int ITEMS_PER_PAGE { get; set; } = 24;

    public int currentPage { get; set; }

    public ActionEdit? FilterAction { get; set; }
    public List<ImageView> Images { get; set; } = new List<ImageView>();
}