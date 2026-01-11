using Microsoft.Win32;
using System;
using System.Windows.Forms;

namespace CTRegistryTree
{
    public class RegistryTreeItem
    {
        /// <summary>
        /// Specifies the type of action to perform, such as running a command, opening a URL, or opening a file.
        /// </summary>
        /// <remarks>Use <see cref="ActionType"/> to indicate the intended operation in APIs that support
        /// multiple action types. The values correspond to distinct behaviors and may affect how parameters are
        /// interpreted or processed.</remarks>
        public enum ActionType
        {
            RunCommand = 1,
            OpenUrl = 2,
            OpenFile = 3
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

        public string Path;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegistryTreeItem"/> class with default property values.
        /// </summary>
        /// <remarks>The <see cref="Text"/> property is set to an empty string, <see cref="Action"/> is
        /// set to <see cref="ActionType.RunCommand"/>, and <see cref="Command"/> is set to an empty string.</remarks>
        public RegistryTreeItem()
        {
            Id = Guid.NewGuid();
            Text = string.Empty;
            Action = ActionType.RunCommand;
            Command = string.Empty;
            Path = "/";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegistryTreeItem"/> class with the specified display text,
        /// action type, and command string.
        /// </summary>
        /// <param name="text">The display text for the registry tree item. Cannot be null.</param>
        /// <param name="action">The action type associated with this item, indicating the operation to perform.</param>
        /// <param name="command">The command string to execute when the item is activated. Cannot be null.</param>
        public RegistryTreeItem(Guid guid, string text, ActionType action, string command, string path = "/")
        {
            Id = guid;
            Text = text;
            Action = action;
            Command = command;
            Path = path;
        }

        /// <summary>
        /// Converts a <see cref="RegistryKey"/> instance to a <see cref="RegistryTreeItem"/> by extracting relevant
        /// values.
        /// </summary>
        /// <remarks>The "Action" value is interpreted as an <see cref="ActionType"/> if it matches a
        /// defined value; otherwise, <see cref="ActionType.RunCommand"/> is used. If the "Name" or "Command" values are
        /// missing, empty strings are used.</remarks>
        /// <param name="key">The registry key containing the values used to initialize the <see cref="RegistryTreeItem"/>. Must not be
        /// <see langword="null"/>.</param>
        public static explicit operator RegistryTreeItem(RegistryKey key)
        {
            if (key == null) return null;
            string text = key.GetValue("Name") as string ?? string.Empty;
            int actionValue = (int)(key.GetValue("Action") ?? 1);
            string command = key.GetValue("Command") as string ?? string.Empty;
            ActionType action = ActionType.RunCommand;
            if (System.Enum.IsDefined(typeof(ActionType), actionValue))
            {
                action = (ActionType)actionValue;
            }
            return new RegistryTreeItem(Guid.NewGuid(), text, action, command);
        }

        /// <summary>
        /// Converts the specified <see cref="RegistryTreeItem"/> to a <see cref="Microsoft.Win32.RegistryKey"/>
        /// instance representing the registry entry for the item.
        /// </summary>
        /// <remarks>The returned registry key is created under the current user's registry hive. The
        /// key's values for "Name", "Action", and "Command" are set based on the properties of the <see
        /// cref="RegistryTreeItem"/>.</remarks>
        /// <param name="item">The <see cref="RegistryTreeItem"/> to convert. If <paramref name="item"/> is <see langword="null"/>, the
        /// operator returns <see langword="null"/>.</param>
        public static explicit operator RegistryKey(RegistryTreeItem item)
        {
            if (item == null) return null;
            var key = Registry.CurrentUser.CreateSubKey($"{CTRegistryTree.ROOT}\\{CTRegistryTree.Items}\\{item.Text}");
            key.SetValue("Name", item.Text);
            key.SetValue("Action", (int)item.Action);
            key.SetValue("Command", item.Command);
            return key;
        }

        /// <summary>
        /// Converts a <see cref="TreeNode"/> to a <see cref="RegistryTreeItem"/> if the node's <c>Tag</c> property
        /// contains a <see cref="RegistryTreeItem"/> instance.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> to convert. The node's <c>Tag</c> property must reference a <see
        /// cref="RegistryTreeItem"/> instance.</param>
        public static explicit operator RegistryTreeItem(TreeNode node)
        {
            if (node == null || !(node.Tag is RegistryTreeItem)) return null;
            return (RegistryTreeItem)node.Tag;
        }

        /// <summary>
        /// Converts a <see cref="RegistryTreeItem"/> instance to a <see cref="TreeNode"/> with its text and tag set
        /// accordingly.
        /// </summary>
        /// <param name="item">The <see cref="RegistryTreeItem"/> to convert. If <paramref name="item"/> is <see langword="null"/>, the
        /// conversion returns <see langword="null"/>.</param>
        public static explicit operator TreeNode(RegistryTreeItem item)
        {
            if (item == null) return null;
            TreeNode node = new TreeNode(item.Text);
            node.Tag = item;
            return node;
        }
    }
}
