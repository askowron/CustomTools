using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CTPlugins
{
    /// <summary>
    /// Thin wrapper over the Windows APIs that give WinForms controls their current system look.
    /// Every call is a no-op where the running Windows version doesn't support it.
    /// </summary>
    public static class NativeTheme
    {
        private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const int Windows11Build = 22000;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUNDSMALL = 3;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string subAppName, string subIdList);

        /// <summary>Relies on the host's app.manifest: without it Windows reports an older build.</summary>
        public static bool SupportsRoundedCorners
        {
            get { return Environment.OSVersion.Version.Build >= Windows11Build; }
        }

        /// <summary>Reads the current "app mode" setting, so a change applies without a restart.</summary>
        public static bool AppsUseDarkTheme
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(PersonalizeKey))
                {
                    return key?.GetValue("AppsUseLightTheme") is int useLight && useLight == 0;
                }
            }
        }

        /// <summary>Asks the window manager for the small rounded corners Windows 11 gives menus.</summary>
        public static void UseRoundedCorners(Control control)
        {
            if (!SupportsRoundedCorners)
                return;

            WhenHandleExists(control, handle =>
            {
                int preference = DWMWCP_ROUNDSMALL;
                DwmSetWindowAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
            });
        }

        /// <summary>Gives a TreeView or ListView File Explorer's arrows, hover and selection.</summary>
        public static void UseExplorerStyle(Control control)
        {
            WhenHandleExists(control, handle => SetWindowTheme(handle, "Explorer", null));
        }

        // HandleCreated also covers WinForms recreating the native window later, which would
        // otherwise silently drop anything applied to the old handle.
        private static void WhenHandleExists(Control control, Action<IntPtr> apply)
        {
            if (control.IsHandleCreated)
                apply(control.Handle);

            control.HandleCreated += (s, e) => apply(control.Handle);
        }
    }
}
