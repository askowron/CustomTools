using System.Drawing;
using System.Windows.Forms;

namespace CTPlugins
{
    /// <summary>
    /// Plugin name at the top of its section. It is a menu item rather than a ToolStripLabel so
    /// its text lines up with the rows below: only menu items use the menu's column layout. It
    /// carries the section's Tag so the renderer draws it as part of the section's card.
    /// </summary>
    public class MenuSectionHeader : ToolStripMenuItem
    {
        private const float CaptionFontSize = 8.25f;

        public MenuSectionHeader(string text) : base(text)
        {
            Tag = text;
            Enabled = false;
            Font = new Font(Font.FontFamily, CaptionFontSize, FontStyle.Bold);
        }

        // Keeps keyboard navigation and hover from stopping on a caption.
        public override bool CanSelect
        {
            get { return false; }
        }
    }
}
