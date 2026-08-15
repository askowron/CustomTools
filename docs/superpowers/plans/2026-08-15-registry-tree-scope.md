# Registry Tree Item Scope (CurrentUser / LocalMachine) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let each `CTRegistryTree` item be created for the current user (`HKCU`) or for all users of the machine (`HKLM`, admin-only), chosen per item in the Add/Edit dialog independently of its parent's scope, with LocalMachine items listed before CurrentUser items at every menu level.

**Architecture:** Storage moves from nested registry subkeys (physical path == logical tree path) to flat per-hive entries keyed by the item's own `Id`, linked by an explicit `ParentId` GUID — required because a child's scope can differ from its parent's, and one registry key can't span two hives. The tray menu builder and the manage-items tree both read both hives, merge the flat lists into a `ParentId → children` map, and recurse from `Guid.Empty`, stably sorting each sibling group LocalMachine-first. Elevation (`WindowsPrincipal.IsInRole(Administrator)`) gates the LocalMachine option in the Add/Edit dialog and the Edit/Remove buttons for existing LocalMachine items.

**Tech Stack:** .NET Framework 4.8, WinForms, `Microsoft.Win32.Registry`, `System.Security.Principal`.

## Global Constraints

- Build with `msbuild CustomTools.slnx /p:Configuration=Debug` (and confirm `/p:Configuration=Release` too) — no `dotnet build`, no test projects, per `CLAUDE.md`. Verification is build + manual check (there is no automated test runner in this repo).
- All new user-facing strings need both English (`Strings.resx`) and Polish (`Strings.pl.resx`) entries, plus the matching generated property in `Strings.Designer.cs`, following the existing naming pattern (e.g. `Label_*`, `ActionType_*`).
- No migration of existing `HKCU` nested-key data — the old on-disk format is abandoned outright, per the approved design (`docs/superpowers/specs/2026-08-15-registry-tree-scope-design.md`).
- Never write to `HKLM` without checking `CTRegistryTree.IsElevated()` first in UI code — reads from `HKLM` are always safe and require no check.
- Preserve the existing `Submenu` action-type behavior exactly: an item is a menu container if it has children **or** `Action == ActionType.Submenu`; a childless `Submenu` item still renders as a disabled placeholder.
- `RegistryTreeItem.Scope` is never itself persisted as a registry value — it's always derived from which hive (`HKEY_CURRENT_USER` vs `HKEY_LOCAL_MACHINE`) a key was read from, so it can't drift out of sync with where the item actually lives.

---

### Task 1: `RegistryTreeItem` data model — `Scope`, `ParentId`, flat storage

**Files:**
- Modify: `CTRegistryTree/RegistryTreeItem.cs`

**Interfaces:**
- Produces: `RegistryTreeItem.Scope` enum (`CurrentUser = 1`, `LocalMachine = 2`), `RegistryTreeItem.ParentId : Guid`, `RegistryTreeItem.ItemScope : Scope`, constructor `RegistryTreeItem(Guid guid, string text, ActionType action, string command, Guid parentId = default(Guid), Scope scope = Scope.CurrentUser)`. Removes `RegistryTreeItem.Path`. These are consumed by every later task.

- [ ] **Step 1: Replace the whole file**

Replace the full contents of `CTRegistryTree/RegistryTreeItem.cs` with:

```csharp
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

            return node;
        }
    }
}
```

- [ ] **Step 2: Build to confirm the model compiles**

Run: `msbuild CustomTools.slnx /p:Configuration=Debug`

Expected: build FAILS — other files still reference the removed `Path` property and the old constructor/operator shapes. That's expected at this point; confirm the *only* errors are in `CTRegistryTree.cs`, `FrmManageItemForm.cs`, and `FrmManageItemsForm.cs` (the files later tasks fix), not in `RegistryTreeItem.cs` itself.

- [ ] **Step 3: Commit**

```bash
git add CTRegistryTree/RegistryTreeItem.cs
git commit -m "Add Scope/ParentId to RegistryTreeItem, switch to flat per-hive storage"
```

---

### Task 2: Localization strings for the Scope selector

**Files:**
- Modify: `CTRegistryTree/Properties/Strings.resx`
- Modify: `CTRegistryTree/Properties/Strings.pl.resx`
- Modify: `CTRegistryTree/Properties/Strings.Designer.cs`

