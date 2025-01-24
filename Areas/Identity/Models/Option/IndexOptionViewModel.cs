// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using App.Models;

namespace App.Areas.Identity.Models.OptionViewModels
{
    public class IndexOptionViewModel
    {
        public IList<UserLoginInfo> CurrentLogins { get; set; }

        public IList<AuthenticationScheme> OtherLogins { get; set; }

        public bool TwoFactor { get; set; }

        public RemoveLoginViewModel RemoveLoginViewModel { get; set; }

        public List<LoggedBrowsersModel> LoggedBrowsers { get; set; }

        public DeleteAccountViewmodel DeleteAccountViewmodel { get; set; }
    }
}
