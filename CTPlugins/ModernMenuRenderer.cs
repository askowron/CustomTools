using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CTPlugins
{
    /// <summary>
    /// Flat ToolStrip renderer matching Windows 11 menus, in the light or dark app theme. Each
    /// contiguous run of items sharing a string Tag is one plugin's section, drawn as a card with
    /// an accent bar. The left image-margin column stays reserved but blank:
    /// <c>ToolStripDropDownMenu</c> recalculates and overwrites its own <c>Padding</c> on every
    /// layout pass and shrinks auto-sized items by their <c>Margin</c>, so that column is the only
    /// text indent that survives.
    /// </summary>
    public class ModernMenuRenderer : ToolStripProfessionalRenderer
    {
        private const int HoverInsetX = 4;
        private const int HoverInsetY = 1;
        private const int HoverRadius = 4;
        private const int SeparatorInset = 8;
        private const int SectionInsetX = 4;
        private const int SectionRadius = 6;
        private const int SectionBarInsetX = 3;
        private const int SectionBarInsetY = 6;
        private const int SectionBarWidth = 3;
        private const int SectionBarRadius = 1;
        private const int SectionContentLeft = 11;
        private const int SectionContentRight = 8;
        private const int ChevronFontDivisor = 5;
        private const int MinChevronHalfHeight = 3;
        private const float ChevronPenWidth = 1.2f;

        private ThemePalette _palette;

        public ModernMenuRenderer()
        {
            UpdateTheme();
        }

        /// <summary>Re-reads the system theme; call before each open so a change applies live.</summary>
        public void UpdateTheme()
        {
            _palette = ThemePalette.Current;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(_palette.MenuBackground))
                e.Graphics.FillRectangle(brush, e.AffectedBounds);

            ToolStripItem first = null;
            ToolStripItem last = null;
            string currentSection = null;

            foreach (ToolStripItem item in e.ToolStrip.Items)
            {
                if (!item.Available)
                    continue;

                string section = SectionOf(item);
                if (section != currentSection)
                {
                    DrawSection(e.Graphics, e.AffectedBounds.Width, first, last);
                    currentSection = section;
                    first = section == null ? null : item;
                }
                last = item;
            }
            DrawSection(e.Graphics, e.AffectedBounds.Width, first, last);
        }

        private void DrawSection(Graphics g, int stripWidth, ToolStripItem first, ToolStripItem last)
        {
            if (first == null)
                return;

            Rectangle card = new Rectangle(SectionInsetX, first.Bounds.Top, stripWidth - SectionInsetX * 2, last.Bounds.Bottom - first.Bounds.Top);
            Rectangle bar = new Rectangle(card.Left + SectionBarInsetX, card.Top + SectionBarInsetY, SectionBarWidth, card.Height - SectionBarInsetY * 2);

            SmoothingMode previous = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = MenuGeometry.RoundedRectangle(card, SectionRadius))
            using (var brush = new SolidBrush(_palette.MenuSection))
            {
                g.FillPath(brush, path);
            }
            using (GraphicsPath path = MenuGeometry.RoundedRectangle(bar, SectionBarRadius))
            using (var brush = new SolidBrush(_palette.AccentText))
            {
                g.FillPath(brush, path);
            }
            g.SmoothingMode = previous;
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // On Windows 11 the window manager draws the rounded outline itself.
            if (NativeTheme.SupportsRoundedCorners)
                return;

            using (var pen = new Pen(_palette.MenuBorder))
                e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            // Deliberately empty: skips the base gradient so the column shows the card behind it.
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Enabled || !(e.Item.Selected || e.Item.Pressed))
                return;

            // e.Graphics is translated so (0,0) is this item's own top-left corner.
            bool inSection = SectionOf(e.Item) != null;
            int left = inSection ? SectionContentLeft : HoverInsetX;
            int right = inSection ? SectionContentRight : HoverInsetX;
            Rectangle hover = Rectangle.FromLTRB(left, HoverInsetY, e.Item.Width - right, e.Item.Height - HoverInsetY);

            SmoothingMode previous = e.Graphics.SmoothingMode;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = MenuGeometry.RoundedRectangle(hover, HoverRadius))
            using (var brush = new SolidBrush(inSection ? _palette.MenuSectionHover : _palette.MenuHover))
            {
                e.Graphics.FillPath(brush, path);
            }
            e.Graphics.SmoothingMode = previous;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            Color color = e.Item is MenuSectionHeader ? _palette.AccentText
                : e.Item.Enabled ? _palette.Text
                : _palette.DisabledText;
            TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, e.TextRectangle, color, e.TextFormat);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            bool inSection = SectionOf(e.Item) != null;
            int left = inSection ? SectionContentLeft : SeparatorInset;
            int y = e.Item.Height / 2;
            Color color = inSection ? _palette.MenuSectionHover : _palette.MenuSeparator;
            using (var pen = new Pen(color))
                e.Graphics.DrawLine(pen, left, y, e.Item.Width - SeparatorInset, y);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            Rectangle area = e.ArrowRectangle;
            int centerX = area.Left + area.Width / 2;
            int centerY = area.Top + area.Height / 2;
            int half = System.Math.Max(MinChevronHalfHeight, e.Item.Font.Height / ChevronFontDivisor);

            Point[] chevron =
            {
                new Point(centerX - half / 2, centerY - half),
                new Point(centerX + half / 2 + 1, centerY),
                new Point(centerX - half / 2, centerY + half),
            };

            SmoothingMode previous = e.Graphics.SmoothingMode;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(e.Item.Enabled ? _palette.Text : _palette.DisabledText, ChevronPenWidth))
            {
                e.Graphics.DrawLines(pen, chevron);
            }
            e.Graphics.SmoothingMode = previous;
        }

        private static string SectionOf(ToolStripItem item)
        {
            string section = item.Tag as string;
            return string.IsNullOrEmpty(section) ? null : section;
        }
    }
}