**Interfaces:**
- Produces: `Properties.Strings.Label_Scope`, `Properties.Strings.Scope_CurrentUser`, `Properties.Strings.Scope_LocalMachine` — consumed by Task 5.

- [ ] **Step 1: Add English strings**

In `CTRegistryTree/Properties/Strings.resx`, insert right after the `ActionType_Submenu` entry (before `Button_Import`):

```xml
  <data name="Label_Scope" xml:space="preserve">
    <value>Scope</value>
  </data>
  <data name="Scope_CurrentUser" xml:space="preserve">
    <value>Current user</value>
  </data>
  <data name="Scope_LocalMachine" xml:space="preserve">
    <value>All users (this computer)</value>
  </data>
```

- [ ] **Step 2: Add Polish strings**

In `CTRegistryTree/Properties/Strings.pl.resx`, insert at the same position (after `ActionType_Submenu`, before `Button_Import`):

```xml
  <data name="Label_Scope" xml:space="preserve">
    <value>Zakres</value>
  </data>
  <data name="Scope_CurrentUser" xml:space="preserve">
    <value>Bieżący użytkownik</value>
  </data>
  <data name="Scope_LocalMachine" xml:space="preserve">
    <value>Wszyscy użytkownicy (ten komputer)</value>
  </data>
```

- [ ] **Step 3: Add generated properties**

In `CTRegistryTree/Properties/Strings.Designer.cs`, insert right after the `ActionType_Submenu` property (before `Button_Import`):

```csharp
        internal static string Label_Scope {
            get {
                return ResourceManager.GetString("Label_Scope", resourceCulture);
            }
        }

        internal static string Scope_CurrentUser {
            get {
                return ResourceManager.GetString("Scope_CurrentUser", resourceCulture);
            }
        }

        internal static string Scope_LocalMachine {
            get {
                return ResourceManager.GetString("Scope_LocalMachine", resourceCulture);
            }
        }
```

- [ ] **Step 4: Build**

