# CustomTools

A Windows system-tray utility with no main window: it lives as a `NotifyIcon` and builds its context menu dynamically from plugins.

## Architecture

The solution is split into three projects that form a plugin host/plugin-API/plugin-implementation chain:

- **CustomTools** (`WinExe`, entry point) — the tray host. References `CTPlugins` only, never plugin implementations directly.
- **CTPlugins** (`Library`) — the plugin contract shared by the host and every plugin: the `ICTPlugin` interface, the `CustomToolsPluginAttribute` used to mark discoverable plugin classes, and `CTPlugins.FindPlugins()`, which scans the `Plugins/` folder next to the host EXE for plugin assemblies at startup.
- **CTRegistryTree** (`Library`) — a plugin that lets you build a tree of custom menu items (run a command, open a URL, open a file, or a plain submenu), persisted under `HKCU\SOFTWARE\Appit\CustomTools\Items`. Items can be exported to and imported from XML.

New plugins are added by referencing `CTPlugins`, implementing `ICTPlugin`, and tagging the class with `[CustomToolsPlugin(name, version)]`.

## Build

.NET Framework 4.8 / WinForms, using the `.slnx` solution format. Build with MSBuild or Visual Studio:

```
msbuild CustomTools.slnx /p:Configuration=Debug
```

There is no `dotnet build` support (classic csproj) and no test project in this repo.

## License

Licensed under the GNU General Public License v3.0 — see [LICENSE.txt](LICENSE.txt).

See [NOTICE](NOTICE) for attribution.
