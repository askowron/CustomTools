# Registry Tree Import/Export Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Import/Export buttons to `FrmManageItemsForm` that let users save the entire `CTRegistryTree` menu-item tree to an XML file and load items back in from one, additively, under the currently selected node.

**Architecture:** A new static class `RegistryTreeXmlSerializer` in the `CTRegistryTree` project converts between `TreeNode`/`RegistryTreeItem` data and an `XDocument`, with no registry or UI dependencies of its own. `FrmManageItemsForm` gets two new buttons that call into it and then reuse the form's existing `SaveItem` method to persist imported items to the registry.

**Tech Stack:** .NET Framework 4.8, WinForms, `System.Xml.Linq` (already referenced by the project).

## Global Constraints

- Build with `msbuild CustomTools.slnx /p:Configuration=Debug` — this repo has no `dotnet build` support (classic csproj) and no test projects, per `CLAUDE.md`. Every task's verification step is a build + manual check, not an automated test run.
- `CTRegistryTree`'s Debug `OutputPath` already redirects to `CustomTools/bin/Debug/Plugins/`, so a Debug build is directly runnable via `CustomTools/bin/Debug/CustomTools.exe` — no manual copy step needed.
- All new user-facing strings need both English (`Strings.resx`) and Polish (`Strings.pl.resx`) entries, following the existing `Button_*`/`Error_*` naming and `Properties.Strings.*` usage pattern already used throughout `CTRegistryTree`.
- Follow the existing error-handling style: catch exceptions at the UI boundary and show them via `MessageBox.Show(..., MessageBoxButtons.OK, MessageBoxIcon.Warning)`, as in `CTRegistryTree.ExecuteAction`.
- Export always serializes the whole tree; import always adds new items (fresh `Guid` per item) under the selected node (or root if nothing is selected) — it never overwrites or replaces existing registry data. (Design decisions from `docs/superpowers/specs/2026-08-13-registry-import-export-design.md`.)
- `Id` and `Path` are not part of the XML schema — only `Text`, `Action` (enum name), and `Command` are serialized per `<Item>` element, nested to mirror the tree.

---

### Task 1: Add Import/Export localization strings

**Files:**
- Modify: `CTRegistryTree/Properties/Strings.resx`
- Modify: `CTRegistryTree/Properties/Strings.pl.resx`
- Modify: `CTRegistryTree/Properties/Strings.Designer.cs`

**Interfaces:**
- Produces: `Properties.Strings.Button_Import`, `Properties.Strings.Button_Export`, `Properties.Strings.Dialog_XmlFilter`, `Properties.Strings.Error_ImportFailed`, `Properties.Strings.Error_ExportFailed` (all `string`, used by Tasks 3 and 4).

- [ ] **Step 1: Add English entries to `Strings.resx`**

In `CTRegistryTree/Properties/Strings.resx`, add these `<data>` elements right before the closing `</root>` tag (after the existing `ActionType_OpenFile` entry):

```xml
  <data name="Button_Import" xml:space="preserve">
    <value>Import</value>
  </data>
  <data name="Button_Export" xml:space="preserve">
    <value>Export</value>
  </data>
  <data name="Dialog_XmlFilter" xml:space="preserve">
    <value>XML files (*.xml)|*.xml</value>
  </data>
  <data name="Error_ImportFailed" xml:space="preserve">
    <value>Failed to import items: {0}</value>
  </data>
  <data name="Error_ExportFailed" xml:space="preserve">
    <value>Failed to export items: {0}</value>
  </data>
```

- [ ] **Step 2: Add Polish entries to `Strings.pl.resx`**

In `CTRegistryTree/Properties/Strings.pl.resx`, add these `<data>` elements right before the closing `</root>` tag (after the existing `ActionType_OpenFile` entry):

```xml
  <data name="Button_Import" xml:space="preserve">
    <value>Importuj</value>
  </data>
  <data name="Button_Export" xml:space="preserve">
    <value>Eksportuj</value>
  </data>
  <data name="Dialog_XmlFilter" xml:space="preserve">
    <value>Pliki XML (*.xml)|*.xml</value>
  </data>
  <data name="Error_ImportFailed" xml:space="preserve">
    <value>Nie udało się zaimportować elementów: {0}</value>
  </data>
  <data name="Error_ExportFailed" xml:space="preserve">
    <value>Nie udało się wyeksportować elementów: {0}</value>
  </data>
```

- [ ] **Step 3: Add generated properties to `Strings.Designer.cs`**

