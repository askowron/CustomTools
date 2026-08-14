# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build

.NET Framework 4.8 / WinForms solution using the new `.slnx` format (`CustomTools.slnx`), no `.sln` file. Build with MSBuild or Visual Studio — there is no `dotnet build` support (classic `Microsoft.CSharp.targets`-based csproj, not SDK-style).

```
msbuild CustomTools.slnx /p:Configuration=Debug
```

There are no test projects and no lint config in this repo.

`CTRegistryTree` builds its output directly into `CustomTools/bin/Debug/Plugins/` (see its `OutputPath` in Debug config) so the host app picks it up as a plugin without a manual copy step. Release builds do not have this redirect — output goes to the project's own `bin\Release\`, so it won't be discovered by the host in a Release run unless copied to `Plugins/` manually.

## Architecture

CustomTools is a Windows system-tray application (`CustomTools/Program.cs`) with no main window: it creates a `NotifyIcon` and builds its context menu dynamically from plugins. The solution is split into three projects that form a plugin host/plugin-API/plugin-implementation chain:

- **CustomTools** (`WinExe`, entry point) — the tray host. References `CTPlugins` only, never plugin implementations directly.
- **CTPlugins** (`Library`) — the plugin contract, shared by the host and every plugin:
  - `ICTPlugin` — interface every plugin implements (`Name`, `GetMenuItems()` returning `ToolStripItem[]`).
  - `CustomToolsPluginAttribute(name, version)` — marks a class as a discoverable plugin.
  - `CTPlugins.FindPlugins()` — scans the `Plugins/` folder next to the host EXE, `Assembly.LoadFile`s every `*.dll`, reflects over types for the attribute, and `Activator.CreateInstance`s each as `ICTPlugin`. This is how new plugin assemblies get picked up — no registration list to edit.
  - `FrmTemplateDialog` — base dialog form with `OKClicked`/`CancelClicked` events; plugin dialogs (e.g. `CTRegistryTree`'s manage-item forms) derive from this instead of `Form` directly.
- **CTRegistryTree** (`Library`) — the one real plugin, and the reference implementation for writing new ones. Persists its menu items under `HKCU\SOFTWARE\Appit\CustomTools\Items` and rebuilds `ToolStripItem[]` from that registry key each time `GetMenuItems()` is called. `RegistryTreeItem` defines explicit conversion operators to/from `RegistryKey` and `TreeNode` to move data between the registry, the model, and the WinForms `TreeView` UI in `FrmManageItemsForm`/`FrmManageItemForm`.

To add a new plugin: create a `Library` project referencing `CTPlugins`, implement `ICTPlugin`, tag the class with `[CustomToolsPlugin(name, version)]`, and set its Debug `OutputPath` to `..\CustomTools\bin\Debug\Plugins\` (matching `CTRegistryTree`'s pattern) so the host discovers it.

Known incomplete areas: the host's "Opcje" menu item is not wired to `FrmOptions` (just shows a placeholder `MessageBox`).
