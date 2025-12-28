using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using XtraSoft.Core;
using XtraSoft.Infrastructure;
using XtraSoft.Web.Areas.Admin.Models.UserViewModels;

namespace XtraSoft.Web.Areas.Admin.Controllers
{

    //[Authorize(Roles = "Admins")]
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RoleAdminController : Controller
    {
        private RoleManager<UserRole> roleManager;
        private UserManager<UserProfile> userManager;
        private readonly EfIRepository<UserRole> _roleRepository;

        public RoleAdminController(
         RoleManager<UserRole> roleMgr,
         UserManager<UserProfile> userMrg,
         EfIRepository<UserRole> roleRepository
         ) 
        {
            roleManager = roleMgr;
            userManager = userMrg;
            _roleRepository = roleRepository;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await roleManager.Roles.ToListAsync();
            var roleWithUsers = new List<RoleWithUsersViewModel>();

            var users = await userManager.Users.ToListAsync(); // Load users into memory

            foreach (var role in roles)
            {
                var usersInRole = new List<UserProfile>();
                foreach (var user in users)
                {
                    if (await userManager.IsInRoleAsync(user, role.Name!))
                    {
                        usersInRole.Add(user);
                    }
                }

                roleWithUsers.Add(new RoleWithUsersViewModel
                {
                    Role = role,
                    Users = usersInRole
                });
            }

            return View(roleWithUsers);
        }

        public async Task<IActionResult> Create(string searchQuery = "", int pageNumber = 1, int pageSize = 10)
        {
            var usersQuery = userManager.Users
                .Where(u => string.IsNullOrEmpty(searchQuery) || u.UserName!.Contains(searchQuery));

            var totalUsers = await usersQuery.CountAsync();
            var users = await usersQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new RoleViewModel
            {
                NonMembers = users
            };

            ViewData["SearchQuery"] = searchQuery;
            ViewData["PageNumber"] = pageNumber;
            ViewData["TotalPages"] = (int)Math.Ceiling(totalUsers / (double)pageSize);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var role = new UserRole
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description
                };

                IdentityResult result = await roleManager.CreateAsync(role);
                if (!result.Succeeded)
                {
                    AddErrorsFromResult(result);
                    return View(viewModel);
                }

                foreach (string userId in viewModel.IdsToAdd ?? new string[] { })
                {
                    UserProfile? user = await userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        result = await userManager.AddToRoleAsync(user, role.Name);
                        if (!result.Succeeded)
                        {
                            AddErrorsFromResult(result);
                        }
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            UserRole? role = await roleManager.FindByIdAsync(id);
            if (role != null)
            {
                IdentityResult result = await roleManager.DeleteAsync(role);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    AddErrorsFromResult(result);
                }
            }
            else
            {
                ModelState.AddModelError("", "No role found");
            }
            return View("Index", roleManager.Roles);
        }

        public async Task<IActionResult> Edit(string id, string searchQuery = "", int pageNumber = 1, int pageSize = 10)
        {
            UserRole? role = await roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            var usersQuery = userManager.Users
                .Where(u => string.IsNullOrEmpty(searchQuery) || u.UserName!.Contains(searchQuery));

            var totalUsers = await usersQuery.CountAsync();
            var users = await usersQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<UserProfile> members = new List<UserProfile>();
            List<UserProfile> nonMembers = new List<UserProfile>();

            foreach (UserProfile user in users)
            {
                var list = await userManager.IsInRoleAsync(user, role.Name!)
                    ? members : nonMembers;
                list.Add(user);
            }

            var viewModel = new RoleViewModel
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description!,
                Members = members,
                NonMembers = nonMembers
            };

            ViewData["SearchQuery"] = searchQuery;
            ViewData["PageNumber"] = pageNumber;
            ViewData["TotalPages"] = (int)Math.Ceiling(totalUsers / (double)pageSize);
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(RoleViewModel model)
        {
            if (ModelState.IsValid)
            {
                UserRole? role = await roleManager.FindByIdAsync(model.Id);
                if (role == null)
                {
                    return NotFound();
                }

                role.Name = model.Name;
                role.Description = model.Description;

                IdentityResult result = await roleManager.UpdateAsync(role);
                if (!result.Succeeded)
                {
                    AddErrorsFromResult(result);
                    return View(model);
                }

                foreach (string userId in model.IdsToAdd ?? new string[] { })
                {
                    UserProfile? user = await userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        result = await userManager.AddToRoleAsync(user, model.Name);
                        if (!result.Succeeded)
                        {
                            AddErrorsFromResult(result);
                        }
                    }
                }

                foreach (string userId in model.IdsToDelete ?? new string[] { })
                {
                    UserProfile? user = await userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        result = await userManager.RemoveFromRoleAsync(user, model.Name);
                        if (!result.Succeeded)
                        {
                            AddErrorsFromResult(result);
                        }
                    }
                }

                if (ModelState.IsValid)
                {
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(model);
        }


        private void AddErrorsFromResult(IdentityResult result)
        {
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }
    }
}
