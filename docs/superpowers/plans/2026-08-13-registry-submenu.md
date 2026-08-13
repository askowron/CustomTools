# Registry Tree Submenu Support Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let users explicitly mark a `CTRegistryTree` item as a "Submenu" (a pure container with no runnable action) via the existing Add/Edit dialog, instead of the current workaround of picking a real action and only having the item become a submenu once it happens to gain a child.

**Architecture:** Add `ActionType.Submenu = 4` to `RegistryTreeItem`. `FrmManageItemForm`'s existing action combo box gets a 4th "Submenu" entry that disables the now-irrelevant Command/Find/Test controls. `CTRegistryTree.BuildMenuItems` treats an item as a container (drop-down) if it has children **or** is explicitly typed `Submenu`, rendering an empty `Submenu` item as a disabled placeholder instead of a clickable leaf with an empty command.

**Tech Stack:** .NET Framework 4.8, WinForms.

## Global Constraints

- Build with `msbuild CustomTools.slnx /p:Configuration=Debug` (and confirm `/p:Configuration=Release` too) — no `dotnet build`, no test projects, per `CLAUDE.md`. Verification is build + manual check.
- All new user-facing strings need both English (`Strings.resx`) and Polish (`Strings.pl.resx`) entries, following the existing `ActionType_*` naming pattern.
- Preserve backward compatibility exactly: any existing item that already has children must keep rendering as a submenu regardless of its own `Action` value — the "has children" check must never be removed, only extended.
- Do not change the registry schema — `Submenu` is just another `int` value already handled generically by the existing `(RegistryKey)item` / `(RegistryTreeItem)key` conversion operators.
- Follow the existing `cbAction.SelectedIndex + 1 == (int)ActionType` mapping convention in `FrmManageItemForm.cs` (documented in a comment above `actionOrder`) — `Submenu` must be appended, not inserted, to keep existing saved items' `Action` values meaningful.

---

### Task 1: Add `Submenu` enum value and localization strings

**Files:**
- Modify: `CTRegistryTree/RegistryTreeItem.cs`
- Modify: `CTRegistryTree/Properties/Strings.resx`
- Modify: `CTRegistryTree/Properties/Strings.pl.resx`
- Modify: `CTRegistryTree/Properties/Strings.Designer.cs`

**Interfaces:**
- Produces: `RegistryTreeItem.ActionType.Submenu` (value `4`), `Properties.Strings.ActionType_Submenu : string` — both consumed by Tasks 2 and 3.

- [ ] **Step 1: Add the enum value**

In `CTRegistryTree/RegistryTreeItem.cs`, change:

```csharp
        public enum ActionType
        {
            RunCommand = 1,
            OpenUrl = 2,
            OpenFile = 3
        }
```

to:

```csharp
        public enum ActionType
        {
            RunCommand = 1,
            OpenUrl = 2,
            OpenFile = 3,
            Submenu = 4
        }
```

- [ ] **Step 2: Add the English string**

In `CTRegistryTree/Properties/Strings.resx`, add right after the existing `ActionType_OpenFile` entry (before `Button_Import`):

```xml
  <data name="ActionType_Submenu" xml:space="preserve">
    <value>Submenu</value>
  </data>
```

- [ ] **Step 3: Add the Polish string**

In `CTRegistryTree/Properties/Strings.pl.resx`, add right after the existing `ActionType_OpenFile` entry (before `Button_Import`):

```xml
  <data name="ActionType_Submenu" xml:space="preserve">
    <value>Podmenu</value>
  </data>
```

- [ ] **Step 4: Add the generated property**

In `CTRegistryTree/Properties/Strings.Designer.cs`, add right after the existing `ActionType_OpenFile` property (before `Button_Import`):

```csharp
        internal static string ActionType_Submenu {
            get {
                return ResourceManager.GetString("ActionType_Submenu", resourceCulture);
            }
        }
```

- [ ] **Step 5: Build to verify**

Run: `msbuild CustomTools.slnx /p:Configuration=Debug`
Expected: Build succeeds (0 errors). Nothing references `Submenu` or `ActionType_Submenu` yet, so this only checks it compiles.

- [ ] **Step 6: Commit**

