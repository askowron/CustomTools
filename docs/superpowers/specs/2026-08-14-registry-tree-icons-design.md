# Registry Tree Icons — Design

## Purpose

Add icons to the `CTRegistryTree` management-dialog `TreeView` (`tvItems` in `FrmManageItemsForm`) so each item's type (Run Command, Open URL, Open File, Submenu) is visually distinguishable at a glance, instead of relying solely on the item's text.

## Scope

- Icons appear in the management dialog's `TreeView` only.
- The real tray context menu is explicitly **out of scope** for this change: `Program.cs` sets `ContextMenuStrip.ShowImageMargin = false` and gives `CTRegistryTree`'s items a left `Margin` so `GroupLabelRenderer` can paint its rotated section-label band in that same space. Re-enabling an image margin there risks visually colliding with that band, and the result can't be verified without an interactive WinForms session — not a risk worth taking for a cosmetic feature. If icons in the tray menu are wanted later, it's a separate, explicitly-scoped follow-up that includes visual verification by the user.

## Icon source

A new static helper, `RegistryTreeIcons` (`CTRegistryTree/RegistryTreeIcons.cs`), resolves one 16×16 `Image` per `RegistryTreeItem.ActionType` value using the Windows shell's icon association lookup (`SHGetFileInfo` from `shell32.dll`, via P/Invoke) — the standard, well-established technique for getting a shell-standard icon without shipping any image assets or touching the filesystem:

- `RunCommand` → `SHGetFileInfo` on a fake `*.exe` path with `SHGFI_USEFILEATTRIBUTES` → the shell's generic executable icon.
- `OpenUrl` → fake `*.url` path → the shell's registered Internet Shortcut icon (globe).
- `OpenFile` → fake path with no extension → the shell's generic unknown-file icon.
- `Submenu` → `FILE_ATTRIBUTE_DIRECTORY` instead of a file extension → the standard Windows folder icon.

`SHGFI_USEFILEATTRIBUTES` means none of these paths need to exist — the shell resolves the icon purely from the extension/attribute, with no disk access. Results are cached in a static dictionary keyed by `ActionType`, so the P/Invoke call happens at most 4 times per process lifetime regardless of how many tree items exist or how often the dialog is reopened.

## Wiring into the TreeView

- `RegistryTreeItem`'s existing `explicit operator TreeNode(RegistryTreeItem item)` — the single conversion point already used by `LoadNodes` (initial load), `btnAdd_Click`, and `BuildImportedNode` (XML import) — sets `node.ImageKey = node.SelectedImageKey = item.Action.ToString()`. This means every code path that already creates a `TreeNode` from a `RegistryTreeItem` picks up the correct icon key automatically, with no per-call-site changes needed.
- `FrmManageItemsForm` builds a local `ImageList` (`ImageSize = 16x16`, `ColorDepth = Depth32Bit` for icon transparency) in its constructor, adding one image per `ActionType` keyed by the enum's `.ToString()` (matching the keys set above), and assigns it to `tvItems.ImageList`.
- The Root node (created directly in `LoadTree`, not via the conversion operator) gets the `Submenu` key too, so it shows a folder icon like any other container.
- `btnEdit_Click` doesn't go through the conversion operator — it mutates the existing selected node in place (`Text`, `Tag`). It needs an explicit `tvItems.SelectedNode.ImageKey = tvItems.SelectedNode.SelectedImageKey = form.Item.Action.ToString();` added alongside its existing `Text`/`Tag` updates, so editing an item's action type (e.g. switching it to `Submenu`) updates its icon too.

## Testing

No test project in this repo. Manual verification: open the manage-items dialog, confirm each of the 4 action types shows a distinct, recognizable icon (folder for Submenu, generic exe for Run Command, globe for Open URL, generic document for Open File) for newly added items, for items loaded from the registry on dialog open, for items brought in via Import, and that editing an item's action type updates its icon immediately.
