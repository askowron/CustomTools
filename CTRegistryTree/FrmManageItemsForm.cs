using CTPlugins;
using System;
using System.Windows.Forms;

namespace CTRegistryTree
{
    public partial class FrmManageItemsForm : FrmTemplateDialog
    {
        public FrmManageItemsForm()
        {
            InitializeComponent();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (tvItems.SelectedNode == null)
                return;

            using(FrmManageItemForm form = new FrmManageItemForm(((RegistryTreeItem)tvItems.SelectedNode.Tag)?.Path ?? ""))
            {
                if(form.ShowDialog() == DialogResult.OK)
                {
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
                        tvItems.SelectedNode.Text = form.Item.Text;
                        tvItems.SelectedNode.Tag = form.Item;
                    }
                }
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if(tvItems.SelectedNode != null)
            {
                //CTRegistryTree.Remove((Registr)tvItems.SelectedNode.Tag)
                tvItems.SelectedNode.Remove();
            }
        }

        private void tvItems_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if(tvItems.SelectedNode != null)
            {
                btnEdit.Enabled = true;
                btnRemove.Enabled = true;
            }
            else
            {
                btnEdit.Enabled = false;
                btnRemove.Enabled = false;
            }
        }

    }
}
