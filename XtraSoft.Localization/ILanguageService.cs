
using XtraSoft.Core;

namespace XtraSoft.Localization;

public interface ILanguageService
{
    IEnumerable<Language> GetLanguages();
    Language GetLanguageByCulture(string culture);
}
