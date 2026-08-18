# Installer and auto-update

## Goal

Today CustomTools has no packaged release — installing means cloning the
repo, building, and manually copying files/`Plugins/` into place (per
`README.md`'s "there are no packaged releases yet"). This adds:

1. A Windows installer (`Setup.exe`) that puts CustomTools into
   `Program Files`, with a Start Menu shortcut and an uninstall entry.
2. A GitHub Actions workflow that builds and publishes that installer to a
   GitHub Release whenever a version tag is pushed.
3. An in-app update checker that runs on startup and once per day
   thereafter, and — when enabled (default: on) — notifies via a tray
   balloon tip if a newer release is available, with a small dialog to
   install it (one UAC prompt), postpone, or skip that version.

## Architecture

### Installer (`installer/CustomTools.iss`, new)

An [Inno Setup](https://jrsoftware.org/isinfo.php) script, compiled with
`ISCC.exe` (Inno Setup Compiler — a build-time tool, not part of
MSBuild/.NET, must be installed separately or provisioned in CI).

Key `[Setup]` directives:
- `AppId` — a fixed GUID, so Inno Setup recognizes upgrades of the same
  product rather than treating each install as new.
- `AppName=CustomTools`, `AppVersion=<passed in via /D from the release
  workflow>` (see below).
- `DefaultDirName={autopf}\CustomTools` (`Program Files` / `Program Files
  (x86)` resolved by Inno Setup for the machine's bitness).
- `PrivilegesRequired=admin` — matches the explicit ask to land in Program
  Files; the app doesn't need admin at runtime, only at install/update time.
- `AppMutex=CustomToolsSingleInstance` — Inno Setup checks this named
  mutex before install; if held (app running), it closes the app
  automatically (`CloseApplications=yes`, the default) and relaunches it
  after install (`RestartApplications=yes`, the default) since it detects
  the mutex existed. `Program.cs` creates this mutex at startup (see
  below) purely so the installer can detect a running instance — it is
  *not* used as a single-instance guard for any other purpose.
- `OutputBaseFilename=CustomToolsSetup`.
- `[Files]`: `CustomTools\bin\Release\CustomTools.exe`, its dependencies,
  and `CustomTools\bin\Release\Plugins\*` (recursesubdirs) → `{app}` /
  `{app}\Plugins`.
- `[Icons]`: one Start Menu shortcut, no desktop shortcut (matches this
  being a tray-only app you set to start with Windows via its own Options
  checkbox, not something you'd normally launch from a Start Menu icon
  repeatedly — but the shortcut is still useful for first launch and for
  users who prefer to launch manually).
- `[Run]`: launch `CustomTools.exe` post-install, unchecked-by-default
  when *not* in silent/update mode (`Flags: nowait postinstall skipifsilent`)
  — silent updates instead rely on `RestartApplications` to relaunch the
  instance that was closed.

Silent/update invocation (used by the in-app updater, see below):
```
CustomToolsSetup.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS
```

### `Program.cs` — app mutex

Adds, near the top of `Main()`:
```csharp
using var appMutex = new System.Threading.Mutex(initiallyOwned: false, "CustomToolsSingleInstance");
```
Held for the process lifetime (not explicitly acquired/released — its
mere existence is what the installer's `AppMutex` check detects). This is
the only change `Program.cs` needs besides wiring up the update checker
(below).

### `UpdateChecker` (new, `CustomTools/UpdateChecker.cs`)

Static class, host-only (like `StartupManager`), not part of the
`CTPlugins` contract.

```csharp
internal static class UpdateChecker
{
    private const string LatestReleaseApiUrl =
        "https://api.github.com/repos/askowron/CustomTools/releases/latest";

    // Starts the startup check (fire-and-forget) and arms a 24h recurring
    // timer for as long as the process lives. Call once from Program.cs.
    public static void StartBackgroundChecking(NotifyIcon trayIcon);

    // Core check: fetch latest release, compare versions, show a balloon
    // tip if newer and not skipped. Swallows all exceptions. Always
    // stamps UpdateLastCheckUtc, success or failure.
    private static async Task CheckAsync(NotifyIcon trayIcon);
}
```

- Registry state, all under the existing `HKCU\SOFTWARE\Appit\CustomTools`
  key (same key `LanguageManager`/`StartupManager` already use):
  - `CheckForUpdates` (DWORD, default `1` when absent — enabled by default
    per the request).
  - `UpdateLastCheckUtc` (string, round-trip `"o"`-format UTC timestamp).
  - `UpdateSkippedVersion` (string, empty/absent by default).
- "Due" means `CheckForUpdates != 0` AND (`UpdateLastCheckUtc` absent OR
  more than 24h in the past). Checked at startup and re-evaluated every
  time the 24h timer fires (so the timer doesn't need its own enabled
  check duplicated — `CheckAsync` is the single gate).
- `CheckAsync`:
  1. `HttpClient.GetStringAsync` (same `TimeSpan.FromSeconds(4)` timeout
     pattern as `FrmLicense.TryDownloadLatestLicenseAsync`) against
     `LatestReleaseApiUrl`. GitHub's API requires a `User-Agent` header on
     all requests — set to `"CustomTools-UpdateChecker"`.
  2. Parse the JSON response with `System.Web.Script.Serialization
     .JavaScriptSerializer` (ships with the .NET Framework 4.8 GAC as part
     of `System.Web.Extensions`, no new NuGet package — but
     `CustomTools.csproj` needs a new `<Reference Include="System.Web.Extensions" />`
     added, it doesn't reference it today) to pull `tag_name` and the
     `browser_download_url` of the asset named `CustomToolsSetup.exe` out
     of the `assets` array.
  3. Strip a leading `v`/`V` from `tag_name`, parse both it and the
     running `AssemblyInformationalVersion` with `Version.Parse` after
     normalizing to at least `major.minor` (`Version.Parse` requires at
     least two components).
  4. If remote > local AND remote-version-string != `UpdateSkippedVersion`:
     set `trayIcon.BalloonTipTitle/Text` and call `ShowBalloonTip`, storing
     the discovered version + download URL in two static fields
     `UpdateChecker.AvailableVersion` / `AvailableDownloadUrl` that
     `FrmUpdateAvailable` reads when opened from the balloon click.
  5. Always writes `UpdateLastCheckUtc = DateTime.UtcNow` in a `finally`,
     whether steps 1-4 succeeded or an exception was caught — a single
     offline launch shouldn't cause the next several launches to all retry
     immediately once back online; it waits out the normal 24h cadence.
  6. Any exception (network, parse, missing asset) is caught and swallowed
     — this runs unattended in the background, same as the license
     refetch; a failed check must be invisible.

### `FrmUpdateAvailable` (new, `CustomTools/FrmUpdateAvailable.cs` + `.Designer.cs`)

A plain `Form` (not `FrmTemplateDialog` — that base is shaped for
OK/Cancel, this dialog has three distinct actions, not a
confirm/discard pair). Contents:
- Label: current version (`AssemblyInformationalVersion`) vs. new version
  (`UpdateChecker.AvailableVersion`).
- LinkLabel: opens the GitHub release page
  (`https://github.com/askowron/CustomTools/releases/tag/v{version}`) via
  `Process.Start`, same pattern as `FrmAbout`'s author/support links.
- Three buttons:
  - **Update now** (`btnUpdateNow`): downloads `AvailableDownloadUrl` to
    `Path.GetTempPath()` via `HttpClient` (no progress UI — installers for
    a small WinForms app are a few MB, expected to be fast; if this proves
    too slow in practice a progress bar can be added later, not needed for
    v1), then `Process.Start` with `UseShellExecute = true`, `Verb =
    "runas"`, `Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART
    /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS"`. Any failure here (UAC
    denied, download failed) shows a `MessageBox` with the error — this is
    the one place in the update flow that surfaces an error, since it's a
    direct result of a user click, not a background action. On success,
    closes the dialog (the installer closes/relaunches the app itself via
    `AppMutex`/`RestartApplications` — no explicit `Application.Exit()`
    call needed or wanted here, since that could race the installer's own
    detection).
  - **Remind me later** (`btnRemindLater`): just closes the dialog. No
    registry write — the next due check (next startup, or next 24h timer
    tick) re-evaluates and re-shows the balloon if still applicable.
  - **Skip this version** (`btnSkipVersion`): writes
    `UpdateSkippedVersion = UpdateChecker.AvailableVersion`, closes the
    dialog.

`trayIcon.BalloonTipClicked` opens this form (`new
FrmUpdateAvailable().Show()` — non-modal is fine, it's user-initiated from
a balloon, doesn't need to block anything).

### `FrmOptions` changes

Adds, alongside the existing `chkStartWithWindows`:
- `chkCheckForUpdates` (CheckBox), constructor sets `.Checked` from
  `UpdateChecker.IsEnabled` (new public bool property on `UpdateChecker`
  mirroring the `CheckForUpdates` registry read, same pattern as
  `StartupManager.IsEnabled`). `OKClicked` handler calls
  `UpdateChecker.SetEnabled(chkCheckForUpdates.Checked)`.
- `btnCheckNow` (Button): calls `UpdateChecker.StartBackgroundChecking`'s
  underlying `CheckAsync` immediately (a new public `public static
  Task CheckNowAsync(NotifyIcon)` entry point — `Program.cs` passes the
  tray icon reference into `FrmOptions`'s constructor so this button and
  the balloon-tip path share the same `NotifyIcon` instance), regardless
  of the "due" 24h gate — an explicit click is always due. Shows a small
  inline "No update available" / "Checking failed" label if the check
  finds nothing, since a manual click deserves *some* feedback even though
  the background version stays silent on failure.

### `Program.cs` wiring

```csharp
UpdateChecker.StartBackgroundChecking(trayIcon);
```
called once, right after `trayIcon.Visible = true`. And:
```csharp
trayIcon.BalloonTipClicked += (s, e) => new FrmUpdateAvailable().Show();
```

### GitHub Actions workflow (`.github/workflows/release.yml`, new)

Triggers on tag push matching `v*` (matches the tag-push flow you already
follow per CLAUDE.md's version-bump steps: bump
`AssemblyInformationalVersion` in the three `AssemblyInfo.cs` files + the
`Program.cs` tooltip, commit, `git tag v0.3 && git push --tags`).

Steps (windows-latest runner, since this is a .NET Framework/WinForms
build):
1. Checkout.
2. Extract version from the tag (`${GITHUB_REF#refs/tags/v}`).
3. Locate MSBuild (`microsoft/setup-msbuild` action).
4. `msbuild CustomTools.slnx /p:Configuration=Release`.
5. Install Inno Setup (`choco install innosetup` — Chocolatey is
   preinstalled on `windows-latest` runners) and run `ISCC.exe
   installer\CustomTools.iss /DMyAppVersion=<extracted version>`.
6. Create a GitHub Release for the pushed tag (`softprops/action-gh-release`
   or `gh release create`) and upload
   `installer\Output\CustomToolsSetup.exe` as its asset.

The workflow does **not** verify the tag's version matches
`AssemblyInformationalVersion` in the `AssemblyInfo.cs` files — that stays
a manual discipline step per the existing documented release process, same
as today. (A future improvement could add that check; out of scope here.)

## Data flow

**Update check (background):**
1. App starts → `UpdateChecker.StartBackgroundChecking` → due? → `CheckAsync`.
2. `CheckAsync` hits GitHub API, compares versions, on newer-and-not-skipped
   shows a balloon tip, always stamps `UpdateLastCheckUtc`.
3. 24h `Timer` re-fires `CheckAsync` for the life of the process (tray apps
   commonly run for days).

**User acts on a balloon:**
1. Click balloon → `FrmUpdateAvailable` opens, showing cached
   `UpdateChecker.AvailableVersion`/`AvailableDownloadUrl` (no re-fetch).
2. Update now → download → elevated silent installer run → installer
   detects `AppMutex`, closes the running app, replaces files, relaunches.
3. Remind later → dialog closes, no state change.
4. Skip → `UpdateSkippedVersion` written, dialog closes.

**Release publishing:**
1. Developer bumps version in 3×`AssemblyInfo.cs` + `Program.cs` tooltip
   (existing documented process), commits, tags `vX.Y`, pushes the tag.
2. Workflow builds Release config, compiles the installer with that
   version, publishes a GitHub Release with `CustomToolsSetup.exe`
   attached.
3. Any running CustomTools instance with update-checking enabled
   discovers this within 24h (or immediately via "Check now").

## Error handling

- Background check: every exception swallowed (network, parse, missing
  asset in the release payload) — see `UpdateChecker.CheckAsync` above.
  Never a MessageBox, never a crash; worst case, no balloon appears.
- Manual "Check now": failure shown as a small inline status label in
  `FrmOptions`, not a blocking MessageBox — it's a secondary action inside
  a dialog the user is already looking at.
- "Update now" download/elevation/launch failure: shown as a `MessageBox`
  in `FrmUpdateAvailable` — the one flow where an error should be visible,
  since it's the direct result of the user asking to install something.
- Installer itself: Inno Setup's own error handling (disk space, locked
  files it can't close via `AppMutex`, etc.) — standard Inno Setup
  behavior, not something this design adds logic for.

## Testing

No test project exists in this repo (per `CLAUDE.md`). Manual verification:

1. Compile the installer locally (`ISCC installer\CustomTools.iss
   /DMyAppVersion=0.3-test`) against a Release build; run it on a clean(ish)
   machine/VM — confirm Program Files install, Start Menu shortcut,
   Add/Remove Programs entry, and first-run launch.
2. With that version installed and running, publish a real (or draft) GitHub
   Release with a higher tag + `CustomToolsSetup.exe` asset attached; use
   Options → "Check now" to confirm the balloon appears without waiting a
   day.
3. Click the balloon → confirm `FrmUpdateAvailable` shows correct
   current/new version numbers and the release-notes link opens the right
   GitHub page.
4. Click "Update now" → confirm the UAC prompt appears, the running tray
   icon disappears during install, and CustomTools relaunches automatically
   post-install at the new version (check About dialog).
5. Repeat the check with "Remind me later" → confirm the balloon reappears
   on next "Check now" / next startup.
6. Repeat with "Skip this version" → confirm the balloon does *not*
   reappear for that same version, but does reappear once an even newer
   release is published.
7. Toggle Options → "Automatically check for updates" off → confirm no
   balloon appears on restart even with a newer release available, and that
   "Check now" still works on demand regardless of the checkbox (manual
   checks aren't gated by the enable/disable toggle).
8. Push a real version tag on a test branch/fork if possible, or at least
   dry-run the workflow file's steps locally, to confirm the Actions
   workflow produces a working `CustomToolsSetup.exe` attached to the
   release.

## Out of scope

- Code signing for `CustomToolsSetup.exe` — it'll trigger Windows
  SmartScreen warnings on first run; acceptable for now for an open-source
  hobby project, same trust level as cloning-and-building already had.
- Download progress UI for the installer download — small file, add later
  if it proves necessary.
- Verifying tag version == `AssemblyInformationalVersion` in CI — stays a
  manual discipline step, as noted above.
- Delta/differential updates — every update downloads the full installer.
- Any per-machine (HKLM) install mode or silent unattended *first* install
  — first install is always the normal Inno Setup UI wizard; only updates
  triggered from within the app run silently.
- Rollback/downgrade UI — the update flow only ever offers moving forward
  to a newer version; manually running an older `Setup.exe` still works
  (Inno Setup doesn't block downgrades by default) but isn't a flow this
  design surfaces in-app.
