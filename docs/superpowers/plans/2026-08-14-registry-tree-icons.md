# Registry Tree Icons Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Show a distinct icon per item type (Run Command, Open URL, Open File, Submenu) in `CTRegistryTree`'s management-dialog `TreeView` (`tvItems` in `FrmManageItemsForm`).

**Architecture:** A new static helper, `RegistryTreeIcons`, resolves and caches one 16×16 shell icon per `RegistryTreeItem.ActionType` via `SHGetFileInfo` (P/Invoke into `shell32.dll`) — no bundled image assets, no disk access. `RegistryTreeItem`'s existing `TreeNode` conversion operator tags every node it creates with an `ImageKey` matching the item's action; `FrmManageItemsForm` builds a small `ImageList` keyed the same way and assigns it to the tree.

**Tech Stack:** .NET Framework 4.8, WinForms, P/Invoke (`shell32.dll`, `user32.dll`).

## Global Constraints

- Build with `msbuild CustomTools.slnx /p:Configuration=Debug` (and confirm `/p:Configuration=Release` too) — no `dotnet build`, no test projects, per `CLAUDE.md`.
- Scope is the management dialog's `TreeView` only. Do **not** touch `CTRegistryTree.cs`'s `BuildMenuItems`, `ToolStripMenuItem.Image`, or `Program.cs`'s `ContextMenuStrip.ShowImageMargin` — the real tray menu is explicitly out of scope for this plan (it conflicts with `GroupLabelRenderer`'s rotated section-label rendering, and that combination needs its own scoped, visually-verified follow-up, not a silent change here).
- Match this codebase's existing style: pre-declare `out` variables rather than using inline `out var`/`out Type` declarations (see `Guid.TryParse(..., out id)` in `RegistryTreeItem.cs` for the existing convention).
- Icon resolution must not touch the filesystem or require bundled image files — `SHGFI_USEFILEATTRIBUTES` with fake paths only.

---

### Task 1: Add `RegistryTreeIcons` shell-icon helper

**Files:**
- Create: `CTRegistryTree/RegistryTreeIcons.cs`
- Modify: `CTRegistryTree/CTRegistryTree.csproj`

**Interfaces:**
- Produces: `RegistryTreeIcons.GetImage(RegistryTreeItem.ActionType action) : System.Drawing.Image` — consumed by Task 2.

- [ ] **Step 1: Create `RegistryTreeIcons.cs`**

Create `CTRegistryTree/RegistryTreeIcons.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace CTRegistryTree
{
    /// <summary>
    /// Resolves a 16x16 shell icon per <see cref="RegistryTreeItem.ActionType"/>, using the Windows
    /// shell's extension/attribute-based icon lookup (no disk access, no bundled image assets).
    /// Results are cached after first use.
    /// </summary>
    internal static class RegistryTreeIcons
    {
        private static readonly Dictionary<RegistryTreeItem.ActionType, Image> cache = new Dictionary<RegistryTreeItem.ActionType, Image>();

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_SMALLICON = 0x000000001;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static Image GetImage(RegistryTreeItem.ActionType action)
        {
            Image image;
            if (!cache.TryGetValue(action, out image))
            {
                image = ResolveIcon(action);
                cache[action] = image;
            }
            return image;
        }

        private static Image ResolveIcon(RegistryTreeItem.ActionType action)
        {
            switch (action)
            {
                case RegistryTreeItem.ActionType.RunCommand:
                    return GetShellIcon("dummy.exe", FILE_ATTRIBUTE_NORMAL);
                case RegistryTreeItem.ActionType.OpenUrl:
                    return GetShellIcon("dummy.url", FILE_ATTRIBUTE_NORMAL);
                case RegistryTreeItem.ActionType.OpenFile:
                    return GetShellIcon("dummy", FILE_ATTRIBUTE_NORMAL);
                case RegistryTreeItem.ActionType.Submenu:
                    return GetShellIcon("folder", FILE_ATTRIBUTE_DIRECTORY);
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }

        private static Image GetShellIcon(string fakePath, uint fileAttributes)
        {
            SHFILEINFO shfi = new SHFILEINFO();
            SHGetFileInfo(fakePath, fileAttributes, ref shfi, (uint)Marshal.SizeOf(shfi), SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES);

            using (Icon icon = Icon.FromHandle(shfi.hIcon))
            {
                Image image = icon.ToBitmap();
                DestroyIcon(shfi.hIcon);
                return image;
            }
        }
    }
}
```

