using System.Drawing;
using System.Windows.Forms;

namespace CTPlugins
{
    /// <summary>
    /// Small caption above one plugin's items. It is a menu item rather than a ToolStripLabel so
    /// its text lines up with the rows below: only menu items use the menu's column layout.
    /// </summary>
    public class MenuSectionHeader : ToolStripMenuItem
    {
        private const float CaptionFontSize = 8f;

        public MenuSectionHeader(string text) : base(text)
        {
            Enabled = false;
            Font = new Font(Font.FontFamily, CaptionFontSize, FontStyle.Regular);
        }

        // Keeps keyboard navigation and hover from stopping on a caption.
        public override bool CanSelect
        {
            get { return false; }
        }
    }
}
