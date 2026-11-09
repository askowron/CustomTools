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

        private static readonly TimeSpan JustClosedWindow = TimeSpan.FromMilliseconds(300);

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
            trayIcon.Text = "Custom Tools v0.9.2";
            trayIcon.BalloonTipClicked += (s, e) => new FrmUpdateAvailable().Show();

            // Find plugins once, but rebuild the menu contents on every open, so changes
            // plugins persist (e.g. in the registry) show up immediately.
            List<ICTPlugin> plugins = CTPlugins.CTPlugins.FindPlugins();
            ContextMenuStrip menu = new ContextMenuStrip();
            ModernMenu.Install(menu);
            RebuildMenu(menu, plugins, trayIcon);

            // Attach the menu to the icon
            trayIcon.ContextMenuStrip = menu;
            ConnectTrayClicks(trayIcon, menu, plugins);

            UpdateChecker.StartBackgroundChecking(trayIcon);

            // Run the application loop with no window
            Application.Run();
        }

        /// <summary>
        /// Opens the menu on a click and closes it on the next one. Pressing the mouse on the
        /// taskbar can dismiss the menu before the click arrives, when it would look closed and
        /// reopen, so whether it was open is decided at the press. NotifyIcon shows the menu by
        /// itself on a right-click, before raising MouseClick, so that path is stopped in Opening.
        /// </summary>
        private static void ConnectTrayClicks(NotifyIcon trayIcon, ContextMenuStrip menu, List<ICTPlugin> plugins)
        {
            bool openAtPress = false;
            DateTime closedAt = DateTime.MinValue;

            menu.Closed += (s, e) => closedAt = DateTime.UtcNow;

            menu.Opening += (s, e) =>
            {
                if (openAtPress)
                    e.Cancel = true;
                else
                    RebuildMenu(menu, plugins, trayIcon);
            };

            trayIcon.MouseDown += (s, e) =>
            {
                openAtPress = menu.Visible || DateTime.UtcNow - closedAt < JustClosedWindow;
            };

            trayIcon.MouseClick += (s, e) =>
            {
                if (openAtPress)
                {
                    openAtPress = false;
                    menu.Close();
                }
                else if (e.Button == MouseButtons.Left)
                {
                    menu.Show(Cursor.Position);
                }
            };
        }

        private static void RebuildMenu(ContextMenuStrip menu, List<ICTPlugin> plugins, NotifyIcon trayIcon)
        {
            menu.Items.Clear();
            foreach (ICTPlugin plugin in plugins)
            {
                ToolStripItem[] items = plugin.GetMenuItems();

                // Every plugin is its own section; a Tag the plugin set itself still wins.
                foreach (ToolStripItem item in items)
                {
                    if (item.Tag == null)
                        item.Tag = plugin.Name;
                }

                menu.Items.AddRange(items);
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
