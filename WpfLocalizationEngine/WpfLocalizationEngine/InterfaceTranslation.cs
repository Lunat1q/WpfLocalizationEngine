using System.Collections.Generic;

namespace WpfLocalizationEngine
{
    public class InterfaceTranslation
    {
        public string LanguageName { get; set; } = "EnglishDefault";
        public Dictionary<string, string> Translation { get; set; } = new Dictionary<string, string>();
    }
}