using CTPlugins;
using CustomTools.Core.Extensions;
using CustomTools.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomTools
{
    internal static class Program
    {
        // Held for the whole process lifetime purely so the Setup.exe installer's
        // AppMutex check can detect a running instance and close/relaunch it during
        // an update. Stored as a static field (rather than a local variable kept
        // alive with GC.KeepAlive) so it stays rooted for as long as the process
        // runs and is never eligible for collection during the (potentially
        // multi-day) Application.Run() call below.
        private static Mutex appMutex;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            appMutex = new Mutex(initiallyOwned: false, "CustomToolsSingleInstance");

            LanguageManager.Apply(LanguageManager.GetSavedLanguageCode());

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Create the tray icon
            NotifyIcon trayIcon = new NotifyIcon();
            trayIcon.Icon = new System.Drawing.Icon(new MemoryStream(Resources.favicon));
            trayIcon.Visible = true;
            trayIcon.Text = "Custom Tools v0.3";
            trayIcon.BalloonTipClicked += (s, e) => new FrmUpdateAvailable().Show();

            // Find plugins once, but rebuild the menu contents on every open, so changes
            // plugins persist (e.g. in the registry) show up immediately.
            List<ICTPlugin> plugins = CTPlugins.CTPlugins.FindPlugins();
            ContextMenuStrip menu = new ContextMenuStrip();
            // ShowImageMargin=true reserves WinForms' native left image-margin column across
            // the whole menu, which GroupLabelRenderer paints over for grouped items and
            // leaves blank otherwise (no plugin sets ToolStripItem.Image). This column is the
            // only reservation mechanism ToolStripDropDownMenu doesn't undo on its own: it
            // recalculates and overwrites ContextMenuStrip.Padding on every layout pass, and it
            // shrinks each auto-sized item's own width by ToolStripItem.Margin — both were tried
            // and both get silently fought by the framework.
            menu.ShowImageMargin = true;
            menu.Renderer = new GroupLabelRenderer();
            menu.Opening += (s, e) => RebuildMenu(menu, plugins, trayIcon);
            RebuildMenu(menu, plugins, trayIcon);

            // Attach the menu to the icon
            trayIcon.ContextMenuStrip = menu;

            // We can also react to a left-click
            trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    menu.Show(Cursor.Position); // shows the menu at the cursor
                }
            };

            UpdateChecker.StartBackgroundChecking(trayIcon);

            // Run the application loop with no window
            Application.Run();
        }

        private static void RebuildMenu(ContextMenuStrip menu, List<ICTPlugin> plugins, NotifyIcon trayIcon)
        {
            menu.Items.Clear();
            foreach (ICTPlugin plugin in plugins)
            {
                menu.Items.AddRange(plugin.GetMenuItems());
            }
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Strings.TrayMenu_Options, null, (s, e) =>
            {
                using (FrmOptions form = new FrmOptions(trayIcon))
                {
                    form.ShowDialog();
                }
            });
            menu.Items.Add(Strings.TrayMenu_About, null, (s, e) =>
            {
                using (FrmAbout form = new FrmAbout())
                {
                    form.ShowDialog();
                }
            });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Strings.TrayMenu_Exit, null, (s, e) => Application.Exit());
        }
    }
}
