using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Models;

public class ContactsModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 5)]
    public string Name { get; set; } = "";

    [StringLength(255, MinimumLength = 5)]
    public string? Email { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = "";

    [Required]
    public string Content { get; set; } = "";

    public int Status { get; set; }

    public DateTime DateSend { get; set; } = DateTime.UtcNow;

    public int? Priority { get; set; }

    public string? Response { get; set; }

    [ForeignKey(nameof(User))]
    public string? UserId { get; set; }

    public virtual AppUser? User { get; set; }
}