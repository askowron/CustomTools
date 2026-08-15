using CTPlugins;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
            RefreshTree(null);
        }

        /// <summary>
        /// Reloads the tree from both hives (merged by ParentId, LocalMachine-first at every level — same
        /// logic <see cref="CTRegistryTree"/> uses for the live menu) and, if <paramref name="selectId"/>
        /// is given, re-selects and reveals that item.
        /// </summary>
        private void RefreshTree(Guid? selectId)
        {
            tvItems.Nodes.Clear();
            var rootNode = new TreeNode(Properties.Strings.Tree_Root);
            tvItems.Nodes.Add(rootNode);

            List<RegistryTreeItem> allItems = CTRegistryTree.ReadAllItems();
            Dictionary<Guid, List<RegistryTreeItem>> childrenByParent = CTRegistryTree.GroupByParent(allItems);

            BuildTreeNodes(Guid.Empty, rootNode, childrenByParent);

            rootNode.Expand();

            if (selectId.HasValue)
                SelectNodeById(rootNode, selectId.Value);

            tvItems_AfterSelect(tvItems, null);
        }

        private static void BuildTreeNodes(Guid parentId, TreeNode parentNode, Dictionary<Guid, List<RegistryTreeItem>> childrenByParent)
        {
            List<RegistryTreeItem> children;
            if (!childrenByParent.TryGetValue(parentId, out children))
                return;

            foreach (var item in children)
            {
                var node = (TreeNode)item;
                parentNode.Nodes.Add(node);
                BuildTreeNodes(item.Id, node, childrenByParent);
            }
        }

        private static bool SelectNodeById(TreeNode node, Guid id)
        {
            foreach (TreeNode child in node.Nodes)
            {
                var item = (RegistryTreeItem)child.Tag;
                if (item != null && item.Id == id)
                {
                    child.TreeView.SelectedNode = child;
                    child.EnsureVisible();
                    return true;
                }
                if (SelectNodeById(child, id))
                    return true;
            }
            return false;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (tvItems.SelectedNode == null)
                return;

            var parentItem = (RegistryTreeItem)tvItems.SelectedNode.Tag;
            Guid parentId = parentItem?.Id ?? Guid.Empty;
            RegistryTreeItem.Scope? parentScope = parentItem?.ItemScope;

            using (FrmManageItemForm form = new FrmManageItemForm(parentId, parentScope))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    SaveItem(form.Item);
                    RefreshTree(form.Item.Id);
                    tvItems.Focus();
                }
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            var originalItem = (RegistryTreeItem)tvItems.SelectedNode?.Tag;
            if (originalItem == null)
                return;

            using (FrmManageItemForm form = new FrmManageItemForm(originalItem))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    if (form.Item.ItemScope != originalItem.ItemScope)
                        DeleteOwnKey(originalItem);

                    SaveItem(form.Item);
                    RefreshTree(form.Item.Id);
                }
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            var item = (RegistryTreeItem)tvItems.SelectedNode?.Tag;
            if (item == null)
                return;

            List<RegistryTreeItem> allItems = CTRegistryTree.ReadAllItems();
            Dictionary<Guid, List<RegistryTreeItem>> childrenByParent = CTRegistryTree.GroupByParent(allItems);

            RemoveItemRecursive(item, childrenByParent);
            RefreshTree(null);
        }

        private static void RemoveItemRecursive(RegistryTreeItem item, Dictionary<Guid, List<RegistryTreeItem>> childrenByParent)
        {
            List<RegistryTreeItem> children;
            if (childrenByParent.TryGetValue(item.Id, out children))
            {
                foreach (var child in children)
                    RemoveItemRecursive(child, childrenByParent);
            }

            DeleteOwnKey(item);
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
                    var targetItem = (RegistryTreeItem)targetNode.Tag;
                    Guid targetParentId = targetItem?.Id ?? Guid.Empty;

                    bool elevated = CTRegistryTree.IsElevated();
                    Guid? firstImportedId = null;
                    foreach (var imported in importedItems)
                    {
                        var item = BuildImportedItem(imported, targetParentId, elevated);
                        if (firstImportedId == null)
                            firstImportedId = item.Id;
                    }

                    RefreshTree(firstImportedId);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(string.Format(Properties.Strings.Error_ImportFailed, exc.Message), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        /// <summary>
        /// Saves an imported node (and its children) as real items under <paramref name="parentId"/>. A
        /// LocalMachine-scoped import is silently downgraded to CurrentUser when the process isn't
        /// elevated, rather than failing the whole import.
        /// </summary>
        private static RegistryTreeItem BuildImportedItem(RegistryTreeImportedItem imported, Guid parentId, bool elevated)
        {
            Guid id = Guid.NewGuid();
            RegistryTreeItem.Scope scope = (imported.Scope == RegistryTreeItem.Scope.LocalMachine && elevated)
                ? RegistryTreeItem.Scope.LocalMachine
                : RegistryTreeItem.Scope.CurrentUser;

            var item = new RegistryTreeItem(id, imported.Text, imported.Action, imported.Command, parentId, scope);
            SaveItem(item);

            foreach (var child in imported.Children)
                BuildImportedItem(child, item.Id, elevated);

            return item;
        }

        private static void SaveItem(RegistryTreeItem item)
        {
            using (var key = (RegistryKey)item) { }
        }

        private static void DeleteOwnKey(RegistryTreeItem item)
        {
            RegistryKey hive = item.ItemScope == RegistryTreeItem.Scope.LocalMachine ? Registry.LocalMachine : Registry.CurrentUser;
            using (var itemsKey = hive.OpenSubKey($@"{CTRegistryTree.ROOT}\{CTRegistryTree.Items}", true))
            {
                itemsKey?.DeleteSubKeyTree(item.Id.ToString(), false);
            }
        }

        private void tvItems_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var item = tvItems.SelectedNode?.Tag as RegistryTreeItem;
            bool hasItem = item != null;
            bool elevated = CTRegistryTree.IsElevated();

            bool blockedByOwnScope = hasItem && item.ItemScope == RegistryTreeItem.Scope.LocalMachine && !elevated;
            bool blockedByDescendant = hasItem && !elevated && HasLocalMachineDescendant(tvItems.SelectedNode);

            btnEdit.Enabled = hasItem && !blockedByOwnScope;
            btnRemove.Enabled = hasItem && !blockedByOwnScope && !blockedByDescendant;
        }

        /// <summary>
        /// True if any descendant (at any depth) of <paramref name="node"/> is LocalMachine-scoped.
        /// Used to block deleting a CurrentUser parent while stranding an undeletable LocalMachine child
        /// when not elevated.
        /// </summary>
        private static bool HasLocalMachineDescendant(TreeNode node)
        {
            foreach (TreeNode child in node.Nodes)
            {
                var childItem = (RegistryTreeItem)child.Tag;
                if (childItem != null && childItem.ItemScope == RegistryTreeItem.Scope.LocalMachine)
                    return true;
                if (HasLocalMachineDescendant(child))
                    return true;
            }
            return false;
        }
    }
}