In `CTRegistryTree/Properties/Strings.Designer.cs`, add these properties right before the closing `}` of the `Strings` class (after the existing `ActionType_OpenFile` property, around line 140):

```csharp
        internal static string Button_Import {
            get {
                return ResourceManager.GetString("Button_Import", resourceCulture);
            }
        }

        internal static string Button_Export {
            get {
                return ResourceManager.GetString("Button_Export", resourceCulture);
            }
        }

        internal static string Dialog_XmlFilter {
            get {
                return ResourceManager.GetString("Dialog_XmlFilter", resourceCulture);
            }
        }

        internal static string Error_ImportFailed {
            get {
                return ResourceManager.GetString("Error_ImportFailed", resourceCulture);
            }
        }

        internal static string Error_ExportFailed {
            get {
                return ResourceManager.GetString("Error_ExportFailed", resourceCulture);
            }
        }
```

- [ ] **Step 4: Build to verify no errors**

Run: `msbuild CustomTools.slnx /p:Configuration=Debug`
Expected: Build succeeds (0 errors). These properties aren't referenced by any code yet, so there's nothing else to check.

- [ ] **Step 5: Commit**

```bash
git add CTRegistryTree/Properties/Strings.resx CTRegistryTree/Properties/Strings.pl.resx CTRegistryTree/Properties/Strings.Designer.cs
git commit -m "Add localization strings for Registry Tree import/export"
```

---

### Task 2: Add `RegistryTreeXmlSerializer`

**Files:**
- Create: `CTRegistryTree/RegistryTreeXmlSerializer.cs`
- Modify: `CTRegistryTree/CTRegistryTree.csproj`

**Interfaces:**
- Consumes: `RegistryTreeItem.ActionType` enum (`CTRegistryTree/RegistryTreeItem.cs`) — values `RunCommand`, `OpenUrl`, `OpenFile`.
- Produces:
  - `RegistryTreeXmlSerializer.Export(IEnumerable<TreeNode> nodes) : XDocument`
  - `RegistryTreeXmlSerializer.Import(string xml) : List<RegistryTreeImportedItem>` — throws `FormatException` if the root element isn't `<RegistryTreeItems>`.
  - `RegistryTreeImportedItem` class with `Text` (string), `Action` (`RegistryTreeItem.ActionType`), `Command` (string), `Children` (`List<RegistryTreeImportedItem>`) — used by Task 4 to walk the parsed tree and create real `RegistryTreeItem`s with fresh IDs.

- [ ] **Step 1: Create `RegistryTreeXmlSerializer.cs`**

Create `CTRegistryTree/RegistryTreeXmlSerializer.cs`:

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
```

- [ ] **Step 2: Register the new file in the csproj**

In `CTRegistryTree/CTRegistryTree.csproj`, add a line inside the existing `<Compile Include="RegistryTreeItem.cs" />` group — insert right after it:

```xml
    <Compile Include="RegistryTreeItem.cs" />
    <Compile Include="RegistryTreeXmlSerializer.cs" />
```

(`System.Xml.Linq` is already referenced by the project — no other csproj changes needed.)

- [ ] **Step 3: Build to verify**

Run: `msbuild CustomTools.slnx /p:Configuration=Debug`
Expected: Build succeeds (0 errors). `RegistryTreeXmlSerializer` isn't called from anywhere yet, so this only checks it compiles standalone.

- [ ] **Step 4: Commit**

```bash
git add CTRegistryTree/RegistryTreeXmlSerializer.cs CTRegistryTree/CTRegistryTree.csproj
git commit -m "Add RegistryTreeXmlSerializer for tree <-> XML conversion"
```

---

### Task 3: Add Export button

**Files:**
- Modify: `CTRegistryTree/FrmManageItemsForm.Designer.cs`
- Modify: `CTRegistryTree/FrmManageItemsForm.cs`

**Interfaces:**
- Consumes: `RegistryTreeXmlSerializer.Export(IEnumerable<TreeNode>)` (Task 2), `Properties.Strings.Button_Export`, `Properties.Strings.Dialog_XmlFilter`, `Properties.Strings.Error_ExportFailed` (Task 1).
- Produces: `btnExport` control and `btnExport_Click` handler on `FrmManageItemsForm`.

- [ ] **Step 1: Add the `btnExport` control in the Designer**

In `CTRegistryTree/FrmManageItemsForm.Designer.cs`, inside `InitializeComponent()`:

1. Add the field declaration next to the others at the top of the method — change:

```csharp
            this.tvItems = new System.Windows.Forms.TreeView();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnEdit = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
