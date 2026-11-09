using System.Drawing;
using System.Windows.Forms;

namespace CTPlugins
{
    /// <summary>
    /// Applies the Windows 11 look to the tray menu. <see cref="Install"/> runs once;
    /// <see cref="Refresh"/> runs after every rebuild, because rebuilding replaces every item.
    /// </summary>
    public static class ModernMenu
    {
        private const float RowHeightPerFontHeight = 1.5f;

        private static Bitmap _rowSpacer;

        public static void Install(ContextMenuStrip menu)
        {
            var renderer = new ModernMenuRenderer();
            menu.Renderer = renderer;
            menu.ShowImageMargin = true;
            menu.Opening += (s, e) => renderer.UpdateTheme();
            NativeTheme.UseRoundedCorners(menu);
        }

        public static void Refresh(ContextMenuStrip menu)
        {
            InsertSectionHeaders(menu.Items);
            StyleItems(menu.Items, RowSpacer((int)(menu.Font.Height * RowHeightPerFontHeight)));
        }

        /// <summary>
        /// ToolStripDropDownMenu sizes every row from its tallest text or image and ignores the
        /// items' Padding, so a transparent image one pixel wide is what sets the row height
        /// without widening the image column.
        /// </summary>
        private static Bitmap RowSpacer(int height)
        {
            if (_rowSpacer == null || _rowSpacer.Height != height)
                _rowSpacer = new Bitmap(1, height);

            return _rowSpacer;
        }

        /// <summary>
        /// Adds a caption above each contiguous run of items sharing a string Tag, and a
        /// separator between two neighboring sections so their cards don't touch.
        /// </summary>
        private static void InsertSectionHeaders(ToolStripItemCollection items)
        {
            string currentSection = null;
            for (int i = 0; i < items.Count; i++)
            {
                string section = items[i].Tag as string;
                if (string.IsNullOrEmpty(section))
                    section = null;

                if (section != null && section != currentSection)
                {
                    if (currentSection != null)
                    {
                        items.Insert(i, new ToolStripSeparator());
                        i++;
                    }
                    items.Insert(i, new MenuSectionHeader(section));
                    i++;
                }
                currentSection = section;
            }
        }

        private static void StyleItems(ToolStripItemCollection items, Bitmap rowSpacer)
        {
            foreach (ToolStripItem item in items)
            {
                var menuItem = item as ToolStripMenuItem;
                if (menuItem == null)
                    continue;

                if (menuItem.Image == null)
                {
                    menuItem.Image = rowSpacer;
                    menuItem.ImageScaling = ToolStripItemImageScaling.None;
                }

                // HasDropDownItems first: reading DropDown would create one on every plain item.
                if (menuItem.HasDropDownItems)
                {
                    NativeTheme.UseRoundedCorners(menuItem.DropDown);
                    StyleItems(menuItem.DropDownItems, rowSpacer);
                }
            }
        }
    }
}
