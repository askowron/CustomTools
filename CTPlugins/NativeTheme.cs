using Microsoft.Win32;
using System;
using System.Drawing;
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
        private const string DwmKey = @"Software\Microsoft\Windows\DWM";
        private const int Windows11Build = 22000;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;
        private const int DWMWCP_ROUNDSMALL = 3;

        private static readonly Color DefaultAccent = Color.FromArgb(0, 103, 192);

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

        /// <summary>The accent color chosen in Windows settings.</summary>
        public static Color AccentColor
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(DwmKey))
                {
                    // Stored as 0xAABBGGRR.
                    if (key?.GetValue("AccentColor") is int abgr)
                        return Color.FromArgb(abgr & 0xFF, (abgr >> 8) & 0xFF, (abgr >> 16) & 0xFF);

                    return DefaultAccent;
                }
            }
        }

        /// <summary>Asks the window manager for the small rounded corners Windows 11 gives menus.</summary>
        public static void UseRoundedCorners(Control control)
        {
            if (!SupportsRoundedCorners)
                return;

            WhenHandleExists(control, handle => SetAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, DWMWCP_ROUNDSMALL));
        }

        /// <summary>
        /// Colors the title bar to match the window, whatever accent setting Windows applies to
        /// title bars. The explicit colors need Windows 11; earlier versions only get dark mode.
        /// </summary>
        public static void UseTitleBarColors(Form form, Color caption, Color text, bool dark)
        {
            WhenHandleExists(form, handle =>
            {
                SetAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, dark ? 1 : 0);
                SetAttribute(handle, DWMWA_CAPTION_COLOR, ColorTranslator.ToWin32(caption));
                SetAttribute(handle, DWMWA_TEXT_COLOR, ColorTranslator.ToWin32(text));
            });
        }

        /// <summary>Draws a native control with a named system theme, such as "Explorer".</summary>
        public static void UseWindowTheme(Control control, string themeName)
        {
            WhenHandleExists(control, handle => SetWindowTheme(handle, themeName, null));
        }

        private static void SetAttribute(IntPtr handle, int attribute, int value)
        {
            DwmSetWindowAttribute(handle, attribute, ref value, sizeof(int));
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
