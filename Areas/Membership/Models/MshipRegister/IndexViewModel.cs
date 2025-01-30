#nullable disable


using App.Models;

namespace App.Areas.Membership.Models.MshipRegister;

public class IndexViewModel
{
    public List<MembershipDetailsModel> MembershipDetails { get; set; }
    public MemberType? MemberType { get; set; } 
}