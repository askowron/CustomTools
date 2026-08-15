using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace CTRegistryTree
{
    /// <summary>
    /// Resolves a 16x16 shell icon per <see cref="RegistryTreeItem.ActionType"/>, using the Windows
    /// shell's extension/attribute-based icon lookup (no disk access, no bundled image assets).
    /// Results are cached after first use.
    /// </summary>
    internal static class RegistryTreeIcons
    {
        private static readonly Dictionary<RegistryTreeItem.ActionType, Image> cache = new Dictionary<RegistryTreeItem.ActionType, Image>();

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_SMALLICON = 0x000000001;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static Image GetImage(RegistryTreeItem.ActionType action)
        {
            Image image;
            if (!cache.TryGetValue(action, out image))
            {
                image = ResolveIcon(action);
                cache[action] = image;
            }
            return image;
        }

        private static Image ResolveIcon(RegistryTreeItem.ActionType action)
        {
            switch (action)
            {
                case RegistryTreeItem.ActionType.RunCommand:
                    return GetShellIcon("dummy.exe", FILE_ATTRIBUTE_NORMAL);
                case RegistryTreeItem.ActionType.OpenUrl:
                    return GetShellIcon("dummy.url", FILE_ATTRIBUTE_NORMAL);
                case RegistryTreeItem.ActionType.OpenFile:
                    return GetShellIcon("dummy", FILE_ATTRIBUTE_NORMAL);
                case RegistryTreeItem.ActionType.Submenu:
                    return GetShellIcon("folder", FILE_ATTRIBUTE_DIRECTORY);
                default:
                    return null;
            }
        }

        private static Image GetShellIcon(string fakePath, uint fileAttributes)
        {
            SHFILEINFO shfi = new SHFILEINFO();
            IntPtr result = SHGetFileInfo(fakePath, fileAttributes, ref shfi, (uint)Marshal.SizeOf(shfi), SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES);

            if (result == IntPtr.Zero || shfi.hIcon == IntPtr.Zero)
                return null;

            try
            {
                using (Icon icon = Icon.FromHandle(shfi.hIcon))
                {
                    return icon.ToBitmap();
                }
            }
            finally
            {
                DestroyIcon(shfi.hIcon);
            }
        }
    }
}
