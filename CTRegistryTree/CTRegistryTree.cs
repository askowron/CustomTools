using CTPlugins;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace CTRegistryTree
{
    [CustomToolsPlugin("Registry Tree", "25.12.9.1")]
    public class CTRegistryTree : ICTPlugin
    {
        public const string ROOT = @"SOFTWARE\Appit\CustomTools";
        public const string Items = "Items";

        public string Name { get; set; }

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
            items.Add(new CtrlToolStripLabel("Registry items"));

            var rootKey = Registry.CurrentUser.OpenSubKey(root);
            if (rootKey != null)
            {
                using (rootKey)
                {
                    items.AddRange(BuildMenuItems(rootKey));
                }
            }

            items.Add(new ToolStripSeparator());
            items.Add(new ToolStripButton("Zarządzaj", null, delegate {
                using (FrmManageItemsForm form = new FrmManageItemsForm())
                {
                    form.ShowDialog();
                }
            }));

            return items.ToArray();
        }

        /// <summary>
        /// Recursively builds menu items from a registry key: leaf items become clickable buttons that
        /// execute their action, items with children become submenus.
        /// </summary>
        private static IEnumerable<ToolStripItem> BuildMenuItems(RegistryKey parentKey)
        {
            foreach (var subKeyName in parentKey.GetSubKeyNames())
            {
                using (var subKey = parentKey.OpenSubKey(subKeyName))
                {
                    var item = (RegistryTreeItem)subKey;

                    if (subKey.SubKeyCount > 0)
                    {
                        var menuItem = new ToolStripMenuItem(item.Text);
                        menuItem.DropDownItems.AddRange(BuildMenuItems(subKey).ToArray());
                        yield return menuItem;
                    }
                    else
                    {
                        var button = new ToolStripButton(item.Text);
                        button.Click += (s, e) => ExecuteAction(item);
                        yield return button;
                    }
                }
            }
        }

        private static void ExecuteAction(RegistryTreeItem item)
        {
            try
            {
                Process.Start(item.Command);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, item.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
