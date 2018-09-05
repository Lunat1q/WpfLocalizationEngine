using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace WpfLocalizationEngine
{
    internal class TranslationEngineImplementation
    {
        private static InterfaceTranslation _alterTranslation;
        private static bool _withEncryption;
        private readonly string _fileExtension;
        private readonly string _translationsFolder;

        public readonly List<string> LanguageList = new List<string>();
        private byte[] _entropy;

        internal TranslationEngineImplementation(string translationExtension, string translationsFolder)
        {
            _fileExtension = translationExtension;
            _translationsFolder = translationsFolder;
        }

        private string Folder => Path.Combine(GetCurrentPath(), _translationsFolder);

        public string CurrentTranslationLanguage { get; set; }

        private static string GetCurrentPath()
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly()?.FullName ?? Directory.GetCurrentDirectory());
        }

        internal TranslationEngineImplementation WithEncryption(bool withEncryption = false, string encryptionKey = "")
        {
            _withEncryption = withEncryption;
            _entropy = Encoding.UTF8.GetBytes(encryptionKey);
            return this;
        }

        public void Initialize()
        {
            if (!Directory.Exists(Folder))
                Directory.CreateDirectory(Folder);

            foreach (var item in Directory.GetFiles(Folder))
            {
                if (!CheckForUnprotectedTranslation(item) || !item.Contains(translationExtension)) continue;
                var fi = new FileInfo(item);
                LanguageList.Add(fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length));
            }
            if (!LanguageList.Contains("English"))
            {
                var englishUi = new InterfaceTranslation
                {
                    LanguageName = "English",
                    Translation = GetTranslationTags(GetAllWindows())
                };
                if (_withEncryption)
                    englishUi.CryptData(Path.Combine(Folder, "English" + _fileExtension), _entropy);
                else
                    englishUi.SerializeDataJson(Path.Combine(Folder, "English" + _fileExtension));
                LanguageList.Add("English");
            }

#if DEBUG
            CurrentTranslationLanguage = "EMPTY";
