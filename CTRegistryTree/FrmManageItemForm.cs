using CTPlugins;
using System;
using System.Windows.Forms;

namespace CTRegistryTree
{
    public partial class FrmManageItemForm : FrmTemplateDialog
    {
        /// <summary>
        /// Action types in the exact order they're added to <c>cbAction</c>, so that
        /// <c>cbAction.SelectedIndex + 1</c> / <c>(int)Item.Action - 1</c> keep mapping
        /// correctly to the enum's declared values (RunCommand=1, OpenUrl=2, OpenFile=3).
        /// </summary>
        private static readonly RegistryTreeItem.ActionType[] actionOrder = new[]
        {
            RegistryTreeItem.ActionType.RunCommand,
            RegistryTreeItem.ActionType.OpenUrl,
            RegistryTreeItem.ActionType.OpenFile
        };

        public RegistryTreeItem Item { get; private set; }

        private readonly string parentPath;

        public FrmManageItemForm(string path = "")
        {
            InitializeComponent();

            parentPath = path;
            PopulateActionItems();
            cbAction.SelectedIndex = 0;

            OKClicked += delegate { Item = BuildItem(new RegistryTreeItem()); };
            CancelClicked += delegate { Item = null; };
        }

        public FrmManageItemForm(RegistryTreeItem item)
        {
            InitializeComponent();

            PopulateActionItems();
            SetItem(item);

            OKClicked += delegate { Item = BuildItem(Item); };
            CancelClicked += delegate { Item = item; };
        }

        private void PopulateActionItems()
        {
            foreach (var action in actionOrder)
            {
                cbAction.Items.Add(GetActionDisplayText(action));
            }
        }

        private static string GetActionDisplayText(RegistryTreeItem.ActionType action)
        {
            switch (action)
            {
                case RegistryTreeItem.ActionType.RunCommand:
                    return Properties.Strings.ActionType_RunCommand;
                case RegistryTreeItem.ActionType.OpenUrl:
                    return Properties.Strings.ActionType_OpenUrl;
                case RegistryTreeItem.ActionType.OpenFile:
                    return Properties.Strings.ActionType_OpenFile;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
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

        private void btnTest_Click(object sender, EventArgs e)
        {
            var action = (RegistryTreeItem.ActionType)(cbAction.SelectedIndex + 1);
            var testItem = new RegistryTreeItem(Guid.NewGuid(), tbText.Text, action, tbCommand.Text);
            CTRegistryTree.ExecuteAction(testItem);
        }
    }
}
