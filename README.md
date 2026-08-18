# CustomTools 🧰

**CustomTools** is a lightweight Windows system-tray utility with no main window: it lives as a single `NotifyIcon` and builds its entire context menu dynamically from **plugins**. Instead of a fixed set of features, the tray menu is whatever plugins you drop into its `Plugins/` folder.

---

## 🔄 How does it work?

The app is a thin host built around a plugin contract:

* **CustomTools** — the tray host (`WinExe`, entry point). On startup it scans the `Plugins/` folder next to the EXE, loads every plugin assembly it finds, and asks each one to contribute its own `ToolStripItem[]` to the tray menu.
* **CTPlugins** — the shared contract. A plugin is any class tagged `[CustomToolsPlugin(name, version)]` that implements `ICTPlugin`; the host never references a plugin implementation directly.
* **CTRegistryTree** — the reference plugin, and the one that ships today. It lets you build a nested tree of custom menu items, persisted under `HKCU\SOFTWARE\Appit\CustomTools\Items`.

Want to write your own? See **[PLUGINS.md](PLUGINS.md)** for a step-by-step guide.

---

## ✨ Key Features

* **Plugin Architecture:** drop a DLL implementing `ICTPlugin` into `Plugins/` and it's picked up automatically — no registration list to edit. See **[PLUGINS.md](PLUGINS.md)** for how to write your own.
* **Registry Tree Menu Builder:** build a nested tree of menu items that each run a command, open a URL, open a file, or act as a plain **submenu** container.
* **Built-in Editor:** a management dialog for the tree — Add, Edit, Remove, and reorder-by-nesting, all in one window.
* **Import / Export:** back up or share your entire menu tree as an XML file.
* **Localization:** English, Polish, German, Spanish, and Italian UI, switchable anytime from the Options dialog (or left to follow the OS default) — takes effect immediately, no restart needed.
* **Options dialog:** toggle "start with Windows" and pick the UI language.
* **About dialog:** version, author, and license info — the license is viewable in-app (bundled for offline use, refreshed from GitHub automatically when you're online) — plus a link to support the project.

---

## 🛠️ Installation & Requirements

CustomTools is built from source — there are no packaged releases yet.

1. Clone the repo and open `CustomTools.slnx` in Visual Studio (or build with MSBuild directly: `msbuild CustomTools.slnx /p:Configuration=Debug`).
2. Requires .NET Framework 4.8.
3. Run `CustomTools.exe` — the built `CTRegistryTree` plugin is picked up automatically from the `Plugins/` folder next to it.

---

## 📅 Roadmap

Planned / incomplete areas:

* **More plugins:** the plugin architecture is ready for more than just the registry-tree menu builder.

---

## ☕ Support & Appreciation

CustomTools is free and open-source. If this tool makes your life easier, consider supporting further development:

👉 **[Buy Me a Coffee](https://buycoffee.to/rico)**

---

## 🤝 Contributing

Have an idea for a new feature or found a bug?
1. Open a new **Issue**.
2. Submit a **Pull Request**.

Your help is always welcome!

---

## 📄 License

Copyright 2026 Adam Skowroński

Licensed under the GNU General Public License v3.0 — see [LICENSE.txt](LICENSE.txt) for the full text, and [NOTICE](NOTICE) for attribution.
