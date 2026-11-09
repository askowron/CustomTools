using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace CTPlugins
{
    /// <summary>
    /// Restyles a finished form to follow the system theme. It works on standard controls in
    /// place, so forms and plugin dialogs keep using plain WinForms controls in their designers.
    /// </summary>
    public static class DialogTheme
    {
        private const string DarkInputTheme = "DarkMode_CFD";
        private const string DarkScrollingTheme = "DarkMode_Explorer";
        private const string LightTheme = "Explorer";
        private const int ButtonRadius = 4;
        private const int GroupRadius = 6;
        private const int GroupCaptionLeft = 10;
        private const int GroupCaptionPadding = 4;
        private const int RichTextIndent = 12;
        private const int GlyphTextGap = 3;
        private const double AccentHoverMix = 0.12;
        private const double AccentPressedMix = 0.2;

        public static void Apply(Form form)
        {
            Apply(form, ThemePalette.Current);
        }

        public static void Apply(Form form, ThemePalette palette)
        {
            form.BackColor = palette.Surface;
            form.ForeColor = palette.Text;
            NativeTheme.UseTitleBarColors(form, palette.Surface, palette.Text, palette.IsDark);
            ApplyToChildren(form, form, palette);
        }

        /// <summary>
        /// RTF text keeps its own colors and indents, so a dark background needs them replaced;
        /// call this again after assigning new content.
        /// </summary>
        public static void StyleRichText(RichTextBox box, ThemePalette palette)
        {
            box.SelectAll();
            box.SelectionColor = palette.Text;
            box.SelectionIndent = RichTextIndent;
            box.SelectionRightIndent = RichTextIndent;
            box.Select(0, 0);
        }

        private static void ApplyToChildren(Control parent, Form form, ThemePalette palette)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is Button button)
                {
                    StyleButton(button, form, palette);
                }
                else if (control is LinkLabel link)
                {
                    link.LinkColor = palette.AccentText;
                    link.ActiveLinkColor = palette.AccentText;
                    link.VisitedLinkColor = palette.AccentText;
                    link.LinkBehavior = LinkBehavior.HoverUnderline;
                }
                else if (control is TextBoxBase || control is ComboBox || control is TreeView)
                {
                    StyleInput(control, palette);
                }
                else if (control is GroupBox group)
                {
                    group.Paint += (s, e) => PaintGroupBox(group, palette, e.Graphics);
                }
                else if (control is CheckBox || control is RadioButton)
                {
                    control.Paint += (s, e) => RepaintDisabledText((ButtonBase)control, palette, e.Graphics);
                }

                ApplyToChildren(control, form, palette);
            }
        }

        private static void StyleInput(Control control, ThemePalette palette)
        {
            control.BackColor = palette.InputFill;
            control.ForeColor = palette.Text;

            bool usesExplorerTheme = control is TreeView || control is RichTextBox;
            string darkTheme = usesExplorerTheme ? DarkScrollingTheme : DarkInputTheme;
            NativeTheme.UseWindowTheme(control, palette.IsDark ? darkTheme : LightTheme);

            if (control is TreeView tree)
            {
                tree.LineColor = palette.Border;
                tree.BorderStyle = BorderStyle.None;
                DrawOutline(tree, palette);
            }
            else if (control is RichTextBox richText)
            {
                richText.BorderStyle = BorderStyle.None;
                StyleRichText(richText, palette);
            }
        }

        /// <summary>The native border ignores the theme, so the parent draws one around the control.</summary>
        private static void DrawOutline(Control control, ThemePalette palette)
        {
            Control parent = control.Parent;
            parent.Resize += (s, e) => parent.Invalidate();
            parent.Paint += (s, e) =>
            {
                Rectangle outline = Rectangle.Inflate(control.Bounds, 1, 1);
                outline.Width--;
                outline.Height--;
                using (var pen = new Pen(palette.Border))
                    e.Graphics.DrawRectangle(pen, outline);
            };
        }

        private static void PaintGroupBox(GroupBox group, ThemePalette palette, Graphics g)
        {
            Size caption = TextRenderer.MeasureText(group.Text, group.Font);
            int top = caption.Height / 2;
            Rectangle border = Rectangle.FromLTRB(0, top, group.Width - 1, group.Height - 1);
            Rectangle gap = new Rectangle(GroupCaptionLeft - GroupCaptionPadding, 0, caption.Width + GroupCaptionPadding * 2, caption.Height);

            g.Clear(group.Parent.BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = MenuGeometry.RoundedRectangle(border, GroupRadius))
            using (var pen = new Pen(palette.Border))
            {
                g.DrawPath(pen, path);
            }

            using (var brush = new SolidBrush(group.Parent.BackColor))
                g.FillRectangle(brush, gap);

            TextRenderer.DrawText(g, group.Text, group.Font, new Point(GroupCaptionLeft, 0), palette.MutedText);
        }

        /// <summary>WinForms etches disabled check and radio text, which is unreadable on dark.</summary>
        private static void RepaintDisabledText(ButtonBase control, ThemePalette palette, Graphics g)
        {
            if (control.Enabled)
                return;

            int textLeft = RadioButtonRenderer.GetGlyphSize(g, RadioButtonState.UncheckedDisabled).Width + GlyphTextGap;
            Rectangle area = Rectangle.FromLTRB(textLeft, 0, control.Width, control.Height);
            using (var brush = new SolidBrush(control.Parent.BackColor))
                g.FillRectangle(brush, area);

            TextRenderer.DrawText(g, control.Text, control.Font, area, palette.DisabledText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        private static void StyleButton(Button button, Form form, ThemePalette palette)
        {
            // Flat buttons already repaint on hover and press, so only Paint needs replacing.
            button.UseVisualStyleBackColor = false;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Paint += (s, e) => PaintButton(button, ReferenceEquals(form.AcceptButton, button), palette, e.Graphics);
        }

        private static void PaintButton(Button button, bool primary, ThemePalette palette, Graphics g)
        {
            bool hover = button.ClientRectangle.Contains(button.PointToClient(Cursor.Position));
            bool pressed = hover && button.Capture && Control.MouseButtons == MouseButtons.Left;

            g.Clear(button.Parent.BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle bounds = new Rectangle(0, 0, button.Width - 1, button.Height - 1);
            bool accent = primary && button.Enabled;
            Color fill = accent ? AccentFill(palette, hover, pressed) : ControlFill(palette, button.Enabled, hover, pressed);

            using (GraphicsPath path = MenuGeometry.RoundedRectangle(bounds, ButtonRadius))
            {
                using (var brush = new SolidBrush(fill))
                    g.FillPath(brush, path);

                if (!accent)
                {
                    Color outline = button.Focused ? palette.AccentText : palette.Border;
                    using (var pen = new Pen(outline))
                        g.DrawPath(pen, path);
                }
            }

            Color text = !button.Enabled ? palette.DisabledText : accent ? palette.OnAccent : palette.Text;
            TextRenderer.DrawText(g, button.Text, button.Font, bounds, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }

        private static Color AccentFill(ThemePalette palette, bool hover, bool pressed)
        {
            if (pressed)
                return ThemePalette.Mix(palette.Accent, Color.Black, AccentPressedMix);

            return hover ? ThemePalette.Mix(palette.Accent, Color.White, AccentHoverMix) : palette.Accent;
        }

        private static Color ControlFill(ThemePalette palette, bool enabled, bool hover, bool pressed)
        {
            if (!enabled)
                return palette.Surface;

            return pressed ? palette.ControlPressed : hover ? palette.ControlHover : palette.ControlFill;
        }
    }
}
