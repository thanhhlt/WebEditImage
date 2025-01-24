#nullable disable

using System.ComponentModel.DataAnnotations;
using App.Models;

namespace App.Areas.Identity.Models.UserViewModels
{
    public class UserListModel
    {
        public int totalUsers { get; set; }
        public int countPages { get; set; }

        public int ITEMS_PER_PAGE { get; set; } = 10;

        public int currentPage { get; set; }

        public string SearchString { get; set; }

        public string MessageSearchResult { get; set; }

        public List<UserViewModel> users { get; set; }
    }

    public class UserViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public Membership MembershipType { get; set; }
    }
}