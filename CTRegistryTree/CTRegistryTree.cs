using CTPlugins;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CTRegistryTree
{
    [CustomToolsPlugin("Registry Tree", "25.12.9.1")]
    public class CTRegistryTree : ICTPlugin
    {
        public const string ROOT = @"SOFTWARE\Appit\CustomTools";
        public const string Items = "Items";

        public string Name { get; set; } = "Registry";

        public CTRegistryTree()
        {
            InitializeItems();
        }

        private void InitializeItems()
        {
            var key = Registry.CurrentUser.OpenSubKey(ROOT);
            if (key == null)
            {
                key = Registry.CurrentUser.CreateSubKey(ROOT);
                key.CreateSubKey(Items);
            }

            if (key != null) key.Close();
        }

        public ToolStripItem[] GetMenuItems()
        {
            return LoadItems($"{ROOT}\\{Items}");
        }

        protected ToolStripItem[] LoadItems(string root)
        {
            List<ToolStripItem> items = new List<ToolStripItem>();

            var rootKey = Registry.CurrentUser.OpenSubKey(root);
            if (rootKey != null)
            {
                using (rootKey)
                {
                    items.AddRange(BuildMenuItems(rootKey));
                }
            }

            items.Add(new ToolStripSeparator());
            var manageItem = new ToolStripMenuItem(Properties.Strings.Menu_Manage, null, delegate {
                using (FrmManageItemsForm form = new FrmManageItemsForm())
                {
                    form.ShowDialog();
                }
            });
            manageItem.Font = new Font(manageItem.Font.FontFamily, manageItem.Font.Size - 1, manageItem.Font.Style);
            manageItem.ForeColor = Color.FromArgb(100, 100, 100);
            items.Add(manageItem);

            // Zamiast osobnego wiersza z podpisem, cała sekcja tej wtyczki jest oznaczana
            // wspólnym Tagiem, który GroupLabelRenderer rysuje jako pionową etykietę z lewej strony,
            // w natywnej kolumnie marginesu obrazków (ContextMenuStrip.ShowImageMargin w Program.cs).
            foreach (var item in items)
            {
                item.Tag = Name;
            }

            return items.ToArray();
        }

        /// <summary>
        /// Recursively builds menu items from a registry key: leaf items become clickable menu items that
        /// execute their action, items with children become submenus, and an item explicitly typed
        /// <see cref="RegistryTreeItem.ActionType.Submenu"/> with no children renders as a disabled placeholder.
        /// </summary>
        private static IEnumerable<ToolStripItem> BuildMenuItems(RegistryKey parentKey)
        {
            foreach (var subKeyName in parentKey.GetSubKeyNames())
            {
                using (var subKey = parentKey.OpenSubKey(subKeyName))
                {
                    var item = (RegistryTreeItem)subKey;
                    bool isContainer = subKey.SubKeyCount > 0 || item.Action == RegistryTreeItem.ActionType.Submenu;

                    if (isContainer)
                    {
                        var menuItem = new ToolStripMenuItem(item.Text);
                        if (subKey.SubKeyCount > 0)
                            menuItem.DropDownItems.AddRange(BuildMenuItems(subKey).ToArray());
                        else
                            menuItem.Enabled = false;
                        yield return menuItem;
                    }
                    else
                    {
                        var menuItem = new ToolStripMenuItem(item.Text);
                        menuItem.Click += (s, e) => ExecuteAction(item);
                        yield return menuItem;
                    }
                }
            }
        }

        internal static void ExecuteAction(RegistryTreeItem item)
        {
            try
            {
                // Win+R expands %variables% before launching; Process.Start does not, so do it ourselves.
                string command = Environment.ExpandEnvironmentVariables(item.Command ?? string.Empty);

                if (item.Action == RegistryTreeItem.ActionType.RunCommand)
                {
                    string fileName, arguments;
                    SplitCommand(command, out fileName, out arguments);
                    Process.Start(new ProcessStartInfo(fileName, arguments) { UseShellExecute = true });
                }
                else
                {
                    Process.Start(command);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, item.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Splits a command line into the program to launch and the arguments to pass it,
        /// so "cmd -k &quot;...&quot;" runs cmd.exe with those arguments instead of being looked up
        /// as a single (nonexistent) file named "cmd -k &quot;...&quot;".
        /// </summary>
        private static void SplitCommand(string command, out string fileName, out string arguments)
        {
            command = (command ?? string.Empty).Trim();

            if (command.StartsWith("\"", StringComparison.Ordinal))
            {
                int closingQuote = command.IndexOf('"', 1);
                if (closingQuote > 0)
                {
                    fileName = command.Substring(1, closingQuote - 1);
                    arguments = command.Substring(closingQuote + 1).Trim();
                    return;
                }
            }

            int firstSpace = command.IndexOf(' ');
            if (firstSpace < 0)
            {
                fileName = command;
                arguments = string.Empty;
            }
            else
            {
                fileName = command.Substring(0, firstSpace);
                arguments = command.Substring(firstSpace + 1).Trim();
            }
        }
    }
}
