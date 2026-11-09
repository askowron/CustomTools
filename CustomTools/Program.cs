using CTPlugins;
using CustomTools.Core.Extensions;
using CustomTools.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomTools
{
    internal static class Program
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int RegisterApplicationRestart(string commandLine, int flags);

        // RESTART_NO_CRASH | RESTART_NO_HANG | RESTART_NO_REBOOT: only relaunch when Restart
        // Manager shuts us down for a patch (i.e. the Inno Setup self-update).
        private const int RestartOnlyForPatch = 1 | 2 | 8;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Inno Setup's RestartApplications only relaunches apps that registered here; without
            // it the installer force-closes us during an update and never starts us again.
            RegisterApplicationRestart(null, RestartOnlyForPatch);

            LanguageManager.Apply(LanguageManager.GetSavedLanguageCode());

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Create the tray icon
            NotifyIcon trayIcon = new NotifyIcon();
            trayIcon.Icon = new System.Drawing.Icon(new MemoryStream(Resources.favicon), SystemInformation.SmallIconSize);
            trayIcon.Visible = true;
            trayIcon.Text = "Custom Tools v0.9";
            trayIcon.BalloonTipClicked += (s, e) => new FrmUpdateAvailable().Show();

            // Find plugins once, but rebuild the menu contents on every open, so changes
            // plugins persist (e.g. in the registry) show up immediately.
            List<ICTPlugin> plugins = CTPlugins.CTPlugins.FindPlugins();
            ContextMenuStrip menu = new ContextMenuStrip();
            ModernMenu.Install(menu);
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
            ModernMenu.Refresh(menu);
        }
    }
}
