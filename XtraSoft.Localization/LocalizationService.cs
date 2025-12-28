using System.Text.RegularExpressions;
using System.Xml.Linq;
using XtraSoft.Core;
using XtraSoft.Infrastructure;

namespace XtraSoft.Localization;

public class LocalizationService : ILocalizationService
{
    private readonly ApplicationDbContext _context;

    public LocalizationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public StringResource GetStringResource(string resourceKey, int languageId)
    {

        var key = resourceKey.Trim();
        return _context.StringResources.FirstOrDefault(x =>
            x.Name == key && x.LanguageId == languageId)!;

        //return _context.StringResources.FirstOrDefault(x =>
        //        x.Name!.Trim().ToLower() == resourceKey.Trim().ToLower()
        //        && x.LanguageId == languageId)!;
    }

    public void AddOrUpdateStringResource(StringResource resource)
    {
        lock (_context)
        {
            _context.Add(resource);
            _context.SaveChanges();
        }
    }
}