Note: `icon.ToBitmap()` copies the pixel data into a new managed `Bitmap` before `DestroyIcon` runs, so the returned `Image` is independent of the native icon handle — this ordering (copy, then destroy) is required and must not be changed.

- [ ] **Step 2: Register the new file in the csproj**

In `CTRegistryTree/CTRegistryTree.csproj`, add a line inside the existing `<Compile Include="RegistryTreeXmlSerializer.cs" />` group — insert right after it:

```xml
    <Compile Include="RegistryTreeXmlSerializer.cs" />
    <Compile Include="RegistryTreeIcons.cs" />
```

- [ ] **Step 3: Build to verify**

Run: `msbuild CustomTools.slnx /p:Configuration=Debug`
Expected: Build succeeds (0 errors). `RegistryTreeIcons` isn't called from anywhere yet, so this only checks it compiles standalone.

- [ ] **Step 4: Commit**

```bash
git add CTRegistryTree/RegistryTreeIcons.cs CTRegistryTree/CTRegistryTree.csproj
git commit -m "Add RegistryTreeIcons shell-icon helper"
```

---

### Task 2: Wire icons into the management dialog's TreeView

**Files:**
- Modify: `CTRegistryTree/RegistryTreeItem.cs`
- Modify: `CTRegistryTree/FrmManageItemsForm.cs`

**Interfaces:**
- Consumes: `RegistryTreeIcons.GetImage(RegistryTreeItem.ActionType)` (Task 1).

- [ ] **Step 1: Tag TreeNodes with an ImageKey in the conversion operator**

In `CTRegistryTree/RegistryTreeItem.cs`, change:

```csharp
        public static explicit operator TreeNode(RegistryTreeItem item)
        {
            if (item == null) return null;
            TreeNode node = new TreeNode(item.Text);
            node.Tag = item;
            return node;
        }
```

to:

```csharp
        public static explicit operator TreeNode(RegistryTreeItem item)
        {
            if (item == null) return null;
            TreeNode node = new TreeNode(item.Text);
            node.Tag = item;
            node.ImageKey = item.Action.ToString();
            node.SelectedImageKey = item.Action.ToString();
            return node;
        }
```

This is the single conversion point already used by `LoadNodes` (initial load from the registry), `btnAdd_Click`, and `BuildImportedNode` (XML import) in `FrmManageItemsForm.cs` — all three pick up the correct `ImageKey` automatically with no changes of their own.

- [ ] **Step 2: Build an ImageList and assign it to the TreeView**

In `CTRegistryTree/FrmManageItemsForm.cs`, add the `System.Drawing` using — change:

```csharp
using CTPlugins;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
```

to:

```csharp
using CTPlugins;
using Microsoft.Win32;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
```

Then change the constructor:

```csharp
        public FrmManageItemsForm()
        {
            InitializeComponent();
            LoadTree();
        }
```

to:

```csharp
        public FrmManageItemsForm()
        {
            InitializeComponent();
            InitializeIcons();
            LoadTree();
        }

        private void InitializeIcons()
        {
            ImageList imageList = new ImageList();
            imageList.ImageSize = new Size(16, 16);
            imageList.ColorDepth = ColorDepth.Depth32Bit;
            foreach (RegistryTreeItem.ActionType action in Enum.GetValues(typeof(RegistryTreeItem.ActionType)))
            {
                imageList.Images.Add(action.ToString(), RegistryTreeIcons.GetImage(action));
            }
            tvItems.ImageList = imageList;
        }
```

- [ ] **Step 3: Give the Root node a folder icon**

In `CTRegistryTree/FrmManageItemsForm.cs`, change `LoadTree`:

```csharp
        private void LoadTree()
        {
            tvItems.Nodes.Clear();
            var rootNode = new TreeNode(Properties.Strings.Tree_Root);
            tvItems.Nodes.Add(rootNode);
```

to:

