using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Models;

public enum Membership
{
        Standard = 1,
        Premium = 2,
        Professional = 3
}

public class MembershipsModel
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(User))]
    [Required]
    public required string UserId { get; set; }

    public Membership MembershipType { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndTime { get; set; }

    public required virtual AppUser User { get; set; }
}