```bash
git add CTRegistryTree/RegistryTreeItem.cs CTRegistryTree/Properties/Strings.resx CTRegistryTree/Properties/Strings.pl.resx CTRegistryTree/Properties/Strings.Designer.cs
git commit -m "Add Submenu action type and localization strings"
```

---

### Task 2: Render `Submenu`-typed items as containers in the tray menu

**Files:**
- Modify: `CTRegistryTree/CTRegistryTree.cs`

**Interfaces:**
- Consumes: `RegistryTreeItem.ActionType.Submenu` (Task 1).

- [ ] **Step 1: Update `BuildMenuItems`**

In `CTRegistryTree/CTRegistryTree.cs`, change:

```csharp
        private static IEnumerable<ToolStripItem> BuildMenuItems(RegistryKey parentKey)
        {
            foreach (var subKeyName in parentKey.GetSubKeyNames())
            {
                using (var subKey = parentKey.OpenSubKey(subKeyName))
                {
                    var item = (RegistryTreeItem)subKey;

                    if (subKey.SubKeyCount > 0)
                    {
                        var menuItem = new ToolStripMenuItem(item.Text);
                        menuItem.DropDownItems.AddRange(BuildMenuItems(subKey).ToArray());
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
        }
```

to:

```csharp
        private static IEnumerable<ToolStripItem> BuildMenuItems(RegistryKey parentKey)
        {
            foreach (var subKeyName in parentKey.GetSubKeyNames())
            {
                using (var subKey = parentKey.OpenSubKey(subKeyName))
                {
                    var item = (RegistryTreeItem)subKey;
                    bool isContainer = subKey.SubKeyCount > 0 || item.Action == RegistryTreeItem.ActionType.Submenu;

                    if (isContainer)
                    {
                        var menuItem = new ToolStripMenuItem(item.Text);
                        if (subKey.SubKeyCount > 0)
                            menuItem.DropDownItems.AddRange(BuildMenuItems(subKey).ToArray());
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
        }
```

This preserves the "has children" check exactly as before (so any legacy item with children still renders as a working submenu no matter its `Action`), and adds the `Submenu` check so a freshly-created, still-empty submenu renders as a grayed-out placeholder instead of a clickable leaf that would try to run an empty command.

- [ ] **Step 2: Build to verify**

Run: `msbuild CustomTools.slnx /p:Configuration=Debug`
Expected: Build succeeds (0 errors).

- [ ] **Step 3: Commit**

```bash
git add CTRegistryTree/CTRegistryTree.cs
git commit -m "Render empty Submenu items as disabled containers in the tray menu"
```

---

### Task 3: Add the "Submenu" option to the Add/Edit item dialog

**Files:**
- Modify: `CTRegistryTree/FrmManageItemForm.cs`
- Modify: `CTRegistryTree/FrmManageItemForm.Designer.cs`

**Interfaces:**
- Consumes: `RegistryTreeItem.ActionType.Submenu`, `Properties.Strings.ActionType_Submenu` (Task 1).
- Produces: `cbAction_SelectedIndexChanged` handler on `FrmManageItemForm`, wired to `cbAction.SelectedIndexChanged`.

- [ ] **Step 1: Extend `actionOrder` and `GetActionDisplayText`**

In `CTRegistryTree/FrmManageItemForm.cs`, change:

```csharp
        private static readonly RegistryTreeItem.ActionType[] actionOrder = new[]
        {
            RegistryTreeItem.ActionType.RunCommand,
            RegistryTreeItem.ActionType.OpenUrl,
            RegistryTreeItem.ActionType.OpenFile
        };
```

to:

```csharp
        private static readonly RegistryTreeItem.ActionType[] actionOrder = new[]
        {
            RegistryTreeItem.ActionType.RunCommand,
            RegistryTreeItem.ActionType.OpenUrl,
            RegistryTreeItem.ActionType.OpenFile,
            RegistryTreeItem.ActionType.Submenu
        };
```

Then change:

```csharp
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
```

to:

```csharp
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
```

- [ ] **Step 2: Add the enable/disable handler and call it on load**

In `CTRegistryTree/FrmManageItemForm.cs`, change the two constructors:

```csharp
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
```

to:

