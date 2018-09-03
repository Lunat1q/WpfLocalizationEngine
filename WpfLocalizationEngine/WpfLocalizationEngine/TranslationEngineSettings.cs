namespace WpfLocalizationEngine
{
    public class TranslationEngineSettings
    {
        public string EncryptionKey { get; set; }

        public bool WithEncryption { get; set; }

        public string Extension { get; set; } = ".uiLanguage";

        public string Folder { get; set; } = "Config\\UILanguages";
    }
}