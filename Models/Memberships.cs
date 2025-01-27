using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Models;

public class MembershipsModel
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(User))]
    [Required]
    public required string UserId { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    [ForeignKey(nameof(MembershipDetails))]
    [Required]
    public int MembershipDetailsId { get; set; }

    public virtual AppUser? User { get; set; }
    public virtual MembershipDetailsModel? MembershipDetails { get; set; }
}