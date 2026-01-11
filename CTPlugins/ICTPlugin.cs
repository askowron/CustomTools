using System.Windows.Forms;

namespace CTPlugins
{
    public interface ICTPlugin
    {
        string Name { get; }
        ToolStripItem[] GetMenuItems();
    }
}