```csharp
        public FrmManageItemForm(string path = "")
        {
            InitializeComponent();

            parentPath = path;
            PopulateActionItems();
            cbAction.SelectedIndex = 0;
            UpdateCommandFieldsEnabled();

            OKClicked += delegate { Item = BuildItem(new RegistryTreeItem()); };
            CancelClicked += delegate { Item = null; };
        }

        public FrmManageItemForm(RegistryTreeItem item)
        {
            InitializeComponent();

            PopulateActionItems();
            SetItem(item);
            UpdateCommandFieldsEnabled();

            OKClicked += delegate { Item = BuildItem(Item); };
            CancelClicked += delegate { Item = item; };
        }
```

Then add the new handler right after `PopulateActionItems`:

```csharp
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
```

(This replaces the original `PopulateActionItems` method — the rest of the file, including `BuildItem`, `SetItem`, `button1_Click`, and `btnTest_Click`, is unchanged.)

- [ ] **Step 3: Wire the event in the Designer**

In `CTRegistryTree/FrmManageItemForm.Designer.cs`, change:

```csharp
            //
            // cbAction
            //
            this.cbAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbAction.FormattingEnabled = true;
            this.cbAction.Location = new System.Drawing.Point(94, 32);
            this.cbAction.Name = "cbAction";
            this.cbAction.Size = new System.Drawing.Size(354, 21);
            this.cbAction.TabIndex = 5;
            //
```

to:

```csharp
            //
            // cbAction
            //
            this.cbAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbAction.FormattingEnabled = true;
            this.cbAction.Location = new System.Drawing.Point(94, 32);
            this.cbAction.Name = "cbAction";
            this.cbAction.Size = new System.Drawing.Size(354, 21);
            this.cbAction.TabIndex = 5;
            this.cbAction.SelectedIndexChanged += new System.EventHandler(this.cbAction_SelectedIndexChanged);
            //
```

- [ ] **Step 4: Build to verify**

Run: `msbuild CustomTools.slnx /p:Configuration=Debug`
Expected: Build succeeds (0 errors).

- [ ] **Step 5: Commit**

```bash
git add CTRegistryTree/FrmManageItemForm.cs CTRegistryTree/FrmManageItemForm.Designer.cs
git commit -m "Add Submenu option to the Registry Tree Add/Edit item dialog"
```

---

### Task 4: Manual end-to-end verification

**Files:** None (verification only).

- [ ] **Step 1: Build both configurations**

Run: `msbuild CustomTools.slnx /p:Configuration=Debug`
Run: `msbuild CustomTools.slnx /p:Configuration=Release`
Expected: Both succeed (0 errors).

- [ ] **Step 2: Verify the dialog toggling**

Launch `CustomTools/bin/Debug/CustomTools.exe`, open Registry → Manage → Add. Switch `cbAction` through all 4 options and confirm the Command textbox, Find button, and Test button disable only when "Submenu" is selected, and re-enable for the other three.

- [ ] **Step 3: Verify an empty submenu renders as a disabled placeholder**

With nothing selected (or Root selected), Add a new item, set Text to e.g. "Empty Submenu Test", Action to "Submenu", leave Command blank, save. Open the tray menu → Registry and confirm "Empty Submenu Test" appears grayed out and does nothing when clicked.

- [ ] **Step 4: Verify a submenu with children works normally**

Select "Empty Submenu Test" in the manage-items tree, Add a child item under it (any real action, e.g. Run Command → `notepad.exe`). Reopen the tray menu → Registry and confirm "Empty Submenu Test" is now a normal, clickable submenu containing the child item, and the child runs `notepad.exe` when clicked.

- [ ] **Step 5: Verify backward compatibility with legacy submenus**

Find (or create) an item whose own Action is something other than Submenu (e.g. Run Command) but which already has children (the pre-existing way of making a submenu). Confirm it still renders as a working submenu in the tray menu — this must be unaffected by this change.

- [ ] **Step 6: Verify editing an item's action back and forth**

Edit "Empty Submenu Test" (which now has a child) and change its Action from Submenu to Run Command; save. Confirm it still renders as a submenu in the tray (because it has a child — the "has children" rule wins), and that the Command/Find/Test fields were re-enabled in the dialog while editing. Remove the child, then confirm the item — still typed Run Command, now with no children — becomes a normal clickable leaf again (unrelated pre-existing behavior, just confirming no regression).
