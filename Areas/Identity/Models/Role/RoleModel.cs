// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace App.Areas.Identity.Models.RoleViewModels
{
    public struct ClaimsRole
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string[] Claims { get; set; }
    }
    public class RoleModel
    {
        public List<ClaimsRole> ClaimsRoles { get; set; } = new List<ClaimsRole>();

        public CreateRoleModel CreateRoleModel { get; set; }

        public string IdRoleDelete { get; set; }
    }
}
