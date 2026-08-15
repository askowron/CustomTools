using Microsoft.Win32;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CTRegistryTree
{
    public class RegistryTreeItem
    {
        /// <summary>
        /// Specifies the type of action to perform, such as running a command, opening a URL, opening a file,
        /// or acting as a pure submenu container with no runnable action.
        /// </summary>
        public enum ActionType
        {
            RunCommand = 1,
            OpenUrl = 2,
            OpenFile = 3,
            Submenu = 4
        }

        /// <summary>
        /// Which registry hive an item is stored under: <see cref="CurrentUser"/> (HKCU, always writable)
        /// or <see cref="LocalMachine"/> (HKLM, write requires an elevated process).
        /// </summary>
        public enum Scope
        {
            CurrentUser = 1,
            LocalMachine = 2
        }

        /// <summary>
        /// Gets or sets the unique identifier for the entity.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Display text for the registry tree item.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the action to be performed.
        /// </summary>
        public ActionType Action { get; set; }

        /// <summary>
        /// Gets or sets the command text to be executed.
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// Id of this item's logical parent, or <see cref="Guid.Empty"/> for a root-level item. Storage is
        /// flat (one key per item, keyed by its own <see cref="Id"/>), so this is the only thing that
        /// expresses tree structure — it is not derived from where the key physically lives, since a
        /// child's <see cref="ItemScope"/> (and therefore hive) can differ from its parent's.
        /// </summary>
        public Guid ParentId { get; set; }

        /// <summary>
        /// Which hive this item is stored under. Never persisted as its own registry value — always
        /// derived from the hive a key was actually read from, so it can't drift out of sync.
        /// </summary>
        public Scope ItemScope { get; set; }

        public RegistryTreeItem()
        {
            Id = Guid.NewGuid();
            Text = string.Empty;
            Action = ActionType.RunCommand;
            Command = string.Empty;
            ParentId = Guid.Empty;
            ItemScope = Scope.CurrentUser;
        }

        public RegistryTreeItem(Guid guid, string text, ActionType action, string command, Guid parentId = default(Guid), Scope scope = Scope.CurrentUser)
        {
            Id = guid;
            Text = text;
            Action = action;
            Command = command;
            ParentId = parentId;
            ItemScope = scope;
        }

        /// <summary>
        /// Converts a <see cref="RegistryKey"/> instance to a <see cref="RegistryTreeItem"/>. <see
        /// cref="ItemScope"/> is inferred from whether <paramref name="key"/>'s full path starts under
        /// HKEY_LOCAL_MACHINE or HKEY_CURRENT_USER.
        /// </summary>
        public static explicit operator RegistryTreeItem(RegistryKey key)
        {
            if (key == null) return null;

            string text = key.GetValue("Name") as string ?? string.Empty;
            int actionValue = (int)(key.GetValue("Action") ?? 1);
            string command = key.GetValue("Command") as string ?? string.Empty;
            ActionType action = ActionType.RunCommand;
            if (Enum.IsDefined(typeof(ActionType), actionValue))
            {
                action = (ActionType)actionValue;
            }

            Guid id;
            if (!Guid.TryParse(key.GetValue("Id") as string, out id))
            {
                id = Guid.NewGuid();
            }

            Guid parentId;
            if (!Guid.TryParse(key.GetValue("ParentId") as string, out parentId))
            {
                parentId = Guid.Empty;
            }

            Scope scope = key.Name.StartsWith(@"HKEY_LOCAL_MACHINE", StringComparison.OrdinalIgnoreCase)
                ? Scope.LocalMachine
                : Scope.CurrentUser;

            return new RegistryTreeItem(id, text, action, command, parentId, scope);
        }

        /// <summary>
        /// Converts the specified <see cref="RegistryTreeItem"/> to a <see cref="RegistryKey"/> under the
        /// hive matching <see cref="ItemScope"/>, at the flat location <c>Items\{Id}</c> (no nesting).
        /// </summary>
        public static explicit operator RegistryKey(RegistryTreeItem item)
        {
            if (item == null) return null;

            RegistryKey hive = item.ItemScope == Scope.LocalMachine ? Registry.LocalMachine : Registry.CurrentUser;
            var key = hive.CreateSubKey($@"{CTRegistryTree.ROOT}\{CTRegistryTree.Items}\{item.Id}");
            key.SetValue("Id", item.Id.ToString());
            key.SetValue("ParentId", item.ParentId.ToString());
            key.SetValue("Name", item.Text);
            key.SetValue("Action", (int)item.Action);
            key.SetValue("Command", item.Command);
            return key;
        }

        /// <summary>
        /// Converts a <see cref="TreeNode"/> to a <see cref="RegistryTreeItem"/> if the node's <c>Tag</c> property
        /// contains a <see cref="RegistryTreeItem"/> instance.
        /// </summary>
        public static explicit operator RegistryTreeItem(TreeNode node)
        {
            if (node == null || !(node.Tag is RegistryTreeItem)) return null;
            return (RegistryTreeItem)node.Tag;
        }

        /// <summary>
        /// Converts a <see cref="RegistryTreeItem"/> instance to a <see cref="TreeNode"/>. LocalMachine
        /// items get a " (LM)" text suffix and a muted gray color, so mixed-scope siblings stay
        /// distinguishable in every tree that renders items this way (manage-items tree, import preview).
        /// The node's ImageKey/SelectedImageKey are set from the item's Action, matched against an
        /// ImageList keyed by ActionType name (see FrmManageItemsForm.InitializeIcons).
        /// </summary>
        public static explicit operator TreeNode(RegistryTreeItem item)
        {
            if (item == null) return null;

            bool isLocalMachine = item.ItemScope == Scope.LocalMachine;
            string text = isLocalMachine ? $"{item.Text} (LM)" : item.Text;

            TreeNode node = new TreeNode(text);
            node.Tag = item;
            if (isLocalMachine)
                node.ForeColor = Color.FromArgb(100, 100, 100);

            node.ImageKey = item.Action.ToString();
            node.SelectedImageKey = item.Action.ToString();
            return node;
        }
    }
}
