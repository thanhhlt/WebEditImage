using Microsoft.AspNetCore.Authorization;

namespace App.Security.Requirements;

public class ImageQuotaRequirement : IAuthorizationRequirement
{
    public ImageQuotaRequirement() {}
}