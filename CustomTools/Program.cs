using CTPlugins;
using CustomTools.Core.Extensions;
using CustomTools.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomTools
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Tworzymy ikonę w trayu
            NotifyIcon trayIcon = new NotifyIcon();
            trayIcon.Icon = new System.Drawing.Icon(new MemoryStream(Resources.favicon));
            trayIcon.Visible = true;
            trayIcon.Text = "Custom Tools";

            // Wyszukujemy pluginy raz, ale odbudowujemy zawartość menu przy każdym otwarciu,
            // żeby zmiany zapisane przez pluginy (np. w rejestrze) były od razu widoczne.
            List<ICTPlugin> plugins = CTPlugins.CTPlugins.FindPlugins();
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.ShowImageMargin = false;
            menu.Renderer = new GroupLabelRenderer();
            // Reserve room for GroupLabelRenderer's vertical group-label column on the container
            // itself (Padding), not per item (Margin) — ToolStripDropDownMenu's auto-sizing shrinks
            // each auto-sized item's own width by its Margin, which clips content that assumed the
            // full, unshrunk width (e.g. the submenu expand arrow). Padding grows the container
            // instead, so items keep their full correct width.
            menu.Padding = new Padding(GroupLabelRenderer.DefaultLabelWidth, menu.Padding.Top, menu.Padding.Right, menu.Padding.Bottom);
            menu.Opening += (s, e) => RebuildMenu(menu, plugins);
            RebuildMenu(menu, plugins);

            // Podpinamy menu do ikony
            trayIcon.ContextMenuStrip = menu;

            // Możemy też reagować na kliknięcie lewym przyciskiem
            trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    menu.Show(Cursor.Position); // pokazuje menu przy kursore
                }
            };

            // Uruchamiamy pętlę aplikacji bez okna
            Application.Run();
        }

        private static void RebuildMenu(ContextMenuStrip menu, List<ICTPlugin> plugins)
        {
            menu.Items.Clear();
            foreach (ICTPlugin plugin in plugins)
            {
                menu.Items.AddRange(plugin.GetMenuItems());
            }
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Strings.TrayMenu_Options, null, (s, e) => MessageBox.Show(Strings.TrayMenu_OptionsPlaceholder));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Strings.TrayMenu_Exit, null, (s, e) => Application.Exit());
        }
    }
}
