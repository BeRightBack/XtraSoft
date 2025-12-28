using DeepL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using XtraSoft.Infrastructure;
using XtraSoft.Localization;
using XtraSoft.Services;
using XtraSoft.Web.Models.HomeViewModels;

namespace XtraSoft.Web.Controllers
{
    public class HomeController : BaseController
    {

        //private readonly UserManager<ApplicationUser> userManager;
        //private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context, ILanguageService languageService,
         ILocalizationService localizationService,
         IOptions<RequestLocalizationOptions> localizationOptions,
         Translator translator,
         IConfiguration configuration,
         //SignInManager<ApplicationUser> signInManager,
         //UserManager<ApplicationUser> userManager,
         IEmailSender emailSender,
         UrlEncoder urlEncoder) : base(context,
             languageService,
             localizationService,
             localizationOptions,
             translator,
             configuration,
             emailSender)
        {
            //this.userManager = userManager;
            //this.signInManager = signInManager;
            _emailSender = emailSender;
            _context = context;
        }

        [TempData]
        public string? ErrorMessage { get; set; }


        [TempData]
        public string? StatusMessage { get; set; }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Faq()
        {
            ViewData["Title"] = @Localize("FAQ - Quebec Iptv");
            ViewData["Description"] = @Localize("Frequently asked questions about our IPTV service.");
            ViewData["Keywords"] = "IPTV, Quebec, Streaming, Live Channels, Online TV, VOD, Canada TV";

            return View();
        }
        [Authorize]
        public IActionResult Contact()
        {
            ViewData["Title"] = @Localize("Contact - Quebec Iptv");
            ViewData["Description"] = @Localize("Get in touch with us.");
            ViewData["Keywords"] = "IPTV, Quebec, Streaming, Live Channels, Online TV, VOD, Canada TV";
            ViewData["Message"] = @LocString("Your contact page.");

            var model = new ContactViewModel
            {
                Name = string.Empty,
                Email = string.Empty,
                Subject = string.Empty,
                Message = string.Empty,
                CaptchaCode = string.Empty // Initialize required property
            };

            // Generate a CAPTCHA code and store it in the session
            var random = new Random();
            var captchaCode = random.Next(1000, 9999).ToString(); // Generate a 4-digit random number
            HttpContext.Session.SetString("CaptchaCode", captchaCode);

            // Optionally, pass the CAPTCHA code to the view for rendering (e.g., as an image or text)
            ViewData["CaptchaCode"] = captchaCode;

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Contact(ContactViewModel model)
        {
            ViewData["Title"] = @Localize("Contact - Quebec Iptv");
            ViewData["Description"] = @Localize("Get in touch with us.");
            ViewData["Keywords"] = "IPTV, Quebec, Streaming, Live Channels, Online TV, VOD, Canada TV";
            ViewData["Message"] = @LocString("Your contact page.");

            // Validate CAPTCHA
            var captchaCode = HttpContext.Session.GetString("CaptchaCode");
            if (string.IsNullOrEmpty(captchaCode) || captchaCode != model.CaptchaCode)
            {
                var errorMessage = @Localize("Invalid CAPTCHA code.").ToString(); // Convert HtmlString to string
                ModelState.AddModelError("CaptchaCode", errorMessage);
            }

            if (ModelState.IsValid)
            {
                // Process the contact form (e.g., send an email)
                _emailSender.SendFromContactAsync(model.Name, model.Email, model.Subject, model.Message);

                TempData["StatusMessage"] = @Localize("Your message has been sent successfully.").ToString();
                return RedirectToAction(nameof(Contact));
            }

            // If validation fails, redisplay the form
            return View(model);
        }


        public IActionResult Privacy()
        {
            ViewData["Title"] = @Localize("Privacy Policy - Quebec Iptv");
            ViewData["Description"] = @Localize("Our privacy policy.");
            ViewData["Keywords"] = "IPTV, Quebec, Streaming, Live Channels, Online TV, VOD, Canada TV";

            return View();
        }

        public IActionResult Test()
        {
            return View();
        }


    }
}
