#nullable disable

namespace App.Areas.Contact.Models;

public class ContactView
{
    public int Id { get; set; }
    public string Name { get; set ; }
    public string Title { get; set; }
    public int Status { get; set; }
    public DateTime DateSend { get; set; }
    public int? Priority { get; set; }
    public string UserId { get; set; }
    public string AvatarPath { get; set; }
}

public class IndexViewModel
{

    //Pagging
    public int totalContacts { get; set; }
    public int countPages { get; set; }

    public int ITEMS_PER_PAGE { get; set; } = 20;

    public int currentPage { get; set; }

    public List<ContactView> Contacts { get; set; }
}