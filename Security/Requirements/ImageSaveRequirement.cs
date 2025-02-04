using Microsoft.AspNetCore.Authorization;

namespace App.Security.Requirements;

public class ImageSaveRequirement : IAuthorizationRequirement
{
    public ImageSaveRequirement() {}
}