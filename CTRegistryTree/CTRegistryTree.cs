using CTPlugins;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Principal;
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

            if (IsElevated())
            {
                var lmKey = Registry.LocalMachine.OpenSubKey(ROOT);
                if (lmKey == null)
                {
                    lmKey = Registry.LocalMachine.CreateSubKey(ROOT);
                    lmKey.CreateSubKey(Items);
                }
                if (lmKey != null) lmKey.Close();
            }
        }

        /// <summary>
        /// True when the current process is running elevated (as Administrator). Writing to HKLM requires
        /// this; reading from HKLM does not.
        /// </summary>
        internal static bool IsElevated()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public ToolStripItem[] GetMenuItems()
        {
            return LoadItems();
        }

        protected ToolStripItem[] LoadItems()
        {
            List<ToolStripItem> items = new List<ToolStripItem>();

            List<RegistryTreeItem> allItems = ReadAllItems();
            Dictionary<Guid, List<RegistryTreeItem>> childrenByParent = GroupByParent(allItems);

            items.AddRange(BuildMenuItems(Guid.Empty, childrenByParent));

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
        /// Reads every item from both HKCU and HKLM (flat, one key per item under Items\{Id}) into a
        /// single list. Reading HKLM never requires elevation.
        /// </summary>
        internal static List<RegistryTreeItem> ReadAllItems()
        {
            var result = new List<RegistryTreeItem>();
            ReadHiveItems(Registry.CurrentUser, result);
            ReadHiveItems(Registry.LocalMachine, result);
            return result;
        }

        private static void ReadHiveItems(RegistryKey hive, List<RegistryTreeItem> result)
        {
            using (var itemsKey = hive.OpenSubKey($@"{ROOT}\{Items}"))
            {
                if (itemsKey == null) return;

                foreach (var subKeyName in itemsKey.GetSubKeyNames())
                {
                    using (var subKey = itemsKey.OpenSubKey(subKeyName))
                    {
                        result.Add((RegistryTreeItem)subKey);
                    }
                }
            }
        }

        /// <summary>
        /// Groups items by <see cref="RegistryTreeItem.ParentId"/>. An item whose ParentId doesn't match
        /// any loaded item's Id (e.g. a dangling reference after a partial delete) is treated as
        /// root-level rather than dropped. Each group is stably sorted LocalMachine-first.
        /// </summary>
        internal static Dictionary<Guid, List<RegistryTreeItem>> GroupByParent(List<RegistryTreeItem> items)
        {
            var ids = new HashSet<Guid>();
            foreach (var item in items) ids.Add(item.Id);

            var map = new Dictionary<Guid, List<RegistryTreeItem>>();
            foreach (var item in items)
            {
                Guid effectiveParent = ids.Contains(item.ParentId) ? item.ParentId : Guid.Empty;
                List<RegistryTreeItem> list;
                if (!map.TryGetValue(effectiveParent, out list))
                {
                    list = new List<RegistryTreeItem>();
                    map[effectiveParent] = list;
                }
                list.Add(item);
            }

            foreach (var list in map.Values)
                SortByScope(list);

            return map;
        }

        /// <summary>
        /// Stably reorders so all LocalMachine items come before all CurrentUser items, preserving each
        /// group's existing relative order (LINQ's OrderBy is a stable sort).
        /// </summary>
        internal static void SortByScope(List<RegistryTreeItem> items)
        {
            var sorted = items.OrderBy(i => i.ItemScope == RegistryTreeItem.Scope.LocalMachine ? 0 : 1).ToList();
            items.Clear();
            items.AddRange(sorted);
        }

        /// <summary>
        /// Recursively builds menu items from the in-memory parent/child map: leaf items become clickable
        /// menu items that execute their action, items with children become submenus, and an item
        /// explicitly typed <see cref="RegistryTreeItem.ActionType.Submenu"/> with no children renders as
        /// a disabled placeholder.
        /// </summary>
        private static IEnumerable<ToolStripItem> BuildMenuItems(Guid parentId, Dictionary<Guid, List<RegistryTreeItem>> childrenByParent)
        {
            List<RegistryTreeItem> children;
            if (!childrenByParent.TryGetValue(parentId, out children))
                yield break;

            foreach (var item in children)
            {
                bool isContainer = childrenByParent.ContainsKey(item.Id) || item.Action == RegistryTreeItem.ActionType.Submenu;

                if (isContainer)
                {
                    var menuItem = new ToolStripMenuItem(item.Text);
                    if (childrenByParent.ContainsKey(item.Id))
                        menuItem.DropDownItems.AddRange(BuildMenuItems(item.Id, childrenByParent).ToArray());
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
