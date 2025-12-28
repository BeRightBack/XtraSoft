using System.ComponentModel.DataAnnotations;
using XtraSoft.Core;

namespace XtraSoft.Web.Areas.Admin.Models.UserViewModels
{
    public class UserViewModel
    {
        public string? Id { get; set; }

        [Display(Name = "UserName")]
        public string? UserName { get; set; }

        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [Display(Name = "Email")]
        public string? Email { get; set; }
        public bool EmailConfirmed { get; set; } = false;

        [Display(Name = "Password")]
        public string? Password { get; set; }

        [Phone]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        public bool PhoneNumberConfirmed { get; set; } = false;

    }

    public class UserIndexViewModel
    {
        public IEnumerable<UserProfile> Users { get; set; } = [];
        public string SearchQuery { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
    }
}
