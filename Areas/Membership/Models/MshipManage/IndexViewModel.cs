#nullable disable

using App.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Areas.Membership.Models.MshipManage;

public class MembershipDetails : MembershipDetailsModel
{
    public string[] MembershipFeatures { get; set; }
}

public class IndexViewModel
{
    public List<MembershipDetails> MembershipDetails { get; set; }
    public SelectList AllFeatures { get; set; }
}