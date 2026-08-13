# Registry Tree Submenu Support — Design

## Purpose

Let users explicitly create a submenu (a pure container item with no runnable action) in `CTRegistryTree`, instead of the current workaround of creating a normal Run Command/Open URL/Open File item and only having it become a submenu once it happens to gain a child. The workaround forces filling in a meaningless Command value and leaves a stale, misleading Action type on what's really just a folder.

## Scope

- Add a `Submenu` action type, selectable in the existing Add/Edit item dialog (`FrmManageItemForm`) — no new dialog, no new button (per the approved design choice).
- Update the tray menu builder (`CTRegistryTree.BuildMenuItems`) so an item explicitly typed `Submenu` always renders as a (possibly empty, disabled) submenu container rather than a clickable leaf.
- Preserve full backward compatibility: any existing item that already has children continues to render as a submenu regardless of its own `Action` value, exactly as today.

## Changes

### `RegistryTreeItem.cs`

Add a fourth enum value:

```csharp
public enum ActionType
{
    RunCommand = 1,
    OpenUrl = 2,
    OpenFile = 3,
    Submenu = 4
}
```

No other changes — the registry conversion operators already persist/read `Action` as a plain `int`, so `Submenu` needs no special-casing there.

### `CTRegistryTree.cs` — `BuildMenuItems`

Currently:

```csharp
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
```

Becomes: an item is a container if it has children **or** is explicitly typed `Submenu`. A `Submenu`-typed item with no children yet renders as a disabled placeholder (nothing to click, nothing to run) rather than a leaf that would try to execute an empty command:

```csharp
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
```

### `FrmManageItemForm.cs`

- `actionOrder` gets `RegistryTreeItem.ActionType.Submenu` appended as the 4th entry, preserving the documented `cbAction.SelectedIndex + 1 == (int)ActionType` mapping.
- `GetActionDisplayText` gets a `case ActionType.Submenu: return Properties.Strings.ActionType_Submenu;`.
- A new `cbAction.SelectedIndexChanged` handler enables/disables `tbCommand`, `button1` (Find), and `btnTest` together: disabled when the selected action is `Submenu`, enabled otherwise. Called once after `PopulateActionItems()`/`SetItem()` to set the initial state too.
- `BuildItem` sets `item.Command = tbCommand.Text` as today; when the field is disabled its text is simply whatever was last in it (typically empty for a new item) — no special-casing needed since a disabled, unused `Command` value on a `Submenu` item is harmless (never read by `BuildMenuItems` for a container).

### Localization

New resx key `ActionType_Submenu` in `Strings.resx` ("Submenu") and `Strings.pl.resx` ("Podmenu"), plus the corresponding generated property in `Strings.Designer.cs` — following the exact pattern of the existing `ActionType_RunCommand`/`OpenUrl`/`OpenFile` entries.

## Testing

No test project in this repo. Manual verification:

- Add a new item, select "Submenu" as its action, confirm the Command/Find/Test fields disable immediately on selection.
- Save it with no children yet; confirm it appears in the tray menu as a grayed-out, non-clickable entry.
- Add a child item under it; confirm it becomes a normal clickable submenu with that child inside.
- Confirm an existing item that already has children (created before this feature, still typed `RunCommand`) still renders as a working submenu — no regression.
- Edit a `Submenu`-typed item back to e.g. `RunCommand`, confirm the Command field re-enables and the item becomes a clickable leaf again (once it has no children) or stays a submenu (if it still has children, per the "children win" rule).
