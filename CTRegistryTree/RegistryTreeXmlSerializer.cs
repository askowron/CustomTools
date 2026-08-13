using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CTRegistryTree
{
    /// <summary>
    /// Converts between the in-memory registry tree (<see cref="TreeNode"/>s tagged with
    /// <see cref="RegistryTreeItem"/>) and an XML document, for import/export. Has no
    /// knowledge of the registry or of live UI state — callers own persisting the result.
    /// </summary>
    internal static class RegistryTreeXmlSerializer
    {
        private const string RootElementName = "RegistryTreeItems";
        private const string ItemElementName = "Item";

        public static XDocument Export(IEnumerable<TreeNode> nodes)
        {
            var root = new XElement(RootElementName);
            foreach (TreeNode node in nodes)
                root.Add(ExportNode(node));

            return new XDocument(root);
        }

        private static XElement ExportNode(TreeNode node)
        {
            var item = (RegistryTreeItem)node.Tag;

            var element = new XElement(ItemElementName,
                new XAttribute("Text", item?.Text ?? string.Empty),
                new XAttribute("Action", (item?.Action ?? RegistryTreeItem.ActionType.RunCommand).ToString()),
                new XAttribute("Command", item?.Command ?? string.Empty));

            foreach (TreeNode child in node.Nodes)
                element.Add(ExportNode(child));

            return element;
        }

        public static List<RegistryTreeImportedItem> Import(string xml)
        {
            XDocument document = XDocument.Parse(xml);
            if (document.Root == null || document.Root.Name.LocalName != RootElementName)
                throw new FormatException($"Expected root element '<{RootElementName}>'.");

            return document.Root.Elements(ItemElementName).Select(ImportElement).ToList();
        }

        private static RegistryTreeImportedItem ImportElement(XElement element)
        {
            string text = (string)element.Attribute("Text") ?? string.Empty;
            string command = (string)element.Attribute("Command") ?? string.Empty;
            string actionText = (string)element.Attribute("Action");

            RegistryTreeItem.ActionType action;
            if (actionText == null || !Enum.TryParse(actionText, out action) || !Enum.IsDefined(typeof(RegistryTreeItem.ActionType), action))
                action = RegistryTreeItem.ActionType.RunCommand;

            var imported = new RegistryTreeImportedItem(text, action, command);
            foreach (var childElement in element.Elements(ItemElementName))
                imported.Children.Add(ImportElement(childElement));

            return imported;
        }
    }

    /// <summary>
    /// A parsed import-file node. Deliberately has no Id or Path yet — both depend on where
    /// in the live tree the caller ends up placing it.
    /// </summary>
    internal sealed class RegistryTreeImportedItem
    {
        public string Text { get; }
        public RegistryTreeItem.ActionType Action { get; }
        public string Command { get; }
        public List<RegistryTreeImportedItem> Children { get; } = new List<RegistryTreeImportedItem>();

        public RegistryTreeImportedItem(string text, RegistryTreeItem.ActionType action, string command)
        {
            Text = text;
            Action = action;
            Command = command;
        }
    }
}