#endif
        }

        private static IEnumerable<Window> GetAllWindows()
        {
            var windowType = typeof(Window);
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var t in a.GetTypes())
                if (windowType.IsAssignableFrom(t) && t.GetCustomAttribute<TranslatableAttribute>() != null)
                    yield return Activator.CreateInstance(t) as Window;
        }

        private bool CheckForUnprotectedTranslation(string path)
        {
            if (!_withEncryption)
                return true;

            var fi = new FileInfo(path);
            var uiLang = TranslationSerializer.DeserializeDataJson<InterfaceTranslation>(path);
            if (uiLang == null) return true;
            var langPath = path.Substring(0, path.Length - fi.Extension.Length) + _fileExtension;
            if (!File.Exists(langPath))
            {
#if DEBUG
                uiLang.CryptData(langPath, _entropy);
                LanguageList.Add(fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length));
#else
                File.Delete(path);
#endif
            }
            return true;
        }

        private void SaveCurrent()
        {
            var langPath = Path.Combine(Folder, CurrentTranslationLanguage + ".json");
            _alterTranslation.SerializeDataJson(langPath);
        }

        public string GetDynamicTranslationString(string tag, string defaultText)
        {
            if (_alterTranslation == null) return defaultText;
            if (_alterTranslation.Translation.ContainsKey(tag))
                return _alterTranslation.Translation[tag];
#if DEBUG
            _alterTranslation.Translation.Add(tag, defaultText);
            SaveCurrent();
#endif
            return defaultText;
        }

        public bool SetLanguage(string languageName)
        {
            if (languageName == CurrentTranslationLanguage || languageName == null) return false;
            if (!LanguageList.Contains(languageName)) return false;
            var lng = TranslationSerializer.DecryptData<InterfaceTranslation>(
                Path.Combine(Folder, languageName + _fileExtension), _entropy);
            if (lng == null) return false;
            _alterTranslation = lng;
            CurrentTranslationLanguage = languageName;
            return true;
        }

        public void ApplyLanguage(UIElement page)
        {
            if (_alterTranslation == null) return;
            foreach (var uiElem in page.GetLogicalChildCollection<CheckBox>())
            {
                var s = uiElem.Tag as string;
                if (s == null) continue;
                if (_alterTranslation.Translation.ContainsKey(s))
                    uiElem.Content = _alterTranslation.Translation[s];
            }

            foreach (var uiElem in page.GetLogicalChildCollection<Label>())
            {
                var s = uiElem.Tag as string;
                if (s == null) continue;
                if (_alterTranslation.Translation.ContainsKey(s))
                    uiElem.Content = _alterTranslation.Translation[s];
            }

            foreach (var uiElem in page.GetLogicalChildCollection<TabItem>())
            {
                var s = uiElem.Tag as string;
                if (s == null) continue;
                if (_alterTranslation.Translation.ContainsKey(s))
                    uiElem.Header = _alterTranslation.Translation[s];
            }

            foreach (var uiElem in page.GetLogicalChildCollection<GroupBox>())
            {
                var s = uiElem.Tag as string;
                if (s == null) continue;
                if (_alterTranslation.Translation.ContainsKey(s))
                    uiElem.Header = _alterTranslation.Translation[s];
            }

            foreach (var uiElem in page.GetLogicalChildCollection<Button>())
            {
                var s = uiElem.Tag as string;
                if (s == null) continue;
                if (_alterTranslation.Translation.ContainsKey(s))
                    uiElem.Content = _alterTranslation.Translation[s];
            }

            foreach (var uiElem in page.GetLogicalChildCollection<TextBlock>())
            {
                var s = uiElem.Tag as string;
                if (s == null) continue;
                if (_alterTranslation.Translation.ContainsKey(s))
                    uiElem.Text = _alterTranslation.Translation[s];
            }

            foreach (var uiElem in page.GetLogicalChildCollection<TextBox>())
            {
                var s = uiElem.Tag as string;
                if (s == null) continue;
                if (_alterTranslation.Translation.ContainsKey(s))
                    uiElem.Text = _alterTranslation.Translation[s];
            }

#if DEBUG
            var dict = GetTranslationTags(new[] {page});
            foreach (var kp in dict)
                if (!_alterTranslation.Translation.ContainsKey(kp.Key))
                    _alterTranslation.Translation.Add(kp.Key, kp.Value);
            SaveCurrent();
#endif
        }

        private static Dictionary<string, string> GetTranslationTags(IEnumerable<UIElement> pages)
        {
            var tagDict = new Dictionary<string, string>();
            foreach (var page in pages)
            {
                foreach (var uiElem in page.GetLogicalChildCollection<CheckBox>())
                {
                    var s = uiElem.Tag as string;
                    if (s == null) continue;
                    if (!tagDict.ContainsKey(s))
                        tagDict.Add(s, uiElem.Content as string);
                }

                foreach (var uiElem in page.GetLogicalChildCollection<Label>())
                {
                    var s = uiElem.Tag as string;
                    if (s == null) continue;
                    if (!tagDict.ContainsKey(s))
                        tagDict.Add(s, uiElem.Content as string);
                }

                foreach (var uiElem in page.GetLogicalChildCollection<TabItem>())
                {
                    var s = uiElem.Tag as string;
                    if (s == null) continue;
                    if (!tagDict.ContainsKey(s))
                        tagDict.Add(s, uiElem.Header as string);
                }

                foreach (var uiElem in page.GetLogicalChildCollection<Button>())
                {
                    var s = uiElem.Tag as string;
                    if (s == null) continue;
                    if (!tagDict.ContainsKey(s))
                        tagDict.Add(s, uiElem.Content as string);
                }

                foreach (var uiElem in page.GetLogicalChildCollection<GroupBox>())
                {
                    var s = uiElem.Tag as string;
                    if (s == null) continue;
                    if (!tagDict.ContainsKey(s))
                        tagDict.Add(s, uiElem.Header as string);
                }

                foreach (var uiElem in page.GetLogicalChildCollection<TextBlock>())
                {
                    var s = uiElem.Tag as string;
                    if (s == null) continue;
                    if (!tagDict.ContainsKey(s))
                        tagDict.Add(s, uiElem.Text);
                }

                foreach (var uiElem in page.GetLogicalChildCollection<TextBox>())
                {
                    var s = uiElem.Tag as string;
                    if (s == null) continue;
                    if (!tagDict.ContainsKey(s))
                        tagDict.Add(s, uiElem.Text);
                }
            }
            return tagDict;
        }
    }
}