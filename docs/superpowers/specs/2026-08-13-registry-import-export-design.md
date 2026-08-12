# Registry Tree Import/Export (XML) — Design

## Purpose

Let users back up and share their `CTRegistryTree` menu items by exporting the tree to an XML file, and restore/merge items by importing an XML file back in. Currently the only way to move items between machines is manual registry editing.

## Scope

- New `btnExport` / `btnImport` buttons on `FrmManageItemsForm`, alongside the existing Add/Edit/Remove buttons.
- Export always serializes the entire tree (not just a selected subtree).
- Import always adds new items — it never replaces or overwrites existing registry data.
- English + Polish localization for all new user-facing strings, per the project's existing localization pattern.

## Architecture

A new static class, `RegistryTreeXmlSerializer`, is added to the `CTRegistryTree` project. It owns all XML read/write logic, keeping `FrmManageItemsForm` focused on UI wiring (file dialogs, tree updates) rather than serialization details. It uses `System.Xml.Linq` (`XDocument`), already available in .NET Framework 4.8 — no new project references needed.

```
FrmManageItemsForm (UI: dialogs, tree updates, registry writes via existing SaveItem)
        |
        v
RegistryTreeXmlSerializer (pure XML <-> RegistryTreeItem tree conversion, no registry/UI access)
```

## XML schema

```xml
<RegistryTreeItems>
  <Item Text="Foo" Action="RunCommand" Command="notepad.exe">
    <Item Text="Bar" Action="OpenUrl" Command="https://example.com" />
  </Item>
</RegistryTreeItems>
```

- Root element: `RegistryTreeItems`.
- Each item is an `Item` element with `Text`, `Action`, `Command` attributes. Children are nested `Item` elements (mirrors the registry key nesting used internally).
- `Id` and `Path` are **not** serialized:
  - `Id` is always regenerated on import (fresh `Guid`), so importing the same file twice never collides with itself or with previously imported items.
  - `Path` is derived from tree position (parent path + own `Id`), same as everywhere else in the codebase.
- `Action` is serialized as the enum name (`RunCommand`, `OpenUrl`, `OpenFile`) rather than the numeric value, for human readability in the exported file. On import, an unrecognized or missing `Action` attribute falls back to `RunCommand`, matching the fallback already used in `RegistryTreeItem`'s `RegistryKey` conversion operator.
- Missing `Text`/`Command` attributes default to empty string, also matching existing conversion-operator behavior.

## Export flow

1. User clicks `btnExport`.
2. `SaveFileDialog` opens, filter `XML files (*.xml)|*.xml`, default file name `RegistryTreeItems.xml`.
3. `FrmManageItemsForm` walks the full `tvItems` tree starting from the root node's children (the root node itself is a UI-only placeholder with no `RegistryTreeItem` tag) and calls `RegistryTreeXmlSerializer.Export(rootTreeNodes)` to build an `XDocument`.
4. Document is saved to the chosen path.
5. On any I/O or serialization failure, a `MessageBox` reports the error (mirrors the try/catch + `MessageBox.Show` pattern in `CTRegistryTree.ExecuteAction`).

## Import flow

1. User selects a target node in the tree (or none, meaning root) and clicks `btnImport`.
2. `OpenFileDialog` opens, same XML filter.
3. `RegistryTreeXmlSerializer.Import(xmlContent)` parses the file into a tree of transient `RegistryTreeItem` objects (fresh `Guid`s, `Path` left unset — the caller fills it in, since path depends on where the import target is in the live tree).
4. `FrmManageItemsForm` recursively walks the parsed tree: for each node, sets `Path` to `{parentPath}/{item.Id}`, writes it to the registry via the existing `(RegistryKey)item` conversion (reusing `SaveItem`), and adds a corresponding `TreeNode` under the target parent node in `tvItems`.
5. The target parent node is expanded so imported items are immediately visible.
6. On any parse or I/O failure (malformed XML, missing root element, unreadable file), nothing is written — the error is caught before any registry writes begin, and a `MessageBox` reports the failure. Partial imports are not possible because parsing (which can fail) is fully separated from writing (which doesn't fail under normal conditions).

## Error handling

Both flows wrap their file access and (de)serialization in try/catch, showing `MessageBox.Show(exc.Message, ..., MessageBoxButtons.OK, MessageBoxIcon.Warning)` — consistent with the existing error-handling style in `CTRegistryTree.ExecuteAction`. Import validates the root element name (`RegistryTreeItems`) and rejects the file with a clear error if it doesn't match, rather than silently importing nothing.

## Localization

New resx keys added to `Strings.resx` and `Strings.pl.resx`, following the existing `Button_*`/`Error_*` naming pattern:

- `Button_Import` — "Import"
- `Button_Export` — "Export"
- `Dialog_XmlFilter` — "XML files (*.xml)|*.xml"
- `Error_ExportFailed` — error message shown on export failure
- `Error_ImportFailed` — error message shown on import failure

## Testing

No test project exists in this repo (per `CLAUDE.md`). Verification is manual:

- Export a tree with nested folders/leaf items, inspect the resulting XML.
- Import that file into a fresh selection (root) and into a non-root node; confirm items appear under the right parent with new IDs, and registry keys are created correctly (spot-check with `regedit` under `HKCU\SOFTWARE\Appit\CustomTools\Items`).
- Import a malformed XML file and confirm a clean error message with no partial writes.
- Confirm existing items are untouched by an import (no overwrite/replace).
