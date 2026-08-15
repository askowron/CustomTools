using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CTPlugins
{
    /// <summary>
    /// ToolStrip renderer that turns consecutive items sharing the same non-null string
    /// <see cref="ToolStripItem.Tag"/> into a section: a rotated label is drawn in a
    /// reserved left column spanning the group's full height, in place of a plain menu row.
    /// The reserved column is WinForms' own image margin (<c>ContextMenuStrip.ShowImageMargin
    /// = true</c>), not a custom <c>Padding</c>/<c>Margin</c> value: <see cref="ToolStripDropDownMenu"/>
    /// recalculates and overwrites its own <c>Padding</c> on every layout pass (every menu
    /// open, since items get rebuilt each time), so anything set on <c>Padding</c> directly
    /// is silently discarded before the menu is shown. The image margin's width, by contrast,
    /// is a native input to that same layout calculation, so it survives.
    /// <see cref="OnRenderImageMargin"/> captures the real, framework-computed bounds of that
    /// column each paint and suppresses its default Office-style gradient. The label itself is
    /// drawn twice by design: once for the whole group in <see cref="OnRenderToolStripBackground"/>
    /// (covers separators, which never repaint themselves individually), and again per row in
    /// <see cref="OnRenderMenuItemBackground"/> immediately after that item's own highlight —
    /// <c>ToolStripMenuItem.OnPaint</c> calls <c>DrawMenuItemBackground</c> unconditionally on
    /// every repaint of that item (hover included), which paints over the whole row including
    /// the label column, so the label has to be redrawn on top of it every time to survive.
    /// </summary>
    public class GroupLabelRenderer : ToolStripProfessionalRenderer
    {
        public const int DefaultLabelWidth = 20;

        public Font LabelFont { get; set; } = new Font("Segoe UI", 9f, FontStyle.Regular);
        public Color LabelColor { get; set; } = Color.DimGray;
        public Color LabelBackColor { get; set; } = Color.FromArgb(235, 235, 235);

        // Fallback for the very first paint, before OnRenderImageMargin has ever run once
        // to report the framework's real, DPI-scaled margin bounds.
        private Rectangle _marginBounds = new Rectangle(0, 0, DefaultLabelWidth, 0);

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            _marginBounds = e.AffectedBounds;
            // Deliberately do not call base: suppress the default gradient fill so this
            // column stays blank except where the label is painted over it.
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            string label = e.Item.Tag as string;
            if (string.IsNullOrEmpty(label))
            {
                base.OnRenderMenuItemBackground(e);
                return;
            }

            // Let the base renderer draw its full-width highlight/selection border first — a
            // proper closed rectangle. Clipping it away from the label column (an earlier
            // attempt) discarded the border's left edge line entirely instead of moving it, so
            // the box came out open on the left. Painting the label on top afterward covers that
            // portion cleanly, then a short line re-closes the border at the new boundary.
            base.OnRenderMenuItemBackground(e);

            // e.Graphics is translated so (0,0) is THIS ITEM's own top-left corner — not
            // toolstrip-relative like ToolStripItem.Bounds (confirmed by the framework's own
            // OnRenderMenuItemBackground, which builds its fill rect as
            // "new Rectangle(Point.Empty, item.Size)"). Every rectangle handed to this
            // Graphics must be in that local space, not e.Item.Bounds' absolute coordinates.
            Rectangle localBounds = new Rectangle(Point.Empty, e.Item.Size);
            int marginWidth = System.Math.Min(_marginBounds.Width, localBounds.Width);
            Rectangle marginSlice = new Rectangle(0, 0, marginWidth, localBounds.Height);

            // FindGroupBand returns toolstrip-absolute Y coordinates (from ToolStripItem.Bounds
            // of the group's first/last items) — convert to this item's local space by
            // subtracting its own absolute top before handing it to DrawLabel alongside the
            // local marginSlice clip.
            Rectangle groupBand = FindGroupBand(e.ToolStrip, e.Item, label);
            Rectangle localBand = new Rectangle(0, groupBand.Top - e.Item.Bounds.Top, marginWidth, groupBand.Height);
            DrawLabel(e.Graphics, label, localBand, marginSlice);

            if (e.Item.Selected)
            {
                using (var pen = new Pen(ColorTable.MenuItemBorder))
                {
                    e.Graphics.DrawLine(pen, marginSlice.Right, 0, marginSlice.Right, localBounds.Height - 1);
                }
            }
        }

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
                    DrawFullGroup(e.Graphics, groupLabel, groupFirst, groupLast);
                    groupLabel = label;
                    groupFirst = item;
                }
                groupLast = item;
            }
            DrawFullGroup(e.Graphics, groupLabel, groupFirst, groupLast);
        }

        /// <summary>
        /// Finds the contiguous run of visible items sharing <paramref name="item"/>'s tag and
        /// returns the vertical span (in the margin column) that its rotated label spans, so a
        /// single row can redraw its own slice positioned identically to the full-group draw.
        /// </summary>
        private static Rectangle FindGroupBand(ToolStrip toolStrip, ToolStripItem item, string label)
        {
            ToolStripItem first = null;
            ToolStripItem last = null;
            bool inGroup = false;

            foreach (ToolStripItem candidate in toolStrip.Items)
            {
                if (!candidate.Visible) continue;

                if ((candidate.Tag as string) == label)
                {
                    if (first == null) first = candidate;
                    last = candidate;
                    if (ReferenceEquals(candidate, item)) inGroup = true;
                }
                else if (inGroup)
                {
                    break;
                }
                else
                {
                    first = null;
                    last = null;
                }
            }

            return (first == null || last == null)
                ? item.Bounds
                : new Rectangle(0, first.Bounds.Top, 0, last.Bounds.Bottom - first.Bounds.Top);
        }

        private void DrawFullGroup(Graphics g, string label, ToolStripItem first, ToolStripItem last)
        {
            if (string.IsNullOrEmpty(label) || first == null || last == null)
                return;

            Rectangle band = new Rectangle(_marginBounds.X, first.Bounds.Top, _marginBounds.Width, last.Bounds.Bottom - first.Bounds.Top);
            DrawLabel(g, label, band, band);
        }

        /// <summary>
        /// Paints the label column background and the rotated text for one group, clipped to
        /// <paramref name="clip"/>. <paramref name="band"/> is the group's full vertical span —
        /// used to center/rotate the text identically regardless of how much of it is actually
        /// visible through <paramref name="clip"/>, so a single row's slice lines up seamlessly
        /// with its neighbors' slices.
        /// </summary>
        private void DrawLabel(Graphics g, string label, Rectangle band, Rectangle clip)
        {
            if (string.IsNullOrEmpty(label) || band.Height <= 0)
                return;

            // band's X/Width come straight from the caller (already matching its own clip
            // rectangle exactly) rather than being re-derived from _marginBounds here — the two
            // callers use different coordinate spaces (toolstrip-absolute vs. item-local), and
            // re-deriving from a single shared field previously let the two drift by a pixel,
            // showing up as a hairline seam between the label and the hover highlight next to it.

            Region oldClip = g.Clip;
            g.SetClip(clip);

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
