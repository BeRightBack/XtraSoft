using Microsoft.Extensions.Localization;

namespace XtraSoft.Localization
{
    public static class LocalizationContext
    {
        public static IStringLocalizer<SharedResource>? Localizer { get; set; }
    }
}
