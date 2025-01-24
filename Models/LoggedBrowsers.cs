using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Models;

public class LoggedBrowsersModel 
{
    [Key]
    public int Id { get; set; }

    public string? BrowserInfo { get; set; }

    public string? IpAddress { get; set; }

    [Required]
    public DateTimeOffset LoginTime { get; set; }

    [ForeignKey(nameof(User))]
    public required string UserId { get; set; }

    public virtual AppUser? User { get; set; }
}