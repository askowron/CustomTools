using Microsoft.Win32;
using System.Windows.Forms;

namespace CustomTools
{
    internal static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "CustomTools";

        public static bool IsEnabled
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false))
                {
                    if (key == null)
                        return false;

                    return key.GetValue(ValueName) as string == ExePathValue;
                }
            }
        }

        public static void SetEnabled(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
            {
                if (enabled)
                    key.SetValue(ValueName, ExePathValue);
                else
                    key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }

        private static string ExePathValue
        {
            get { return "\"" + Application.ExecutablePath + "\""; }
        }
    }
}
