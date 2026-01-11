using CTPlugins;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
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

            if(key == null) key.Close();
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
                foreach (var subKeyName in rootKey.GetSubKeyNames())
                {
                    var itemKey = rootKey.OpenSubKey(subKeyName);
                    items.Add(new ToolStripButton(itemKey.GetValue("Name") as string));
                }
                rootKey.Close();
            }

            items.Add(new ToolStripSeparator());
            items.Add(new ToolStripButton("Zarządzaj", null, delegate {
                using (FrmManageItemsForm form = new FrmManageItemsForm())
                { 
                    if(form.ShowDialog() == DialogResult.OK)
                    {
                        // odświeżamy menu
                    }
                }
            }));

            return items.ToArray();
        }
    }
}
