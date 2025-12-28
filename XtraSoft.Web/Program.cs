using IptvWebProject.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using System.Net;
using XtraSoft.Identity;
using XtraSoft.Infrastructure;
using XtraSoft.Infrastructure.Middlewares;
using XtraSoft.Localization;
using XtraSoft.Services;
using XtraSoft.Web.Filters;
using XtraSoft.Web.Hubs;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.MySQL(
        connectionString: "Server=localhost;Database=xtrasoft_logs;User=CHANGE_ME_DB_USER;Password=CHANGE_ME_DB_PASSWORD;",
        tableName: "Logs",
        restrictedToMinimumLevel: LogEventLevel.Information
    )
    .WriteTo.File(path: "logs/log-.txt",
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: LogEventLevel.Information
    ).CreateLogger();

var builder = WebApplication.CreateBuilder(args);



try
{

    // Register separated modules
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplicationServices(builder.Configuration);
    builder.Services.AddXtraLocalization(builder.Configuration);
    builder.Services.AddXtraIdentity(builder.Configuration);

    builder.Services.AddHttpContextAccessor();

    // Add session services
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    builder.Services.AddControllersWithViews(options =>
    {
        options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());
        options.Filters.Add(new MobileLayoutFilter());
    })
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

    builder.Services.AddRazorPages(options =>
    {
        options.Conventions.ConfigureFilter(new MobileLayoutPageFilter());
    })
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();
    
    builder.Services.AddServerSideBlazor();


    builder.Services.AddLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();
    });

    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 104857600; // 100MB
    });

    

    builder.Services.AddScoped<IVerificationCodeRepository, VerificationCodeRepository>();
    builder.Services.AddScoped<SecureVerificationService>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
        RequireHeaderSymmetry = false,
        ForwardLimit = null,
        KnownProxies = { IPAddress.Parse("127.0.0.1") } // or your Apache proxy IP
    });

    app.UseHttpsRedirection();

    var locOptions = ((IApplicationBuilder)app).ApplicationServices.GetRequiredService<IOptions<RequestLocalizationOptions>>();
    app.UseRequestLocalization(locOptions.Value);

    app.UseStaticFiles();
    app.UseSession();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseMiddleware<LocalizationMiddleware>();
    app.UseMiddleware<VisitorLoggingMiddleware>();

    app.MapAreaControllerRoute(
        name: "admin",
        areaName: "Admin",
        pattern: "Admin/{controller=Home}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    app.MapRazorPages();
    app.MapBlazorHub();
    app.MapHub<ChatHub>("/chathub");

    try
    {
        if (app.Environment.IsDevelopment())
        {
            Log.Information("dev env");
            await SeedLanguageAsync(app);
            app.SeedData();
            //await SeedLanguageAsync(app);
            app.Run();
        }
        else
        {
            //await SeedLanguageAsync(app);
            //app.SeedData();
            Log.Information("prod env");
            app.Run();
        }
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Application start-up failed 1");
        throw; // Re-throw the exception to ensure it is not swallowed
    }

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed 2");
    throw; // Re-throw the exception to ensure it is not swallowed
}
finally
{
    Log.CloseAndFlush();

}


static async Task SeedLanguageAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();

    var service = scope.ServiceProvider.GetRequiredService<SeedLanguage>();
    await service.EnsureSeedLanguageAsync();
}