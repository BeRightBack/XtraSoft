using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace XtraSoft.Services
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register domain services
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<ITranslationService, DeepLTranslationService>();
            services.AddScoped<IIpGeolocationService, IpGeolocationService>();
            services.AddScoped(typeof(IGenericService<,>), typeof(GenericService<,>));
            services.AddScoped<BookService>();

            // Other cross-cutting services
            services.AddMemoryCache();

            return services;
        }
    }

    // Minimal interfaces/impls for compilation
    

    public interface ITranslationService { Task<string> TranslateAsync(string text, string target); }
    public class DeepLTranslationService : ITranslationService { public Task<string> TranslateAsync(string t, string target) => Task.FromResult(t); }

    public interface IIpGeolocationService { Task<string> LookupCountryAsync(string ip); }
    public class IpGeolocationService : IIpGeolocationService { public Task<string> LookupCountryAsync(string ip) => Task.FromResult("Unknown"); }
}