Run: `msbuild CustomTools.slnx /p:Configuration=Debug`
Expected: same set of pre-existing errors as Task 1 Step 2 (no new errors from this task's files).

- [ ] **Step 5: Commit**

```bash
git add CTRegistryTree/Properties/Strings.resx CTRegistryTree/Properties/Strings.pl.resx CTRegistryTree/Properties/Strings.Designer.cs
git commit -m "Add Scope label/option localization strings"
```

---

### Task 3: `CTRegistryTree.cs` — elevation check, multi-hive load, LM-first ordering

**Files:**
- Modify: `CTRegistryTree/CTRegistryTree.cs`

**Interfaces:**
- Consumes: `RegistryTreeItem.Scope`, `.ParentId`, `.ItemScope` (Task 1).
- Produces: `CTRegistryTree.IsElevated() : bool`, `CTRegistryTree.ReadAllItems() : List<RegistryTreeItem>`, `CTRegistryTree.GroupByParent(List<RegistryTreeItem>) : Dictionary<Guid, List<RegistryTreeItem>>` (values pre-sorted LM-first) — all `internal static`, consumed by Task 6 (`FrmManageItemsForm.cs`).

- [ ] **Step 1: Replace the whole file**

Replace the full contents of `CTRegistryTree/CTRegistryTree.cs` with:

```csharp
using CTPlugins;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Principal;
using System.Windows.Forms;

namespace CTRegistryTree
{
    [CustomToolsPlugin("Registry Tree", "25.12.9.1")]
    public class CTRegistryTree : ICTPlugin
    {
        public const string ROOT = @"SOFTWARE\Appit\CustomTools";
        public const string Items = "Items";

        public string Name { get; set; } = "Registry";

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
            if (key != null) key.Close();

            if (IsElevated())
            {
                var lmKey = Registry.LocalMachine.OpenSubKey(ROOT);
                if (lmKey == null)
                {
                    lmKey = Registry.LocalMachine.CreateSubKey(ROOT);
                    lmKey.CreateSubKey(Items);
                }
                if (lmKey != null) lmKey.Close();
            }
        }

        /// <summary>
        /// True when the current process is running elevated (as Administrator). Writing to HKLM requires
        /// this; reading from HKLM does not.
        /// </summary>
        internal static bool IsElevated()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public ToolStripItem[] GetMenuItems()
        {
            return LoadItems();
        }

        protected ToolStripItem[] LoadItems()
        {
            List<ToolStripItem> items = new List<ToolStripItem>();

            List<RegistryTreeItem> allItems = ReadAllItems();
            Dictionary<Guid, List<RegistryTreeItem>> childrenByParent = GroupByParent(allItems);

            items.AddRange(BuildMenuItems(Guid.Empty, childrenByParent));

            items.Add(new ToolStripSeparator());
            var manageItem = new ToolStripMenuItem(Properties.Strings.Menu_Manage, null, delegate {
                using (FrmManageItemsForm form = new FrmManageItemsForm())
                {
                    form.ShowDialog();
                }
            });
            manageItem.Font = new Font(manageItem.Font.FontFamily, manageItem.Font.Size - 1, manageItem.Font.Style);
            manageItem.ForeColor = Color.FromArgb(100, 100, 100);
            items.Add(manageItem);

            // Zamiast osobnego wiersza z podpisem, cała sekcja tej wtyczki jest oznaczana
            // wspólnym Tagiem, który GroupLabelRenderer rysuje jako pionową etykietę z lewej strony,
            // w natywnej kolumnie marginesu obrazków (ContextMenuStrip.ShowImageMargin w Program.cs).
            foreach (var item in items)
            {
                item.Tag = Name;
            }

            return items.ToArray();
        }

        /// <summary>
        /// Reads every item from both HKCU and HKLM (flat, one key per item under Items\{Id}) into a
        /// single list. Reading HKLM never requires elevation.
        /// </summary>
        internal static List<RegistryTreeItem> ReadAllItems()
        {
            var result = new List<RegistryTreeItem>();
            ReadHiveItems(Registry.CurrentUser, result);
            ReadHiveItems(Registry.LocalMachine, result);
            return result;
        }

        private static void ReadHiveItems(RegistryKey hive, List<RegistryTreeItem> result)
        {
            using (var itemsKey = hive.OpenSubKey($@"{ROOT}\{Items}"))
            {
                if (itemsKey == null) return;

                foreach (var subKeyName in itemsKey.GetSubKeyNames())
                {
                    using (var subKey = itemsKey.OpenSubKey(subKeyName))
                    {
                        result.Add((RegistryTreeItem)subKey);
                    }
                }
            }
        }

        /// <summary>
        /// Groups items by <see cref="RegistryTreeItem.ParentId"/>. An item whose ParentId doesn't match
        /// any loaded item's Id (e.g. a dangling reference after a partial delete) is treated as
        /// root-level rather than dropped. Each group is stably sorted LocalMachine-first.
        /// </summary>
        internal static Dictionary<Guid, List<RegistryTreeItem>> GroupByParent(List<RegistryTreeItem> items)
        {
            var ids = new HashSet<Guid>();
            foreach (var item in items) ids.Add(item.Id);

            var map = new Dictionary<Guid, List<RegistryTreeItem>>();
            foreach (var item in items)
            {
                Guid effectiveParent = ids.Contains(item.ParentId) ? item.ParentId : Guid.Empty;
                List<RegistryTreeItem> list;
                if (!map.TryGetValue(effectiveParent, out list))
                {
                    list = new List<RegistryTreeItem>();
                    map[effectiveParent] = list;
                }
                list.Add(item);
            }

            foreach (var list in map.Values)
                SortByScope(list);

            return map;
        }

        /// <summary>
        /// Stably reorders so all LocalMachine items come before all CurrentUser items, preserving each
        /// group's existing relative order (LINQ's OrderBy is a stable sort).
        /// </summary>
        internal static void SortByScope(List<RegistryTreeItem> items)
        {
            var sorted = items.OrderBy(i => i.ItemScope == RegistryTreeItem.Scope.LocalMachine ? 0 : 1).ToList();
            items.Clear();
            items.AddRange(sorted);
        }

        /// <summary>
        /// Recursively builds menu items from the in-memory parent/child map: leaf items become clickable
        /// menu items that execute their action, items with children become submenus, and an item
        /// explicitly typed <see cref="RegistryTreeItem.ActionType.Submenu"/> with no children renders as
        /// a disabled placeholder.
        /// </summary>
        private static IEnumerable<ToolStripItem> BuildMenuItems(Guid parentId, Dictionary<Guid, List<RegistryTreeItem>> childrenByParent)
        {
            List<RegistryTreeItem> children;
            if (!childrenByParent.TryGetValue(parentId, out children))
                yield break;

            foreach (var item in children)
            {
                bool isContainer = childrenByParent.ContainsKey(item.Id) || item.Action == RegistryTreeItem.ActionType.Submenu;

                if (isContainer)
                {
                    var menuItem = new ToolStripMenuItem(item.Text);
                    if (childrenByParent.ContainsKey(item.Id))
                        menuItem.DropDownItems.AddRange(BuildMenuItems(item.Id, childrenByParent).ToArray());
                    else
                        menuItem.Enabled = false;
                    yield return menuItem;
                }
                else
                {
                    var menuItem = new ToolStripMenuItem(item.Text);
                    menuItem.Click += (s, e) => ExecuteAction(item);
                    yield return menuItem;
                }
            }
        }

        internal static void ExecuteAction(RegistryTreeItem item)
        {
            try
            {
                // Win+R expands %variables% before launching; Process.Start does not, so do it ourselves.
                string command = Environment.ExpandEnvironmentVariables(item.Command ?? string.Empty);

                if (item.Action == RegistryTreeItem.ActionType.RunCommand)
                {
                    string fileName, arguments;
                    SplitCommand(command, out fileName, out arguments);
                    Process.Start(new ProcessStartInfo(fileName, arguments) { UseShellExecute = true });
                }
                else
                {
                    Process.Start(command);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, item.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Splits a command line into the program to launch and the arguments to pass it,
        /// so "cmd -k &quot;...&quot;" runs cmd.exe with those arguments instead of being looked up
        /// as a single (nonexistent) file named "cmd -k &quot;...&quot;".
        /// </summary>
        private static void SplitCommand(string command, out string fileName, out string arguments)
        {
            command = (command ?? string.Empty).Trim();

            if (command.StartsWith("\"", StringComparison.Ordinal))
            {
                int closingQuote = command.IndexOf('"', 1);
                if (closingQuote > 0)
                {
                    fileName = command.Substring(1, closingQuote - 1);
                    arguments = command.Substring(closingQuote + 1).Trim();
                    return;
                }
            }

            int firstSpace = command.IndexOf(' ');
            if (firstSpace < 0)
            {
                fileName = command;
                arguments = string.Empty;
            }
            else
            {
                fileName = command.Substring(0, firstSpace);
                arguments = command.Substring(firstSpace + 1).Trim();
            }
        }
    }
}
```

- [ ] **Step 2: Build**

Run: `msbuild CustomTools.slnx /p:Configuration=Debug`
Expected: remaining errors only in `FrmManageItemForm.cs` and `FrmManageItemsForm.cs` (still calling the old `Path`-based constructors/API) — fixed in Tasks 5–6. `RegistryTreeXmlSerializer.cs` doesn't reference anything changed yet, so it should compile cleanly even before Task 4 adds `Scope` to it.

- [ ] **Step 3: Commit**

```bash
git add CTRegistryTree/CTRegistryTree.cs
git commit -m "Read both hives, merge by ParentId, order menu items LocalMachine-first"
```

---

### Task 4: `RegistryTreeXmlSerializer.cs` — carry `Scope` through import/export

**Files:**
- Modify: `CTRegistryTree/RegistryTreeXmlSerializer.cs`

**Interfaces:**
- Consumes: `RegistryTreeItem.Scope`, `.ItemScope` (Task 1).
- Produces: `RegistryTreeImportedItem.Scope : RegistryTreeItem.Scope`, constructor `RegistryTreeImportedItem(string text, RegistryTreeItem.ActionType action, string command, RegistryTreeItem.Scope scope)` — consumed by Task 6.

- [ ] **Step 1: Replace the whole file**

Replace the full contents of `CTRegistryTree/RegistryTreeXmlSerializer.cs` with:

```csharp
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
                new XAttribute("Command", item?.Command ?? string.Empty),
                new XAttribute("Scope", (item?.ItemScope ?? RegistryTreeItem.Scope.CurrentUser).ToString()));

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

            string scopeText = (string)element.Attribute("Scope");
            RegistryTreeItem.Scope scope;
            if (scopeText == null || !Enum.TryParse(scopeText, out scope) || !Enum.IsDefined(typeof(RegistryTreeItem.Scope), scope))
                scope = RegistryTreeItem.Scope.CurrentUser;

            var imported = new RegistryTreeImportedItem(text, action, command, scope);
            foreach (var childElement in element.Elements(ItemElementName))
                imported.Children.Add(ImportElement(childElement));

            return imported;
        }
    }

    /// <summary>
    /// A parsed import-file node. Deliberately has no Id or ParentId yet — both depend on where
    /// in the live tree the caller ends up placing it.
    /// </summary>
    internal sealed class RegistryTreeImportedItem
    {
        public string Text { get; }
        public RegistryTreeItem.ActionType Action { get; }
        public string Command { get; }
        public RegistryTreeItem.Scope Scope { get; }
        public List<RegistryTreeImportedItem> Children { get; } = new List<RegistryTreeImportedItem>();

        public RegistryTreeImportedItem(string text, RegistryTreeItem.ActionType action, string command, RegistryTreeItem.Scope scope)
        {
            Text = text;
            Action = action;
            Command = command;
            Scope = scope;
        }
    }
}
```

- [ ] **Step 2: Build**

Run: `msbuild CustomTools.slnx /p:Configuration=Debug`
Expected: remaining errors only in `FrmManageItemForm.cs` and `FrmManageItemsForm.cs`.

- [ ] **Step 3: Commit**

```bash
git add CTRegistryTree/RegistryTreeXmlSerializer.cs
git commit -m "Carry Scope through registry tree XML import/export"
```

---

### Task 5: `FrmManageItemForm` — Scope selector UI

**Files:**
- Modify: `CTRegistryTree/FrmManageItemForm.Designer.cs`
- Modify: `CTRegistryTree/FrmManageItemForm.cs`

**Interfaces:**
- Consumes: `RegistryTreeItem.Scope`/`.ParentId`/`.ItemScope` (Task 1), `Properties.Strings.Label_Scope`/`Scope_CurrentUser`/`Scope_LocalMachine` (Task 2), `CTRegistryTree.IsElevated()` (Task 3).
- Produces: constructors `FrmManageItemForm(Guid parentId, RegistryTreeItem.Scope? parentScope = null)` (new item) and `FrmManageItemForm(RegistryTreeItem item)` (edit existing, unchanged signature) — consumed by Task 6. `FrmManageItemForm.Item.ItemScope` and `.ParentId` are now populated on `OKClicked`.

- [ ] **Step 1: Add the Scope group box to the designer**

In `CTRegistryTree/FrmManageItemForm.Designer.cs`, in the field declarations at the top of `InitializeComponent`, change:

```csharp
            this.btnTest = new System.Windows.Forms.Button();
            this.SuspendLayout();
```

to:

```csharp
            this.btnTest = new System.Windows.Forms.Button();
            this.gbScope = new System.Windows.Forms.GroupBox();
            this.rbLocalMachine = new System.Windows.Forms.RadioButton();
            this.rbCurrentUser = new System.Windows.Forms.RadioButton();
            this.gbScope.SuspendLayout();
            this.SuspendLayout();
```

Then, right after the `btnTest` block (after its `Click` handler line, before the `// FrmManageItemForm` comment), insert:

```csharp
            //
            // gbScope
            //
            this.gbScope.Controls.Add(this.rbLocalMachine);
            this.gbScope.Controls.Add(this.rbCurrentUser);
            this.gbScope.Location = new System.Drawing.Point(12, 163);
            this.gbScope.Name = "gbScope";
            this.gbScope.Size = new System.Drawing.Size(436, 48);
            this.gbScope.TabIndex = 10;
            this.gbScope.TabStop = false;
            this.gbScope.Text = Properties.Strings.Label_Scope;
            //
            // rbCurrentUser
            //
            this.rbCurrentUser.AutoSize = true;
            this.rbCurrentUser.Checked = true;
            this.rbCurrentUser.Location = new System.Drawing.Point(12, 20);
            this.rbCurrentUser.Name = "rbCurrentUser";
            this.rbCurrentUser.TabIndex = 0;
            this.rbCurrentUser.TabStop = true;
            this.rbCurrentUser.Text = Properties.Strings.Scope_CurrentUser;
            this.rbCurrentUser.UseVisualStyleBackColor = true;
            //
            // rbLocalMachine
            //
            this.rbLocalMachine.AutoSize = true;
            this.rbLocalMachine.Location = new System.Drawing.Point(180, 20);
            this.rbLocalMachine.Name = "rbLocalMachine";
            this.rbLocalMachine.TabIndex = 1;
            this.rbLocalMachine.Text = Properties.Strings.Scope_LocalMachine;
            this.rbLocalMachine.UseVisualStyleBackColor = true;
```

Then change:

```csharp
            this.ClientSize = new System.Drawing.Size(464, 210);
            this.Controls.Add(this.btnTest);
```

to:

```csharp
            this.ClientSize = new System.Drawing.Size(464, 270);
            this.Controls.Add(this.gbScope);
            this.Controls.Add(this.btnTest);
```

Then change:

```csharp
            this.Controls.SetChildIndex(this.btnTest, 0);
            this.ResumeLayout(false);
            this.PerformLayout();
```

to:

```csharp
            this.Controls.SetChildIndex(this.btnTest, 0);
            this.Controls.SetChildIndex(this.gbScope, 0);
            this.gbScope.ResumeLayout(false);
            this.gbScope.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
```

Finally, in the private field declarations at the bottom of the file, change:

```csharp
        private System.Windows.Forms.Button btnTest;
    }
}
```

to:

```csharp
        private System.Windows.Forms.Button btnTest;
        private System.Windows.Forms.GroupBox gbScope;
        private System.Windows.Forms.RadioButton rbLocalMachine;
        private System.Windows.Forms.RadioButton rbCurrentUser;
    }
}
```

- [ ] **Step 2: Wire up scope logic in the code-behind**

Replace the full contents of `CTRegistryTree/FrmManageItemForm.cs` with:

```csharp
using CTPlugins;
using System;
using System.Windows.Forms;

namespace CTRegistryTree
{
    public partial class FrmManageItemForm : FrmTemplateDialog
    {
        /// <summary>
        /// Action types in the exact order they're added to <c>cbAction</c>. This array is the
        /// single source of truth for mapping <c>cbAction.SelectedIndex</c> to an <see
        /// cref="RegistryTreeItem.ActionType"/> — always read the selection through
        /// <c>actionOrder[cbAction.SelectedIndex]</c>, never by arithmetic on the enum's values.
        /// </summary>
        private static readonly RegistryTreeItem.ActionType[] actionOrder = new[]
        {
            RegistryTreeItem.ActionType.RunCommand,
            RegistryTreeItem.ActionType.OpenUrl,
            RegistryTreeItem.ActionType.OpenFile,
            RegistryTreeItem.ActionType.Submenu
        };

        public RegistryTreeItem Item { get; private set; }

        private readonly Guid newItemParentId;
        private readonly bool isEditing;

        /// <summary>
        /// Dialog for adding a new item under <paramref name="parentId"/> (<see cref="Guid.Empty"/> for a
        /// root-level item). <paramref name="parentScope"/>, when given, is used only to pick a sensible
        /// default Scope selection (LocalMachine if the parent is LocalMachine and that's currently usable).
        /// </summary>
        public FrmManageItemForm(Guid parentId, RegistryTreeItem.Scope? parentScope = null)
        {
            InitializeComponent();

            newItemParentId = parentId;
            isEditing = false;

            PopulateActionItems();
            cbAction.SelectedIndex = 0;
            UpdateCommandFieldsEnabled();
            InitializeScopeOptions();

            bool preferLocalMachine = parentScope == RegistryTreeItem.Scope.LocalMachine && rbLocalMachine.Enabled;
            rbLocalMachine.Checked = preferLocalMachine;
            rbCurrentUser.Checked = !preferLocalMachine;

            OKClicked += delegate { Item = BuildItem(new RegistryTreeItem()); };
            CancelClicked += delegate { Item = null; };
        }

        public FrmManageItemForm(RegistryTreeItem item)
        {
            InitializeComponent();

            isEditing = true;

            PopulateActionItems();
            InitializeScopeOptions();
            SetItem(item);
            UpdateCommandFieldsEnabled();

            OKClicked += delegate { Item = BuildItem(Item); };
            CancelClicked += delegate { Item = item; };
        }

        private void InitializeScopeOptions()
        {
            rbLocalMachine.Enabled = CTRegistryTree.IsElevated();
        }

        private void PopulateActionItems()
        {
            foreach (var action in actionOrder)
            {
                cbAction.Items.Add(GetActionDisplayText(action));
            }
        }

        private void cbAction_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateCommandFieldsEnabled();
        }

        private void UpdateCommandFieldsEnabled()
        {
            bool isSubmenu = cbAction.SelectedIndex >= 0
                && actionOrder[cbAction.SelectedIndex] == RegistryTreeItem.ActionType.Submenu;

            tbCommand.Enabled = !isSubmenu;
            button1.Enabled = !isSubmenu;
            btnTest.Enabled = !isSubmenu;
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
                case RegistryTreeItem.ActionType.Submenu:
                    return Properties.Strings.ActionType_Submenu;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }

        private RegistryTreeItem BuildItem(RegistryTreeItem item)
        {
            item.Text = tbText.Text;
            item.Action = actionOrder[cbAction.SelectedIndex];
            item.Command = tbCommand.Text;
            item.ItemScope = rbLocalMachine.Checked ? RegistryTreeItem.Scope.LocalMachine : RegistryTreeItem.Scope.CurrentUser;
            if (!isEditing)
                item.ParentId = newItemParentId;
            return item;
        }

        private void SetItem(RegistryTreeItem item)
        {
            Item = item;
            tbText.Text = Item.Text;
            cbAction.SelectedIndex = (int)Item.Action - 1;
            tbCommand.Text = Item.Command;
            rbLocalMachine.Checked = Item.ItemScope == RegistryTreeItem.Scope.LocalMachine;
            rbCurrentUser.Checked = !rbLocalMachine.Checked;
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
            var action = actionOrder[cbAction.SelectedIndex];
            var testItem = new RegistryTreeItem(Guid.NewGuid(), tbText.Text, action, tbCommand.Text);
            CTRegistryTree.ExecuteAction(testItem);
        }
    }
}
```

Note: reaching the `FrmManageItemForm(RegistryTreeItem item)` (edit) constructor for a LocalMachine item always implies the process is elevated, because Task 6 disables the Edit button for LocalMachine items when not elevated — so `SetItem` never needs to re-check elevation before checking `rbLocalMachine`.

- [ ] **Step 3: Build**

Run: `msbuild CustomTools.slnx /p:Configuration=Debug`
Expected: remaining errors only in `FrmManageItemsForm.cs` (still calling the old `FrmManageItemForm(string path)` / `.Path`-based API).

- [ ] **Step 4: Commit**

```bash
git add CTRegistryTree/FrmManageItemForm.cs CTRegistryTree/FrmManageItemForm.Designer.cs
git commit -m "Add Scope selector to the Add/Edit item dialog, gated on elevation"
```

---

### Task 6: `FrmManageItemsForm.cs` — merged tree, elevation gating, recursive remove, hive-move edit

**Files:**
- Modify: `CTRegistryTree/FrmManageItemsForm.cs`

**Interfaces:**
- Consumes: `RegistryTreeItem.Scope`/`.ParentId`/`.ItemScope` (Task 1), `CTRegistryTree.IsElevated()`/`.ReadAllItems()`/`.GroupByParent()` (Task 3), `RegistryTreeImportedItem.Scope` (Task 4), `FrmManageItemForm(Guid, RegistryTreeItem.Scope?)` / `FrmManageItemForm(RegistryTreeItem)` (Task 5).
- Produces: nothing consumed by other tasks — this is the last production file in the chain.

- [ ] **Step 1: Replace the whole file**

Replace the full contents of `CTRegistryTree/FrmManageItemsForm.cs` with:

```csharp
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
```

- [ ] **Step 2: Build (Debug)**

Run: `msbuild CustomTools.slnx /p:Configuration=Debug`
Expected: SUCCESS, zero errors.

- [ ] **Step 3: Build (Release)**

Run: `msbuild CustomTools.slnx /p:Configuration=Release`
Expected: SUCCESS, zero errors. (Per `CLAUDE.md`, Release output isn't auto-copied to `Plugins/` — not needed for manual testing in Task 7, which uses the Debug build.)

- [ ] **Step 4: Commit**

```bash
git add CTRegistryTree/FrmManageItemsForm.cs
git commit -m "Merge both hives in the manage-items tree; gate Add/Edit/Remove on elevation"
```

---

### Task 7: Manual verification

**Files:** none (verification only).

No test project exists in this repo (`CLAUDE.md`), so this feature is verified by running the built app directly, non-elevated and elevated. `CustomTools/bin/Debug/Plugins/CTRegistryTree.dll` is where the Debug build lands (per the project's `OutputPath` redirect) — running `CustomTools/bin/Debug/CustomTools.exe` after Task 6's Debug build will pick it up.

- [ ] **Step 1: Non-elevated pass**

Launch `CustomTools.exe` normally (not as Administrator).

- Open the tray menu → Manage. Add a CurrentUser item at the tree root; confirm it appears with no "(LM)" suffix, in default text color.
- Confirm the Add dialog's "All users (this computer)" radio is disabled (grayed) and unselectable.
- Close and reopen the tray menu; confirm the new item appears as a clickable entry.
- In Manage, select any item (there should be no LocalMachine items yet) — Edit/Remove should be enabled for it.

- [ ] **Step 2: Elevated pass — LocalMachine items and ordering**

Right-click `CustomTools.exe` (or its Debug build output) → "Run as administrator".

- In Manage, add a LocalMachine item at the tree root; confirm the "All users (this computer)" radio is enabled and selectable this time.
- Add a second CurrentUser item at the tree root (if one doesn't already exist from Step 1's data).
- Close and reopen the tray menu; confirm the LocalMachine item appears **before** the CurrentUser item(s) at the top level, and both show up in the Manage tree with the LocalMachine one suffixed " (LM)" in gray.
- Add a LocalMachine child under the CurrentUser root item, and a CurrentUser child under the LocalMachine root item; confirm both nest correctly under their chosen parent and each level orders its own LocalMachine children before its own CurrentUser children.

- [ ] **Step 3: Elevated pass — scope move and cascading delete**

Still running elevated:

- Edit the CurrentUser root item from Step 2 and flip it to LocalMachine; save. Confirm: it now shows " (LM)" in the tree, the tray menu ordering updates accordingly, and its previously-added children (of either scope) are still nested under it correctly.
- Select a parent item that has both LocalMachine and CurrentUser descendants and click Remove; confirm the whole subtree disappears from the tree and from the tray menu.

- [ ] **Step 4: Non-elevated pass — gating on existing LocalMachine items**

Close the elevated instance, relaunch non-elevated:

- In Manage, select an existing LocalMachine item; confirm Edit and Remove are both disabled (grayed).
- Select a CurrentUser item that has a LocalMachine descendant (create one while elevated first if none exists); confirm Remove is disabled on it too, while Edit stays enabled (since the item itself is CurrentUser).
- Select a plain CurrentUser item with no LocalMachine descendants; confirm Edit and Remove are both enabled.

- [ ] **Step 5: Import/export round-trip**

- While elevated, build a small mixed-scope tree (a CurrentUser item and a LocalMachine item, one nested under the other), then Export it to an XML file. Open the file in a text editor and confirm each `<Item>` has a `Scope="CurrentUser"` or `Scope="LocalMachine"` attribute.
- Import that same file back in (into a different parent node, to avoid Id collisions with the originals — Import always assigns fresh Ids). Confirm both scopes round-trip correctly (LocalMachine stays LocalMachine, since still elevated).
- Relaunch non-elevated and import the same file again. Confirm the LocalMachine item imports as a CurrentUser item instead (no error, no elevation prompt).
- Import a pre-existing export file that predates this feature (no `Scope` attribute) if one is available, or hand-craft one without the attribute; confirm it imports as CurrentUser without error.

- [ ] **Step 6: Regression check — Submenu action type still works**

- Add an item with Action = Submenu and no children; confirm it renders as a disabled placeholder in the tray menu (this behavior must be unaffected by the scope changes).
- Add a child under it; confirm it becomes a normal clickable submenu.
