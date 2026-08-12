using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CTPlugins
{
    /// <summary>
    /// ToolStrip renderer that turns consecutive items sharing the same non-null string
    /// <see cref="ToolStripItem.Tag"/> into a section: a rotated label is drawn in a
    /// reserved left column spanning the group's full height, in place of a plain menu row.
    /// Plugins opt in by tagging the items they return from <c>GetMenuItems()</c> with
    /// their display name and giving those items a left <see cref="ToolStripItem.Margin"/>
    /// of <see cref="DefaultLabelWidth"/> to make room for the label column.
    /// </summary>
    public class GroupLabelRenderer : ToolStripProfessionalRenderer
    {
        public const int DefaultLabelWidth = 20;

        public int LabelWidth { get; set; } = DefaultLabelWidth;
        public Font LabelFont { get; set; } = new Font("Segoe UI", 9f, FontStyle.Regular);
        public Color LabelColor { get; set; } = Color.DimGray;
        public Color LabelBackColor { get; set; } = Color.FromArgb(235, 235, 235);

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            base.OnRenderToolStripBackground(e);

            ToolStripItem groupFirst = null;
            ToolStripItem groupLast = null;
            string groupLabel = null;

            foreach (ToolStripItem item in e.ToolStrip.Items)
            {
                if (!item.Visible) continue;

                string label = item.Tag as string;
                if (label != groupLabel)
                {
                    DrawGroup(e.Graphics, groupLabel, groupFirst, groupLast);
                    groupLabel = label;
                    groupFirst = item;
                }
                groupLast = item;
            }
            DrawGroup(e.Graphics, groupLabel, groupFirst, groupLast);
        }

        private void DrawGroup(Graphics g, string label, ToolStripItem first, ToolStripItem last)
        {
            if (string.IsNullOrEmpty(label) || first == null || last == null)
                return;

            Rectangle band = new Rectangle(0, first.Bounds.Top, LabelWidth, last.Bounds.Bottom - first.Bounds.Top);

            using (var backBrush = new SolidBrush(LabelBackColor))
                g.FillRectangle(backBrush, band);

            // Shrink the font when the group is too short to fit the label at full size
            // (e.g. a single item), rather than letting the rotated text spill into the next group.
            const float minFontSize = 6f;
            const float verticalPadding = 8f;
            const float safetyFactor = 0.85f;
            Font font = LabelFont;
            SizeF size = g.MeasureString(label, font);
            float availableHeight = band.Height - verticalPadding;
            if (size.Width > availableHeight && availableHeight > minFontSize)
            {
                float fittedSize = System.Math.Max(minFontSize, font.Size * (availableHeight / size.Width) * safetyFactor);
                font = new Font(font.FontFamily, fittedSize, font.Style);
                size = g.MeasureString(label, font);
            }

            Region oldClip = g.Clip;
            g.SetClip(band);

            GraphicsState state = g.Save();
            g.TranslateTransform(band.Left + band.Width / 2f, band.Top + band.Height / 2f);
            g.RotateTransform(-90);
            using (var textBrush = new SolidBrush(LabelColor))
            {
                g.DrawString(label, font, textBrush, -size.Width / 2f, -size.Height / 2f);
            }
            g.Restore(state);

            g.Clip = oldClip;

            if (!ReferenceEquals(font, LabelFont))
                font.Dispose();
        }
    }
}