```

to:

```csharp
            this.tvItems = new System.Windows.Forms.TreeView();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnEdit = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
```

2. Add the control block right after the `btnRemove` block (after its `this.btnRemove.UseVisualStyleBackColor = true;` line, before the `// FrmManageItemsForm` comment):

```csharp
            // 
            // btnExport
            // 
            this.btnExport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExport.Location = new System.Drawing.Point(296, 99);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(75, 23);
            this.btnExport.TabIndex = 4;
            this.btnExport.Text = Properties.Strings.Button_Export;
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
```

3. Add it to the form's `Controls` and child-index list — change:

```csharp
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.btnEdit);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.tvItems);
            this.MinimumSize = new System.Drawing.Size(400, 450);
            this.Name = "FrmManageItemsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = Properties.Strings.Form_ManageItems_Title;
            this.Controls.SetChildIndex(this.tvItems, 0);
            this.Controls.SetChildIndex(this.btnAdd, 0);
            this.Controls.SetChildIndex(this.btnEdit, 0);
            this.Controls.SetChildIndex(this.btnRemove, 0);
```

to:

```csharp
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.btnEdit);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.tvItems);
            this.MinimumSize = new System.Drawing.Size(400, 450);
            this.Name = "FrmManageItemsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = Properties.Strings.Form_ManageItems_Title;
            this.Controls.SetChildIndex(this.tvItems, 0);
            this.Controls.SetChildIndex(this.btnAdd, 0);
            this.Controls.SetChildIndex(this.btnEdit, 0);
            this.Controls.SetChildIndex(this.btnRemove, 0);
            this.Controls.SetChildIndex(this.btnExport, 0);
```

4. Add the field declaration at the bottom of the file — change:

```csharp
        private System.Windows.Forms.Button btnRemove;
    }
}
```

to:

```csharp
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnExport;
    }
}
```

- [ ] **Step 2: Add the `btnExport_Click` handler**

In `CTRegistryTree/FrmManageItemsForm.cs`, add the needed usings — change:

```csharp
using CTPlugins;
using Microsoft.Win32;
using System;
using System.Windows.Forms;
```

to:

```csharp
using CTPlugins;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
```

Then add the handler after `btnRemove_Click` (before `SaveItem`):

```csharp
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
```

- [ ] **Step 3: Build to verify**

Run: `msbuild CustomTools.slnx /p:Configuration=Debug`
Expected: Build succeeds (0 errors).

- [ ] **Step 4: Manually verify export**

1. Launch `CustomTools/bin/Debug/CustomTools.exe`.
2. Open the tray icon's menu → Registry → Manage. Use **Add** to create at least one top-level item and one nested item (select the top-level item, then Add again) with distinct Text/Action/Command values.
3. Click **Export**, save as e.g. `test-export.xml`.
4. Open the saved file in a text editor and confirm it has a `<RegistryTreeItems>` root with nested `<Item Text="..." Action="..." Command="...">` elements matching what you created, and no `Id`/`Path` attributes.

- [ ] **Step 5: Commit**

```bash
git add CTRegistryTree/FrmManageItemsForm.Designer.cs CTRegistryTree/FrmManageItemsForm.cs
git commit -m "Add Export button to Registry Tree manage-items dialog"
```

---

### Task 4: Add Import button

**Files:**
- Modify: `CTRegistryTree/FrmManageItemsForm.Designer.cs`
- Modify: `CTRegistryTree/FrmManageItemsForm.cs`

**Interfaces:**
- Consumes: `RegistryTreeXmlSerializer.Import(string)` and `RegistryTreeImportedItem` (Task 2), `Properties.Strings.Button_Import`, `Properties.Strings.Dialog_XmlFilter`, `Properties.Strings.Error_ImportFailed` (Task 1), existing private `SaveItem(RegistryTreeItem)` method and `RegistryTreeItem(Guid, string, RegistryTreeItem.ActionType, string, string)` constructor and `(TreeNode)item` conversion operator (both in `RegistryTreeItem.cs`).
- Produces: `btnImport` control and `btnImport_Click` handler on `FrmManageItemsForm`.

- [ ] **Step 1: Add the `btnImport` control in the Designer**

In `CTRegistryTree/FrmManageItemsForm.Designer.cs`, inside `InitializeComponent()`:

1. Add the field declaration — change:

```csharp
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
```

to:

```csharp
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.btnImport = new System.Windows.Forms.Button();
```

