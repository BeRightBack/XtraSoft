using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace XtraSoft.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var conn = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<ApplicationDbContext>(opts =>
            {
                opts.UseMySQL(conn!, mysql =>
                {
                    // Put migrations into the Infrastructure assembly
                    mysql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);

                    // Connection resiliency (relational)
                    mysql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
                });

                // Optional tuning
                // opts.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                // opts.CommandTimeout(30);
            });

            services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
            services.Configure<IpGeolocationOptions>(configuration.GetSection("IpGeolocation"));
            services.Configure<DeepLOptions>(configuration.GetSection("DeepLConfig"));

            services.AddScoped<IUnitOfWork, EfUnitOfWork>();
            services.AddScoped(typeof(EfIRepository<>), typeof(EfRepository<>));
            services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

            return services;
        }
    }
}