```csharp
        private void LoadTree()
        {
            tvItems.Nodes.Clear();
            var rootNode = new TreeNode(Properties.Strings.Tree_Root);
            rootNode.ImageKey = RegistryTreeItem.ActionType.Submenu.ToString();
            rootNode.SelectedImageKey = RegistryTreeItem.ActionType.Submenu.ToString();
            tvItems.Nodes.Add(rootNode);
```

(The rest of `LoadTree` is unchanged.)

- [ ] **Step 4: Refresh the icon when editing an item's action type**

In `CTRegistryTree/FrmManageItemsForm.cs`, change `btnEdit_Click`:

```csharp
        private void btnEdit_Click(object sender, EventArgs e)
        {
            if(tvItems.SelectedNode != null)
            {
                using(FrmManageItemForm form = new FrmManageItemForm((RegistryTreeItem)tvItems.SelectedNode?.Tag))
                {
                    if(form.ShowDialog() == DialogResult.OK)
                    {
                        SaveItem(form.Item);

                        tvItems.SelectedNode.Text = form.Item.Text;
                        tvItems.SelectedNode.Tag = form.Item;
                    }
                }
            }
        }
```

to:

```csharp
        private void btnEdit_Click(object sender, EventArgs e)
        {
            if(tvItems.SelectedNode != null)
            {
                using(FrmManageItemForm form = new FrmManageItemForm((RegistryTreeItem)tvItems.SelectedNode?.Tag))
                {
                    if(form.ShowDialog() == DialogResult.OK)
                    {
                        SaveItem(form.Item);

                        tvItems.SelectedNode.Text = form.Item.Text;
                        tvItems.SelectedNode.Tag = form.Item;
                        tvItems.SelectedNode.ImageKey = form.Item.Action.ToString();
                        tvItems.SelectedNode.SelectedImageKey = form.Item.Action.ToString();
                    }
                }
            }
        }
```

This is needed because `btnEdit_Click` mutates the existing node in place rather than rebuilding it through the `TreeNode` conversion operator from Step 1 — without this, changing an item's action type (e.g. to `Submenu`) wouldn't update its icon until the dialog was reopened.

- [ ] **Step 5: Build to verify**

Run: `msbuild CustomTools.slnx /p:Configuration=Debug`
Expected: Build succeeds (0 errors).

- [ ] **Step 6: Commit**

```bash
git add CTRegistryTree/RegistryTreeItem.cs CTRegistryTree/FrmManageItemsForm.cs
git commit -m "Show per-type icons in the Registry Tree manage-items TreeView"
```

---

### Task 3: Manual verification

**Files:** None (verification only).

- [ ] **Step 1: Build both configurations**

Run: `msbuild CustomTools.slnx /p:Configuration=Debug`
Run: `msbuild CustomTools.slnx /p:Configuration=Release`
Expected: Both succeed (0 errors).

- [ ] **Step 2: Verify icons for each action type**

Launch `CustomTools/bin/Debug/CustomTools.exe`, open Registry → Manage. Add one item of each type (Run Command, Open URL, Open File, Submenu) and confirm each shows a distinct, recognizable icon: a generic program icon for Run Command, a globe/shortcut icon for Open URL, a generic document icon for Open File, and a folder icon for Submenu. Confirm the Root node also shows a folder icon.

- [ ] **Step 3: Verify icons survive reload**

Close and reopen the Manage dialog (or close and reopen `CustomTools.exe`). Confirm every item still shows its correct icon after being reloaded from the registry (exercises the `LoadNodes` → `TreeNode` conversion-operator path rather than the just-added path).

- [ ] **Step 4: Verify icons after Import**

Export the current tree (if the Export feature is available), then Import it back under a different node. Confirm imported items show correct icons (exercises the `BuildImportedNode` → `TreeNode` conversion-operator path).

- [ ] **Step 5: Verify icon updates on edit**

Edit an existing Run Command item and change its action to Submenu; save. Confirm its icon updates immediately to the folder icon without needing to reopen the dialog.

- [ ] **Step 6: Confirm the tray menu is unaffected**

Open the actual tray context menu (left-click the tray icon or right-click, per `Program.cs`) and confirm it looks exactly as before — no icons, group label band still renders correctly. This plan intentionally does not touch the tray menu.
