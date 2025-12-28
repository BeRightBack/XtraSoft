using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XtraSoft.Core;
using XtraSoft.Infrastructure;

namespace XtraSoft.Identity
{
    public static class IdentityServiceCollectionExtensions
    {
        public static IServiceCollection AddXtraIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddIdentity<UserProfile, UserRole>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredUniqueChars = 6;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(2);
                options.Lockout.MaxFailedAccessAttempts = 3;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // Add the 'Admin' authorization policy
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
            });
            // Ensure HttpContext is available for scoped services that depend on it
            //services.AddHttpContextAccessor();

            return services;
        }
    }
}
