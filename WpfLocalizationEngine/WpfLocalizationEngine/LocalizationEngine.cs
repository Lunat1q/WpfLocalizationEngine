using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace WpfLocalizationEngine
{
    public static class TranslationEngine
    {
        private static TranslationEngineImplementation Instance { get; set; }


        public static IEnumerable<string> LanguageList => Instance.LanguageList;

        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged = delegate { };
        public static void RaiseStaticPropertyChanged([CallerMemberName] string propName = null)
        {
            StaticPropertyChanged(null, new PropertyChangedEventArgs(propName));
        }
        public static void RaiseStaticPropertyChangedByName(string propName = null)
        {
            StaticPropertyChanged(null, new PropertyChangedEventArgs(propName));
        }

        public static string CurrentTranslationLanguage
        {
            get { return Instance.CurrentTranslationLanguage; }
            set
            {
                if (Instance.CurrentTranslationLanguage == value) return;
                Instance.CurrentTranslationLanguage = value;
                RaiseStaticPropertyChanged();
            }
        }

        public static void SetUp(TranslationEngineSettings settings)
        {
            Instance = new TranslationEngineImplementation(settings.Extension, settings.Folder);
            if (settings.WithEncryption)
            {
                Instance.WithEncryption(true, settings.EncryptionKey);
            }

            Instance.Initialize();
        }

        public static string GetDynamicTranslationString(string tag, string defaultText) => Instance.GetDynamicTranslationString(tag, defaultText);

        public static void SetLanguage(string languageName)
        {
            if (Instance.SetLanguage(languageName))
            {
                RaiseStaticPropertyChangedByName("CurrentTranslationLanguage");
            }
        }

        public static void ApplyLanguage(UIElement page) => Instance.ApplyLanguage(page);
    }
}
