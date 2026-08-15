# Start-with-Windows option

## Goal

Replace the tray icon's "Opcje"/"Options" menu placeholder (currently just a
`MessageBox` per `CLAUDE.md`'s "known incomplete areas") with a real Options
dialog. For now the dialog holds exactly one control: a checkbox that enables
or disables launching CustomTools automatically when Windows starts. The
dialog is built to hold more options later without changing its shape.

## Architecture

### `FrmOptions` (`CustomTools/FrmOptions.cs` + `.Designer.cs`)

Currently an empty designer stub (`Form1`, no controls). It changes to
derive from `CTPlugins.FrmTemplateDialog` instead of `Form` directly — the
same base the `CTRegistryTree` dialogs use — which supplies OK/Cancel
buttons and `OKClicked`/`CancelClicked` events for free, so `FrmOptions`
doesn't need to reinvent dialog chrome.

Contents:
- One `CheckBox`, `chkStartWithWindows`, label from a new resx string.
- Constructor sets `chkStartWithWindows.Checked = StartupManager.IsEnabled`.
- Subscribes to `OKClicked`: calls
  `StartupManager.SetEnabled(chkStartWithWindows.Checked)`.
- No handling needed on `CancelClicked` — the checkbox's in-memory state is
  simply discarded, matching the OK/Cancel dialogs `CTRegistryTree` already
  uses elsewhere in the app.

### `StartupManager` (new, `CustomTools/StartupManager.cs`)

A static class, internal to `CustomTools` (not part of the `CTPlugins`
contract — this is host-only behavior, not plugin behavior):

```csharp
internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "CustomTools";

    public static bool IsEnabled { get; }   // reads HKCU Run value, compares to current exe path
    public static void SetEnabled(bool enabled); // writes or deletes the value
}
```

- `IsEnabled` opens `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
  read-only, reads the `CustomTools` value, and returns whether it equals
  the quoted current `Application.ExecutablePath`. Missing key or missing
  value both mean "not enabled" (no exception).
- `SetEnabled(true)` opens/creates the Run key writable and sets
  `CustomTools` to `"\"" + Application.ExecutablePath + "\""`.
- `SetEnabled(false)` opens the Run key writable and deletes the
  `CustomTools` value if present (`DeleteValue(name, throwOnMissingValue:
  false)`).
- HKCU is always writable by the owning user — no elevation, no admin
  rights, no failure mode worth catching beyond what .NET already does by
  default (an unexpected exception here is as unexpected as any other HKCU
  write elsewhere in the app, e.g. `CTRegistryTree`'s existing HKCU writes,
  which also aren't wrapped).

Comparing against the *current* exe path (not just checking value
presence) means a CustomTools.exe that has moved (e.g. a rebuild to a
different bin path) shows the checkbox unchecked rather than falsely
claiming it's still enabled — `SetEnabled(true)` on next OK click then
overwrites the stale path.

### `Program.cs`

`RebuildMenu`'s Options menu item handler changes from:

```csharp
menu.Items.Add(Strings.TrayMenu_Options, null, (s, e) => MessageBox.Show(Strings.TrayMenu_OptionsPlaceholder));
```

to:

```csharp
menu.Items.Add(Strings.TrayMenu_Options, null, (s, e) => new FrmOptions().ShowDialog());
```

### Localization

`CustomTools/Properties/Strings.resx` (English, neutral) and
`Strings.pl.resx` (Polish) both get:

| Key | English | Polish |
|---|---|---|
| Options_StartWithWindows | Start with Windows | Uruchamiaj z systemem Windows |
| Options_Title | Options | Opcje |

`TrayMenu_OptionsPlaceholder` is removed from both resx files — it becomes
dead code once the real dialog replaces the `MessageBox` call, and nothing
else references it.

`FrmOptions`'s `Text` (dialog title) is set to `Strings.Options_Title` in
its constructor.

## Data flow

1. Tray menu → "Options" clicked → `new FrmOptions().ShowDialog()`.
2. `FrmOptions` constructor reads `StartupManager.IsEnabled`, sets checkbox.
3. User toggles checkbox, clicks OK (or Cancel).
4. On OK: `FrmTemplateDialog.btnOK_Click` fires `OKClicked` →
   `StartupManager.SetEnabled(checked-state)` → HKCU Run key updated →
   dialog closes.
5. On Cancel: dialog closes, no registry write.
6. Next time the dialog opens, it reflects whatever is actually in the
   registry (step 2), so external changes (e.g. the user manually editing
   the Run key) are picked up correctly rather than trusting stale UI
   state.

## Error handling

No new error handling beyond what's described above (defensive read,
best-effort write). This matches the existing style of registry access
elsewhere in the codebase (`CTRegistryTree`) — HKCU is not expected to fail
for a per-user autostart entry.

## Testing

No test project exists in this repo (per `CLAUDE.md`). Manual verification
after building:

1. Run `CustomTools.exe`, open the tray menu, click Options — dialog shows
   with checkbox unchecked (fresh install, no Run entry yet).
2. Check the box, click OK. Confirm
   `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\CustomTools` exists
   and points at the running exe's path.
3. Reopen Options — checkbox shows checked.
4. Uncheck, click OK. Confirm the Run value is gone.
5. Check, click Cancel. Confirm no Run value was written (Cancel discards).
6. Switch Windows UI culture to Polish, confirm the dialog title and
   checkbox label are in Polish.

## Out of scope

- No other options besides start-with-windows — this is explicitly a
  "for now, just this one" request; the dialog shape (OK/Cancel via
  `FrmTemplateDialog`) is chosen so more options can be added later without
  restructuring it.
- No startup-folder-shortcut alternative — HKCU Run key only, per the
  approved design choice.
- No elevation / HKLM Run key (all-users autostart) — this is a per-user
  setting only, consistent with the app having no admin-mode concept for
  itself (unlike `CTRegistryTree`'s HKLM item support).
- No migration/cleanup of any pre-existing Run entries a user may have
  created manually under a different value name.
