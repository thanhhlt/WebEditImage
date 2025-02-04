using Microsoft.AspNetCore.Authorization;

namespace App.Security.Requirements;

public class FeatureAccessRequirement : IAuthorizationRequirement
{
    public FeatureAccessRequirement() {}
}