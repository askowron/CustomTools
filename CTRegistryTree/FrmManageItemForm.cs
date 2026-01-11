using CTPlugins;
using System;

namespace CTRegistryTree
{
    public partial class FrmManageItemForm : FrmTemplateDialog
    {
        public RegistryTreeItem Item { get; private set; }
        
        private string Path;

        public FrmManageItemForm(string path = "")
        {
            InitializeComponent();

            Path = path;
            cbAction.Items.AddRange(Enum.GetNames(typeof(RegistryTreeItem.ActionType)));
            cbAction.SelectedIndex = 0;

            OKClicked += delegate {

                Item = new RegistryTreeItem();
                Item.Text = tbText.Text;
                Item.Action = (RegistryTreeItem.ActionType)(cbAction.SelectedIndex + 1);
                Item.Command = tbCommand.Text;
                Item.Path = $"{Path}/{Item.Id}";
            };

            CancelClicked += delegate {
                Item = null;
            };
        }

        public FrmManageItemForm(RegistryTreeItem item) : base()
        {
            SetItem(item);
        }

        private void SetItem(RegistryTreeItem item)
        {
            Item = item;
            tbText.Text = Item.Text;
            cbAction.SelectedIndex = (int)Item.Action - 1;
            tbCommand.Text = Item.Command;
        }
    }
}
