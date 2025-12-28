using DeepL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using System.Globalization;
using XtraSoft.Localization.Custom;

namespace XtraSoft.Localization
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddXtraLocalization(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ILocalizationService, LocalizationService>();
            services.AddScoped<ILanguageService, LanguageService>();
            services.AddScoped<IStringLocalizer<SharedResource>, DbStringLocalizer<SharedResource>>();

            services.AddScoped<Translator>(sp =>
            {
                var authKey = configuration["DeepLConfig:AuthKey"] ?? throw new InvalidOperationException("DeepLConfig:AuthKey configuration value not found.");
                return new Translator(authKey);
            });

            //services.AddLocalization(options => options.ResourcesPath = "Resources");

            //services.Configure<RequestLocalizationOptions>(opts =>
            //{
            //    var supported = new[] { new CultureInfo("en-US"), new CultureInfo("fr-FR"), new CultureInfo("it-IT") };
            //    opts.DefaultRequestCulture = new RequestCulture("en-US");

            //    // Set supported cultures directly (SetSupportedCultures extension wasn't resolved)
            //    opts.SupportedCultures = supported.ToList();
            //    opts.SupportedUICultures = supported.ToList();
            //});
            services.AddTransient<SeedLanguage>();

            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new List<CultureInfo>
        {
            new("en"),
            new("fr"),
            new("es"),
            new("it"),
            new("pt")
        };

                options.DefaultRequestCulture = new RequestCulture(culture: "fr", uiCulture: "fr");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;

                options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
            });

            services.AddScoped(typeof(IStringLocalizer<>), typeof(DbStringLocalizer<>));
            services.AddScoped<LocalizationHelper>();
            services.AddLocalization();

            return services;
        }
    }
}
