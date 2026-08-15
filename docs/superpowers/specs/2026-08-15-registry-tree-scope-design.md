# Registry Tree Item Scope (CurrentUser / LocalMachine) — Design

## Purpose

Let each `CTRegistryTree` item be created either for the current user only (`HKCU`, today's only option) or for all users of the machine (`HKLM`, admin-only to write). Scope is chosen per item in the Add/Edit dialog, independently of its parent's scope. In the tray menu, LocalMachine items are listed before CurrentUser items, at every menu level.

## Scope

- Add a per-item `Scope` (CurrentUser / LocalMachine), settable and editable in `FrmManageItemForm`, defaulting to LocalMachine being disabled unless the process is elevated.
- Change on-disk storage from nested subkeys (physical path = logical tree path) to flat per-hive entries keyed by the item's own Id, linked by an explicit `ParentId`. This is required because a child's scope can now differ from its parent's, and a single registry key cannot span two hives.
- Update the tray menu builder and the manage-items tree to read both hives, merge them by `ParentId`, and order siblings LocalMachine-first at every level.
- Gate writes to `HKLM` behind an elevation check; disable the relevant UI when not elevated rather than attempting and failing.
- Extend XML import/export to carry `Scope`, defaulting to CurrentUser for files exported before this change.
- No migration of existing `HKCU` data from the old nested-key format — it's abandoned; users re-create items as needed.

Out of scope: self-elevation (relaunching the tray app as admin), a "move item + all descendants" bulk operation beyond what falls out of the flat model naturally, and any change to how the group label / vertical band rendering (`GroupLabelRenderer`) works.

## Data model

### `RegistryTreeItem.cs`

- Add `public enum Scope { CurrentUser = 1, LocalMachine = 2 }`.
- Add `public Guid ParentId { get; set; }` (`Guid.Empty` = root-level item).
- Add `public Scope ItemScope { get; set; }` (named `ItemScope` to avoid colliding with `System.ComponentModel.ISite.Scope`-style ambiguity and reads clearly as `item.ItemScope`).
- Remove `Path`.
- `RegistryKey → RegistryTreeItem` conversion:
  - Reads `Id`, `ParentId` (parsed as `Guid`, defaults to `Guid.Empty` if missing/invalid — treated as root), `Name`, `Action`, `Command` as today.
  - `ItemScope` is derived from `key.Name`: starts with `"HKEY_LOCAL_MACHINE"` → `Scope.LocalMachine`, else `Scope.CurrentUser`. Not read from a stored value — the hive a key physically lives in *is* its scope, so there's nothing to desync.
- `RegistryTreeItem → RegistryKey` conversion:
  - Picks the root hive from `item.ItemScope` (`Registry.LocalMachine` or `Registry.CurrentUser`).
  - Creates/opens `{ROOT}\{Items}\{item.Id}` under that hive (flat — no path segments).
  - Sets `Id`, `ParentId` (`item.ParentId.ToString()`), `Name`, `Action`, `Command`.
- `TreeNode ↔ RegistryTreeItem` conversions unchanged in shape (still tag-based), but the `TreeNode`'s `Text` gains a scope suffix — see Manage Items tree below.

## Reading & menu building

### `CTRegistryTree.cs`

`InitializeItems` additionally ensures `HKLM\{ROOT}\{Items}` exists when running elevated; when not elevated it just skips creating it (nothing to write) — reading a missing key already returns `null`/empty subkeys, which the loader below already treats as "no LocalMachine items yet".

`LoadItems` changes from a single-hive recursive registry walk to:

1. Read all direct value-bearing keys under `HKCU\{ROOT}\{Items}` and, if present, `HKLM\{ROOT}\{Items}` (each is now a flat list of Id-named keys, not nested) into one `List<RegistryTreeItem>`.
2. Build `Dictionary<Guid, List<RegistryTreeItem>>` grouping by `ParentId`.
3. Recursively build `ToolStripItem[]` starting from `ParentId == Guid.Empty`, same leaf/container logic as today except "is a container" is now `childrenByParent.ContainsKey(item.Id) || item.Action == ActionType.Submenu` (replacing the `SubKeyCount > 0` check, since there's no physical nesting to count).
4. At each level, order the sibling list stably: all `LocalMachine` items first (in their existing relative order), then all `CurrentUser` items (in their existing relative order) — a stable `OrderBy(i => i.ItemScope == Scope.LocalMachine ? 0 : 1)`.
5. Items whose `ParentId` doesn't match any loaded item's `Id` (orphans — e.g. parent was deleted while this item's own hive was unreachable) are treated as root-level rather than silently dropped.

`ExecuteAction` and `SplitCommand` are unchanged.

Add `internal static bool IsElevated()` (via `new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)`), used by both forms.

## Manage Items tree

### `FrmManageItemsForm.cs`

- `LoadTree`/`LoadNodes` replaced by the same merge-by-`ParentId` + LM-first stable ordering used for the live menu, so the editor tree visually matches what the tray shows.
- Each `TreeNode`'s displayed text gets a scope suffix appended (not stored — computed at display time): `" (LM)"` for LocalMachine, nothing for CurrentUser (the common case stays visually quiet; only the less-common LocalMachine items are flagged). Reuses the muted-gray style already used for the "Manage" menu entry (`Color.FromArgb(100, 100, 100)`) applied to the suffix... WinForms `TreeNode.Text` can't mix colors within one node, so instead the whole node's `ForeColor` is set to that gray only when `ItemScope == LocalMachine`, keeping CurrentUser nodes at the default color. Text suffix `" (LM)"` still included for colorblind-safe / non-visual clarity (e.g. copy-paste, screen readers).
- `btnAdd_Click`: `ParentId` for the new item = selected node's item `Id`, or `Guid.Empty` if the root pseudo-node is selected (same selection logic as today, just capturing `Id` instead of `Path`).
- `btnAdd`/`btnEdit`/`btnRemove` enablement (`tvItems_AfterSelect` plus a re-check after tree mutations):
  - `btnEdit` and `btnRemove` disabled additionally when the selected item's `ItemScope == LocalMachine` and `!IsElevated()`.
  - `btnRemove` additionally disabled when any descendant (recursive, regardless of its own scope) has `ItemScope == LocalMachine` and `!IsElevated()` — prevents deleting a CurrentUser parent while stranding an undeletable LocalMachine child.
  - `btnAdd` is not scope-gated itself (adding a CurrentUser child under any parent is always allowed); the *dialog's* LocalMachine option is what's gated (see below).
- `RemoveItem` becomes recursive over the in-memory `ParentId` map instead of `DeleteSubKeyTree`: delete all descendants first (each from its own hive, by `Id`), then delete the item's own key from its own hive.
- `SaveItem` unchanged in shape (`using (var key = (RegistryKey)item) { }`) — the conversion operator now picks the hive from `ItemScope`.
- Editing an item whose `ItemScope` changed (via the dialog): `SaveItem` writes to the new hive location (new key, same Id); the old location's key must then be deleted explicitly (`btnEdit_Click` deletes the pre-edit key from its original hive when `form.Item.ItemScope` differs from the original item's `ItemScope`, before calling `SaveItem`). Children are unaffected since they reference `ParentId` (a GUID value), not a physical path.

## Add/Edit dialog

### `FrmManageItemForm.cs` / `.Designer.cs`

- New `GroupBox` "Zakres" (Scope) containing two `RadioButton`s: `rbCurrentUser` ("Bieżący użytkownik") and `rbLocalMachine` ("Wszyscy użytkownicy (ten komputer)"), placed above the existing OK/Cancel row, pushing dialog height down accordingly.
- `rbLocalMachine.Enabled = CTRegistryTree.IsElevated()`. If not elevated, only `rbCurrentUser` is selectable.
- Constructor for a **new** item (`FrmManageItemForm(RegistryTreeItem parentItem)` — parameter changes from `string path` to the parent `RegistryTreeItem` itself, or `null` for root): default scope = parent's `ItemScope` if usable (CurrentUser always usable; LocalMachine only if elevated), else `CurrentUser`.
- Constructor for **editing** an existing item: `rbLocalMachine.Checked`/`rbCurrentUser.Checked` set from `item.ItemScope`. (Reaching this dialog for a LocalMachine item already implies elevation, per the manage-tree gating above, so the existing selection is always legal.)
- `BuildItem`: sets `item.ItemScope = rbLocalMachine.Checked ? Scope.LocalMachine : Scope.CurrentUser`; sets `item.ParentId` from the constructor's parent reference (or `Guid.Empty`) instead of deriving `Path`.

## Import / Export

### `RegistryTreeXmlSerializer.cs`

- `ExportNode` adds `new XAttribute("Scope", (item?.ItemScope ?? Scope.CurrentUser).ToString())`.
- `ImportElement` reads a `Scope` attribute the same defensive way `Action` is read: missing or unparseable → `Scope.CurrentUser`.
- `RegistryTreeImportedItem` gains a `Scope` property, threaded through the constructor.

### `FrmManageItemsForm.btnImport_Click` / `BuildImportedNode`

- If an imported item's `Scope` is `LocalMachine` and `!CTRegistryTree.IsElevated()`, it's silently downgraded to `CurrentUser` before saving (import proceeds; nothing fails).
- `BuildImportedNode` sets `ParentId` from the target node's item `Id` (or `Guid.Empty` for the tree root) instead of building a `Path` string.

## Localization

New `Strings.resx` / `Strings.pl.resx` keys, following the existing pattern:

- `Label_Scope` — "Scope" / "Zakres"
- `Scope_CurrentUser` — "Current user" / "Bieżący użytkownik"
- `Scope_LocalMachine` — "All users (this computer)" / "Wszyscy użytkownicy (ten komputer)"

Plus the matching generated properties in `Strings.Designer.cs`.

## Testing

No test project in this repo. Manual verification:

- Add a CurrentUser item at root; confirm it appears in the tray menu and in the manage tree with no suffix.
- Running elevated: add a LocalMachine item at root; confirm it appears in the tray menu *before* the CurrentUser item, and shows the " (LM)" suffix / gray color in the manage tree.
- Running elevated: add a LocalMachine child under a CurrentUser parent, and a CurrentUser child under a LocalMachine parent; confirm both nest correctly and each level orders LM before CU among its own siblings.
- Running non-elevated: confirm the Add/Edit dialog's LocalMachine radio is disabled; confirm Edit/Remove are disabled for existing LocalMachine items in the manage tree; confirm Remove is disabled for a CurrentUser item that has a LocalMachine descendant.
- Running elevated: edit an existing CurrentUser item, flip it to LocalMachine, save; confirm it moved (gone from HKCU, present in HKLM, tray menu reflects new position/ordering), and any of its children still resolve correctly.
- Remove a parent with mixed-scope descendants while elevated; confirm the whole subtree is gone from both hives.
- Export a tree with mixed scopes, re-import it into a fresh area; confirm scopes round-trip. Import an old (pre-Scope) export file; confirm everything imports as CurrentUser.
- Non-elevated: import a file containing a LocalMachine-scoped item; confirm it's imported as CurrentUser rather than failing.
