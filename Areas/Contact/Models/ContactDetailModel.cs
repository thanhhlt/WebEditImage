#nullable disable

namespace App.Areas.Contact.Models;

public class ContactDetailModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string Response { get; set; }
    public int Status { get; set; }
    public DateTime DateSend { get; set; }
    public int? Priority { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string AvatarPath { get; set; }
}