2. Add the control block right after the `btnExport` block (after its `this.btnExport.Click += ...;` line, before the `// FrmManageItemsForm` comment):

```csharp
            // 
            // btnImport
            // 
            this.btnImport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnImport.Location = new System.Drawing.Point(296, 128);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(75, 23);
            this.btnImport.TabIndex = 5;
            this.btnImport.Text = Properties.Strings.Button_Import;
            this.btnImport.UseVisualStyleBackColor = true;
            this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
```

3. Add it to `Controls` and the child-index list — change:

```csharp
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.btnEdit);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.tvItems);
            this.MinimumSize = new System.Drawing.Size(400, 450);
            this.Name = "FrmManageItemsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = Properties.Strings.Form_ManageItems_Title;
            this.Controls.SetChildIndex(this.tvItems, 0);
            this.Controls.SetChildIndex(this.btnAdd, 0);
            this.Controls.SetChildIndex(this.btnEdit, 0);
            this.Controls.SetChildIndex(this.btnRemove, 0);
            this.Controls.SetChildIndex(this.btnExport, 0);
```

to:

```csharp
            this.Controls.Add(this.btnImport);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.btnEdit);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.tvItems);
            this.MinimumSize = new System.Drawing.Size(400, 450);
            this.Name = "FrmManageItemsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = Properties.Strings.Form_ManageItems_Title;
            this.Controls.SetChildIndex(this.tvItems, 0);
            this.Controls.SetChildIndex(this.btnAdd, 0);
            this.Controls.SetChildIndex(this.btnEdit, 0);
            this.Controls.SetChildIndex(this.btnRemove, 0);
            this.Controls.SetChildIndex(this.btnExport, 0);
            this.Controls.SetChildIndex(this.btnImport, 0);
```

4. Add the field declaration at the bottom of the file — change:

```csharp
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnExport;
    }
}
```

to:

```csharp
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnImport;
    }
}
```

- [ ] **Step 2: Add the `btnImport_Click` handler**

In `CTRegistryTree/FrmManageItemsForm.cs`, add the `System.IO` using — change:

```csharp
using CTPlugins;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
```

to:

```csharp
using CTPlugins;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
```

Then add the handler and its private helper after `btnExport_Click` (before `SaveItem`):

```csharp
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
```

Note: this calls the existing `private static void SaveItem(RegistryTreeItem item)` method already defined further down in this file — no change needed there.

- [ ] **Step 3: Build to verify**

Run: `msbuild CustomTools.slnx /p:Configuration=Debug`
Expected: Build succeeds (0 errors).

- [ ] **Step 4: Manually verify import**

1. Launch `CustomTools/bin/Debug/CustomTools.exe`, open Registry → Manage.
2. With nothing selected, click **Import** and choose the `test-export.xml` file saved in Task 3. Confirm the exported items reappear as new nodes directly under Root, expanded, with the same Text/Action/Command as before.
3. Select one of the newly imported nodes, click **Import** again with the same file. Confirm the items are added as *children* of the selected node this time (not siblings), each with freshly generated IDs — open `regedit`, navigate to `HKCU\SOFTWARE\Appit\CustomTools\Items`, and confirm the two imports produced separate registry key GUIDs rather than colliding.
4. Confirm the items that existed before either import (from Task 3, step 2) are unchanged.
5. Create a malformed XML file (e.g. `<Foo/>` as its entire content) and try importing it. Confirm a `MessageBox` reports a clear error and no new nodes/registry keys are created.

- [ ] **Step 5: Commit**

```bash
git add CTRegistryTree/FrmManageItemsForm.Designer.cs CTRegistryTree/FrmManageItemsForm.cs
git commit -m "Add Import button to Registry Tree manage-items dialog"
```

---

### Task 5: Final round-trip verification

**Files:** None (verification only).

- [ ] **Step 1: Full round-trip check**

With the app still running from Task 4: export the full tree again (now containing the original items plus both import passes), then import that new export file under a fresh third location in the tree. Confirm the resulting subtree structure (nesting, Text, Action, Command) exactly matches what was exported, and that no existing items were altered or removed anywhere in the tree.

- [ ] **Step 2: Confirm Release build isn't silently broken**

Run: `msbuild CustomTools.slnx /p:Configuration=Release`
Expected: Build succeeds (0 errors). (Per `CLAUDE.md`, Release output isn't auto-copied to `Plugins/`, so no manual run is expected here — this just confirms the new code compiles in both configurations.)
