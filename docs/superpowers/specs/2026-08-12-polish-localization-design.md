# Polish localization

## Goal

Add Polish as a second UI language alongside English, using the standard .NET
satellite-resource-assembly mechanism. No new settings UI: the app follows
the OS's current UI culture automatically (Polish when Windows is set to
Polish, English otherwise).

## Architecture

Each of the three projects that currently has hardcoded UI text —
`CustomTools`, `CTPlugins`, `CTRegistryTree` — gets its own resource pair:

- `Strings.resx` — neutral culture, English, with Custom Tool set to
  `ResXFileCodeGenerator` (or `PublicResXFileCodeGenerator` to match the
  project's existing access-modifier convention) so each project gets a
  strongly-typed `Strings.SomeKey` accessor.
- `Strings.pl.resx` — Polish translations under the same keys, no code
  generator (data only).

One resx pair per project (not one shared file) because each project already
owns its own UI strings, and the host (`CustomTools`) never references the
plugin implementation (`CTRegistryTree`) directly — there's no single place
a shared resx could live without violating that boundary.

MSBuild recognizes the `.pl.resx` filename suffix and automatically builds a
satellite assembly `pl\<AssemblyName>.resources.dll` next to each project's
main output DLL. For `CTRegistryTree`, whose Debug `OutputPath` already
redirects into `CustomTools\bin\Debug\Plugins\`, the satellite lands at
`CustomTools\bin\Debug\Plugins\pl\CTRegistryTree.resources.dll`. No project
file changes are needed beyond adding the two `.resx` files — old-style
`Microsoft.Common.targets`-based csproj builds satellite assemblies from
culture-suffixed resx filenames without extra configuration.

`ResourceManager` (used internally by the generated `Strings` class)
resolves satellite assemblies relative to the main assembly's on-disk
location, which still works when the plugin DLL is loaded via
`Assembly.LoadFile` in `CTPlugins.FindPlugins()` — no change needed there.

No explicit language-switching code is added anywhere. `ResourceManager`
already picks the Polish satellite automatically when
`Thread.CurrentThread.CurrentUICulture` is Polish, and falls back to the
neutral English resx otherwise.

## String inventory

Neutral (English) resx normalizes strings that are currently hardcoded in
Polish; `Strings.pl.resx` restores the Polish text (and adds it for strings
that were only ever in English).

### CustomTools (`Properties\Strings.resx`)

| Key | English | Polish |
|---|---|---|
| TrayMenu_Options | Options | Opcje |
| TrayMenu_OptionsPlaceholder | Option 2 clicked | Kliknięto opcję 2 |
| TrayMenu_Exit | Exit | Zamknij |

The tray tooltip text `"Custom Tools"` (`Program.cs`) is the product name
and is not localized.

### CTPlugins (`Strings.resx`, used by `FrmTemplateDialog`)

| Key | English | Polish |
|---|---|---|
| Dialog_OK | OK | OK |
| Dialog_Cancel | Cancel | Anuluj |

### CTRegistryTree (`Strings.resx`)

| Key | English | Polish |
|---|---|---|
| Menu_Manage | ⚙ Manage | ⚙ Zarządzaj |
| Tree_Root | Root | Główny |
| Form_ManageItems_Title | Manage Items | Zarządzaj elementami |
| Button_Add | Add | Dodaj |
| Button_Edit | Edit | Edytuj |
| Button_Remove | Remove | Usuń |
| Form_ManageItem_Title | Add / Edit Item | Dodaj / Edytuj element |
| Label_Text | Text | Tekst |
| Label_Action | Action | Akcja |
| Label_Command | Command | Polecenie |
| Button_Find | Find | Znajdź |
| Button_Test | Test | Testuj |
| ActionType_RunCommand | Run command | Uruchom polecenie |
| ActionType_OpenUrl | Open URL | Otwórz URL |
| ActionType_OpenFile | Open file | Otwórz plik |

## ActionType dropdown

`FrmManageItemForm.cbAction` currently populates via
`Enum.GetNames(typeof(RegistryTreeItem.ActionType))`, showing raw enum
identifiers (`RunCommand`, `OpenUrl`, `OpenFile`) untranslated in both
languages today.

This changes to an ordered list of `(RegistryTreeItem.ActionType, string
displayText)` pairs built from the `ActionType_*` resx keys above, in enum
declaration order (`RunCommand`, `OpenUrl`, `OpenFile` — values 1, 2, 3).
The combo box is bound to display text only; the persisted registry value
stays the numeric `(int)ActionType` index, so existing saved items are
unaffected by the switch. The existing index math
(`cbAction.SelectedIndex + 1` in `BuildItem`,
`cbAction.SelectedIndex = (int)Item.Action - 1` in `SetItem`) is unchanged
since list order still matches enum value order.

## Out of scope

- No in-app language switcher — Windows' UI culture is the only control.
- No changes to the "Opcje" menu's wiring beyond translating its existing
  placeholder text; it remains the known-incomplete stub described in
  `CLAUDE.md`.
- Dynamic/runtime text (user-typed item names, exception messages, dialog
  titles built from `item.Text`) is not touched — only static UI chrome.
- `RegistryTreeItem` XML doc comments are not translated.
