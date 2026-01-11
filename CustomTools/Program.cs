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

            // Tworzymy dynamiczne menu
            ContextMenuStrip menu = new ContextMenuStrip();
            // Wyszukiwanie Pluginów
            foreach (ICTPlugin plugin in CTPlugins.CTPlugins.FindPlugins())
            {
                //menu.Items.Add("Opcja 1", null, (s, e) => MessageBox.Show("Kliknięto opcję 1"));
                //menu.Items.Add("Opcja 2", null, (s, e) => MessageBox.Show("Kliknięto opcję 2"));
                menu.Items.AddRange(plugin.GetMenuItems());
            }            
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Opcje", null, (s, e) => MessageBox.Show("Kliknięto opcję 2"));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Zamknij", null, (s, e) => Application.Exit());

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
    }
}
