using DeepL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using XtraSoft.Infrastructure;
using XtraSoft.Localization;
using XtraSoft.Services;
using XtraSoft.Web.Areas.Admin.Models.VisitorViewModels;
using XtraSoft.Web.Controllers;
using XtraSoft.Web.Models.PagedList;

namespace XtraSoft.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class VisitorController : BaseController
    {
        private readonly ApplicationDbContext _context;
        public VisitorController(
            ApplicationDbContext context,
            ILanguageService languageService,
            ILocalizationService localizationService,
            IOptions<RequestLocalizationOptions> localizationOptions,
            Translator translator,
            IConfiguration configuration,
            IEmailSender emailSender)
            : base(context, languageService, localizationService, localizationOptions, translator, configuration, emailSender)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string visitsSearchString, string locationsSearchString, int? visitsPageNumber, int? locationsPageNumber)
        {
            var visitorLogs = _context.VisitorLogs.AsQueryable();

            var totalVisits = await visitorLogs.CountAsync();
            var uniqueVisitors = await visitorLogs.Select(v => v.IpAddress).Distinct().CountAsync();

            var visitsQuery = visitorLogs
                .GroupBy(v => v.VisitTime.Date)
                .Select(g => new VisitPerDay { Date = g.Key, Count = g.Count() })
                .OrderByDescending(v => v.Date)
                .AsQueryable();

            if (!string.IsNullOrEmpty(visitsSearchString))
            {
                visitsQuery = visitsQuery.Where(v => v.Date.ToString().Contains(visitsSearchString));
            }

            var visitsPerDay = await PaginatedList<VisitPerDay>.CreateAsync(visitsQuery, visitsPageNumber ?? 1, 10);

            var locationsQuery = visitorLogs
                .GroupBy(v => new { v.VisitTime.Date, v.Country, v.City, v.Region })
                .Select(g => new VisitorLocation
                {
                    Date = g.Key.Date,
                    Country = g.Key.Country,
                    City = g.Key.City,
                    Region = g.Key.Region,
                    Count = g.Count()
                })
                .OrderByDescending(v => v.Date)
                .AsQueryable();

            if (!string.IsNullOrEmpty(locationsSearchString))
            {
                locationsQuery = locationsQuery.Where(v => v.Date.ToString().Contains(locationsSearchString));
            }

            var visitorLocations = await PaginatedList<VisitorLocation>.CreateAsync(locationsQuery, locationsPageNumber ?? 1, 10);

            var model = new VisitorStatisticsViewModel
            {
                TotalVisits = totalVisits,
                UniqueVisitors = uniqueVisitors,
                VisitsPerDay = visitsPerDay,
                VisitorLocations = visitorLocations,
                VisitsSearchString = visitsSearchString,
                LocationsSearchString = locationsSearchString
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteVisits(DateTime date)
        {
            var visitsToDelete = _context.VisitorLogs.Where(v => v.VisitTime.Date == date);
            _context.VisitorLogs.RemoveRange(visitsToDelete);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteLocations(DateTime date)
        {
            var locationsToDelete = _context.VisitorLogs.Where(v => v.VisitTime.Date == date);
            _context.VisitorLogs.RemoveRange(locationsToDelete);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Roles()
        {
            return View();
        }
    }
}
