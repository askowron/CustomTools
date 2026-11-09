using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CTPlugins
{
    /// <summary>
    /// Flat ToolStrip renderer matching Windows 11 menus, in the light or dark app theme.
    /// The left image-margin column stays reserved but blank: <c>ToolStripDropDownMenu</c>
    /// recalculates and overwrites its own <c>Padding</c> on every layout pass and shrinks
    /// auto-sized items by their <c>Margin</c>, so that column is the only text indent that survives.
    /// </summary>
    public class ModernMenuRenderer : ToolStripProfessionalRenderer
    {
        private const int HoverInsetX = 4;
        private const int HoverInsetY = 1;
        private const int HoverRadius = 4;
        private const int SeparatorInset = 8;
        private const int ChevronFontDivisor = 5;
        private const int MinChevronHalfHeight = 3;
        private const float ChevronPenWidth = 1.2f;

        private static readonly Palette Light = new Palette
        {
            Background = Color.FromArgb(249, 249, 249),
            Hover = Color.FromArgb(236, 236, 236),
            Text = Color.FromArgb(26, 26, 26),
            DisabledText = Color.FromArgb(140, 140, 140),
            CaptionText = Color.FromArgb(96, 96, 96),
            Separator = Color.FromArgb(229, 229, 229),
            Border = Color.FromArgb(214, 214, 214),
        };

        private static readonly Palette Dark = new Palette
        {
            Background = Color.FromArgb(44, 44, 44),
            Hover = Color.FromArgb(61, 61, 61),
            Text = Color.FromArgb(255, 255, 255),
            DisabledText = Color.FromArgb(128, 128, 128),
            CaptionText = Color.FromArgb(171, 171, 171),
            Separator = Color.FromArgb(64, 64, 64),
            Border = Color.FromArgb(80, 80, 80),
        };

        private Palette _palette;

        public ModernMenuRenderer()
        {
            UpdateTheme();
        }

        /// <summary>Re-reads the system theme; call before each open so a change applies live.</summary>
        public void UpdateTheme()
        {
            _palette = NativeTheme.AppsUseDarkTheme ? Dark : Light;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(_palette.Background))
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // On Windows 11 the window manager draws the rounded outline itself.
            if (NativeTheme.SupportsRoundedCorners)
                return;

            using (var pen = new Pen(_palette.Border))
                e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            // Deliberately empty: skips the base gradient so the column matches the background.
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            // e.Graphics is translated so (0,0) is this item's own top-left corner.
            Rectangle row = new Rectangle(Point.Empty, e.Item.Size);
            using (var brush = new SolidBrush(_palette.Background))
                e.Graphics.FillRectangle(brush, row);

            if (!e.Item.Enabled || !(e.Item.Selected || e.Item.Pressed))
                return;

            Rectangle hover = Rectangle.Inflate(row, -HoverInsetX, -HoverInsetY);
            SmoothingMode previous = e.Graphics.SmoothingMode;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = RoundedRectangle(hover, HoverRadius))
            using (var brush = new SolidBrush(_palette.Hover))
            {
                e.Graphics.FillPath(brush, path);
            }
            e.Graphics.SmoothingMode = previous;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            Color color = e.Item is MenuSectionHeader ? _palette.CaptionText
                : e.Item.Enabled ? _palette.Text
                : _palette.DisabledText;
            TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, e.TextRectangle, color, e.TextFormat);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using (var pen = new Pen(_palette.Separator))
                e.Graphics.DrawLine(pen, SeparatorInset, y, e.Item.Width - SeparatorInset, y);
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

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private sealed class Palette
        {
            public Color Background;
            public Color Hover;
            public Color Text;
            public Color DisabledText;
            public Color CaptionText;
            public Color Separator;
            public Color Border;
        }
    }
}
