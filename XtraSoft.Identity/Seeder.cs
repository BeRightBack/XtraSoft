using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XtraSoft.Core;
using XtraSoft.Infrastructure;

namespace XtraSoft.Identity
{
    public static class Seeder
    {

        public static void SeedData(this IApplicationBuilder app) => SeedStore(app).GetAwaiter().GetResult();

        private static byte[] GetDefaultProfilePicture()
        {
            try
            {
                // Load the default image from a file or resource
                // For example, loading from a file:
                string filePath = "wwwroot/images/default-user.png";
                return File.ReadAllBytes(filePath);
            }
            catch (FileNotFoundException)
            {
                // Handle the case where the file is not found
                // Return an empty byte array or a default image byte array
                return Array.Empty<byte>();
            }
        }

        private static async Task SeedStore(IApplicationBuilder app)
        {

            using (var scope = app.ApplicationServices.CreateScope())
            {
                var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
                //context!.Database.Migrate();

                IConfiguration config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                UserManager<UserProfile> userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserProfile>>();
                RoleManager<UserRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<UserRole>>();

                string roleName = config["Administrator:Role"] ?? "Admin";
                string userName = config["Administrator:User"] ?? "email@example.com";
                string password = config["Administrator:Password"] ?? "CHANGE_ME_ADMIN_PASSWORD";

                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new UserRole(roleName));
                }
                UserProfile? dashboardUser = await userManager.FindByEmailAsync(userName);
                if (dashboardUser == null)
                {
                    dashboardUser = new UserProfile
                    {
                        UserName = userName,
                        Email = userName,
                        EmailConfirmed = true,
                        ProfilePicture = GetDefaultProfilePicture(),
                        FirstName = "Admin",
                        LastName = "Admin",
                        PhoneNumber = "1234567890",
                        PhoneNumberConfirmed = true,
                        ProfilePicturePath = "/images/default-user.png"

                    };
                    await userManager.CreateAsync(dashboardUser);
                    dashboardUser = await userManager.FindByEmailAsync(userName);
                    await userManager.AddPasswordAsync(dashboardUser!, password);
                }
                if (!await userManager.IsInRoleAsync(dashboardUser!, roleName))
                {
                    await userManager.AddToRoleAsync(dashboardUser!, roleName);
                }
            }
        }
    }
}
