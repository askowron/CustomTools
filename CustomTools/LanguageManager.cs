using CustomTools.Properties;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace CustomTools
{
    internal static class LanguageManager
    {
        private const string RootKeyPath = @"SOFTWARE\Appit\CustomTools";
        private const string ValueName = "Language";

        public struct Language
        {
            public string Code;
            public string DisplayName;

            public Language(string code, string displayName)
            {
                Code = code;
                DisplayName = displayName;
            }

            public override string ToString()
            {
                return DisplayName;
            }
        }

        public static IEnumerable<Language> GetSupportedLanguages()
        {
            yield return new Language("", Strings.Options_Language_SystemDefault);
            yield return new Language("en", "English");
            yield return new Language("pl", "Polski");
            yield return new Language("de", "Deutsch");
            yield return new Language("es", "Español");
            yield return new Language("it", "Italiano");
        }

        public static string GetSavedLanguageCode()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RootKeyPath, writable: false))
            {
                return key?.GetValue(ValueName) as string ?? "";
            }
        }

        public static void SetSavedLanguageCode(string code)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RootKeyPath))
            {
                key.SetValue(ValueName, code ?? "");
            }
        }

        public static void Apply(string code)
        {
            CultureInfo culture = string.IsNullOrEmpty(code)
                ? CultureInfo.InstalledUICulture
                : new CultureInfo(code);

            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
    }
}
