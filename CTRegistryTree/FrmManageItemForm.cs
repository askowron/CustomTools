using CTPlugins;
using System;
using System.Windows.Forms;

namespace CTRegistryTree
{
    public partial class FrmManageItemForm : FrmTemplateDialog
    {
        public RegistryTreeItem Item { get; private set; }

        private readonly string parentPath;

        public FrmManageItemForm(string path = "")
        {
            InitializeComponent();

            parentPath = path;
            cbAction.Items.AddRange(Enum.GetNames(typeof(RegistryTreeItem.ActionType)));
            cbAction.SelectedIndex = 0;

            OKClicked += delegate { Item = BuildItem(new RegistryTreeItem()); };
            CancelClicked += delegate { Item = null; };
        }

        public FrmManageItemForm(RegistryTreeItem item)
        {
            InitializeComponent();

            cbAction.Items.AddRange(Enum.GetNames(typeof(RegistryTreeItem.ActionType)));
            SetItem(item);

            OKClicked += delegate { Item = BuildItem(Item); };
            CancelClicked += delegate { Item = item; };
        }

        private RegistryTreeItem BuildItem(RegistryTreeItem item)
        {
            item.Text = tbText.Text;
            item.Action = (RegistryTreeItem.ActionType)(cbAction.SelectedIndex + 1);
            item.Command = tbCommand.Text;
            if (string.IsNullOrEmpty(item.Path) || item.Path == "/")
                item.Path = $"{parentPath}/{item.Id}";
            return item;
        }

        private void SetItem(RegistryTreeItem item)
        {
            Item = item;
            tbText.Text = Item.Text;
            cbAction.SelectedIndex = (int)Item.Action - 1;
            tbCommand.Text = Item.Command;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    tbCommand.Text = dialog.FileName;
                }
            }
        }
    }
}
