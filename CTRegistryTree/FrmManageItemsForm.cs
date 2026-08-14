using CTPlugins;
using Microsoft.Win32;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CTRegistryTree
{
    public partial class FrmManageItemsForm : FrmTemplateDialog
    {
        public FrmManageItemsForm()
        {
            InitializeComponent();
            InitializeIcons();
            LoadTree();
        }

        private void InitializeIcons()
        {
            ImageList imageList = new ImageList();
            imageList.ImageSize = new Size(16, 16);
            imageList.ColorDepth = ColorDepth.Depth32Bit;
            foreach (RegistryTreeItem.ActionType action in Enum.GetValues(typeof(RegistryTreeItem.ActionType)))
            {
                imageList.Images.Add(action.ToString(), RegistryTreeIcons.GetImage(action));
            }
            tvItems.ImageList = imageList;
        }

        private void LoadTree()
        {
            tvItems.Nodes.Clear();
            var rootNode = new TreeNode(Properties.Strings.Tree_Root);
            rootNode.ImageKey = RegistryTreeItem.ActionType.Submenu.ToString();
            rootNode.SelectedImageKey = RegistryTreeItem.ActionType.Submenu.ToString();
            tvItems.Nodes.Add(rootNode);

            var rootKey = Registry.CurrentUser.OpenSubKey($"{CTRegistryTree.ROOT}\\{CTRegistryTree.Items}");
            if (rootKey != null)
            {
                using (rootKey)
                {
                    LoadNodes(rootKey, rootNode);
                }
            }

            rootNode.Expand();
        }

        private static void LoadNodes(RegistryKey key, TreeNode parentNode)
        {
            foreach (var subKeyName in key.GetSubKeyNames())
            {
                using (var subKey = key.OpenSubKey(subKeyName))
                {
                    var item = (RegistryTreeItem)subKey;
                    var node = (TreeNode)item;
                    parentNode.Nodes.Add(node);
                    LoadNodes(subKey, node);
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (tvItems.SelectedNode == null)
                return;

            using(FrmManageItemForm form = new FrmManageItemForm(((RegistryTreeItem)tvItems.SelectedNode.Tag)?.Path ?? ""))
            {
                if(form.ShowDialog() == DialogResult.OK)
                {
                    SaveItem(form.Item);

                    tvItems.SelectedNode.Nodes.Add((TreeNode)form.Item);
                    if(!tvItems.SelectedNode.IsExpanded)
                        tvItems.SelectedNode.Expand();

                    tvItems.Focus();
                }
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if(tvItems.SelectedNode != null)
            {
                using(FrmManageItemForm form = new FrmManageItemForm((RegistryTreeItem)tvItems.SelectedNode?.Tag))
                {
                    if(form.ShowDialog() == DialogResult.OK)
                    {
                        SaveItem(form.Item);

                        tvItems.SelectedNode.Text = form.Item.Text;
                        tvItems.SelectedNode.Tag = form.Item;
                        tvItems.SelectedNode.ImageKey = form.Item.Action.ToString();
                        tvItems.SelectedNode.SelectedImageKey = form.Item.Action.ToString();
                    }
                }
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if(tvItems.SelectedNode != null)
            {
                var item = (RegistryTreeItem)tvItems.SelectedNode.Tag;
                if (item != null)
                    RemoveItem(item);

                tvItems.SelectedNode.Remove();
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog { Filter = Properties.Strings.Dialog_XmlFilter, FileName = "RegistryTreeItems.xml" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    TreeNode rootNode = tvItems.Nodes[0];
                    XDocument document = RegistryTreeXmlSerializer.Export(rootNode.Nodes.Cast<TreeNode>());
                    document.Save(dialog.FileName);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(string.Format(Properties.Strings.Error_ExportFailed, exc.Message), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog { Filter = Properties.Strings.Dialog_XmlFilter })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    string xml = File.ReadAllText(dialog.FileName);
                    var importedItems = RegistryTreeXmlSerializer.Import(xml);

                    TreeNode targetNode = tvItems.SelectedNode ?? tvItems.Nodes[0];
                    string targetPath = ((RegistryTreeItem)targetNode.Tag)?.Path ?? "";

                    foreach (var imported in importedItems)
                        targetNode.Nodes.Add(BuildImportedNode(imported, targetPath));

                    if (!targetNode.IsExpanded)
                        targetNode.Expand();
                }
                catch (Exception exc)
                {
                    MessageBox.Show(string.Format(Properties.Strings.Error_ImportFailed, exc.Message), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private static TreeNode BuildImportedNode(RegistryTreeImportedItem imported, string parentPath)
        {
            Guid id = Guid.NewGuid();
            var item = new RegistryTreeItem(id, imported.Text, imported.Action, imported.Command, $"{parentPath}/{id}");
            SaveItem(item);

            var node = (TreeNode)item;
            foreach (var child in imported.Children)
                node.Nodes.Add(BuildImportedNode(child, item.Path));

            return node;
        }

        private static void SaveItem(RegistryTreeItem item)
        {
            using (var key = (RegistryKey)item) { }
        }

        private static void RemoveItem(RegistryTreeItem item)
        {
            string relativePath = $"{CTRegistryTree.Items}{item.Path.Replace('/', '\\')}";
            int lastSeparator = relativePath.LastIndexOf('\\');
            string parentPath = relativePath.Substring(0, lastSeparator);
            string keyName = relativePath.Substring(lastSeparator + 1);

            using (var parentKey = Registry.CurrentUser.OpenSubKey($"{CTRegistryTree.ROOT}\\{parentPath}", true))
            {
                parentKey?.DeleteSubKeyTree(keyName, false);
            }
        }

        private void tvItems_AfterSelect(object sender, TreeViewEventArgs e)
        {
            bool hasItem = tvItems.SelectedNode?.Tag is RegistryTreeItem;
            btnEdit.Enabled = hasItem;
            btnRemove.Enabled = hasItem;
        }

    }
}
