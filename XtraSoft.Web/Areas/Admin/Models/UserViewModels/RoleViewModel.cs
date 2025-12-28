using System.ComponentModel.DataAnnotations;
using XtraSoft.Core;

namespace XtraSoft.Web.Areas.Admin.Models.UserViewModels
{
    public class RoleViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role Description")]
        public string Description { get; set; } = string.Empty;

        public IEnumerable<UserProfile> Members { get; set; } = new List<UserProfile>();
        public IEnumerable<UserProfile> NonMembers { get; set; } = new List<UserProfile>();
        public string[]? IdsToAdd { get; set; }
        public string[]? IdsToDelete { get; set; }
    }

    
}
