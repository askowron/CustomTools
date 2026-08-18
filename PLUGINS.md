# Writing a CustomTools Plugin

CustomTools has no built-in features of its own beyond the tray icon and the
Options/About dialogs — everything you see in the context menu comes from a
**plugin**. This guide walks through building one from scratch, using the
`CTRegistryTree` project (shipped in this repo) as the reference
implementation throughout.

---

## 1. The contract

A plugin is a `Library` (class library) project that:

1. References `CTPlugins.csproj`.
2. Contains a public class implementing `ICTPlugin`:

   ```csharp
   // CTPlugins/ICTPlugin.cs
   public interface ICTPlugin
   {
       string Name { get; }
       ToolStripItem[] GetMenuItems();
   }
   ```

3. Tags that class with `[CustomToolsPlugin(name, version)]` so the host can
   find it via reflection:

   ```csharp
   [CustomToolsPlugin("My Plugin", "1.0.0")]
   public class MyPlugin : ICTPlugin
   {
       public string Name => "My Plugin";

       public ToolStripItem[] GetMenuItems()
       {
           return new ToolStripItem[]
           {
               new ToolStripMenuItem("Say hello", null, (s, e) =>
                   MessageBox.Show("Hello from MyPlugin!"))
           };
       }
   }
   ```

That's the entire contract. The host (`CustomTools/Program.cs`) never
references your plugin's assembly or types directly — it only ever talks to
`ICTPlugin`.

### How discovery works

At startup, `CTPlugins.FindPlugins()` (`CTPlugins/CTPlugins.cs`) scans the
`Plugins/` folder next to `CustomTools.exe`, `Assembly.LoadFrom`s every
`*.dll` in it, reflects over the types for the `CustomToolsPluginAttribute`,
and `Activator.CreateInstance`s each match. There is no registration list to
edit — dropping a compatible DLL into `Plugins/` is enough.

### How the menu gets (re)built

`GetMenuItems()` is called **every time the tray menu is opened**
(`ContextMenuStrip.Opening` in `Program.cs`), not just once at startup. This
is intentional — it's what lets `CTRegistryTree` show registry-backed
changes without restarting the app. Two consequences for your plugin:

- Rebuild your `ToolStripItem[]` fresh from whatever your source of truth is
  (registry, config file, in-memory state) on every call — don't cache stale
  items.
- Keep it fast. It runs synchronously on the UI thread right before the menu
  is shown.

---

## 2. Project setup

1. Create a new **Class Library** project (old-style, non-SDK `.csproj`,
   targeting **.NET Framework 4.8** — this repo has no SDK-style projects and
   no `dotnet` tooling).
2. Add a project reference to `CTPlugins.csproj`.
3. In the Debug configuration, set:

   ```xml
   <OutputPath>..\CustomTools\bin\Debug\Plugins\</OutputPath>
   ```

   This makes your plugin's DLL land directly in the host's `Plugins/`
   folder on every Debug build, so `CustomTools.exe` picks it up with no
   manual copy step — exactly what `CTRegistryTree.csproj` does. **Release**
   builds don't get this redirect; output goes to your project's own
   `bin\Release\`, and you'll need to copy the DLL (and any satellite
   resource folders, see below) into `Plugins/` yourself for a Release run to
   find it.
4. Add the new project to `CustomTools.slnx`.

---

## 3. Menu item grouping (optional, but recommended)

If your plugin contributes more than a couple of items, stamp a shared
`Tag` on every `ToolStripItem` you return — the host installs a
`GroupLabelRenderer` (`CTPlugins/GroupLabelRenderer.cs`) on the tray's
`ContextMenuStrip` that turns a contiguous run of items sharing the same
non-null string `Tag` into a labeled section, with a small rotated caption
drawn in the menu's left margin. `CTRegistryTree` does this with its own
`Name`:

```csharp
foreach (var item in items)
{
    item.Tag = Name;
}
```

You don't have to do this — ungrouped items just render as plain rows — but
it's what makes multiple plugins visually distinguishable in one menu.

---

## 4. Dialogs

If your plugin needs a settings/management window, derive it from
`FrmTemplateDialog` (`CTPlugins/FrmTemplateDialog.cs`) instead of `Form`
directly. It already wires up OK/Cancel buttons docked to the bottom and
exposes `OKClicked`/`CancelClicked` events:

```csharp
public partial class MyPluginDialog : FrmTemplateDialog
{
    public MyPluginDialog()
    {
        InitializeComponent();
        OKClicked += delegate { /* persist whatever changed */ };
    }
}
```

See `CTRegistryTree/FrmManageItemsForm.cs` / `FrmManageItemForm.cs` for a
full example, including a nested child dialog.

---

## 5. Localization

Every project in this repo ships a neutral `Properties/Strings.resx` plus
satellite translations for `pl`, `de`, `es`, and `it`; .NET picks the
culture-matching one automatically. Your plugin should follow the same
pattern:

1. Add every user-facing string as a key in `Properties/Strings.resx`
   (neutral/English).
2. Copy the same key set into `Strings.pl.resx`, `Strings.de.resx`,
   `Strings.es.resx`, `Strings.it.resx` with translated values (or just the
   English text as a placeholder — a missing satellite value falls back to
   the neutral one, it's just not translated).
3. Reference strings in code as `Properties.Strings.MyKey`.

**Important:** `Strings.Designer.cs` (the strongly-typed accessor class) is
generated by Visual Studio's `ResXFileCodeGenerator` at *design time* —
`msbuild`/CLI builds do **not** regenerate it. Every time you add a key to
the neutral `.resx`, you must also hand-add the matching property to
`Strings.Designer.cs` (or open the `.resx` in Visual Studio once to let it
regenerate). See any existing `Strings.Designer.cs` in this repo for the
exact boilerplate shape.

The app-wide language picker in Options (`CustomTools/LanguageManager.cs`)
sets `Thread.CurrentThread.CurrentUICulture` at runtime, so as long as your
plugin reads `Properties.Strings.*` fresh each time (which `GetMenuItems()`
being called on every menu open naturally gives you), it'll follow the
user's chosen language with no extra work on your part.

---

## 6. Checklist

- [ ] New `Library` project, `TargetFrameworkVersion` = `v4.8`, references `CTPlugins.csproj`.
- [ ] A class implementing `ICTPlugin`, tagged `[CustomToolsPlugin(name, version)]`.
- [ ] Debug `OutputPath` set to `..\CustomTools\bin\Debug\Plugins\`.
- [ ] `GetMenuItems()` rebuilds fresh from your data source and runs fast.
- [ ] (Optional) shared `Tag` on your items for `GroupLabelRenderer` grouping.
- [ ] (Optional) dialogs derive from `FrmTemplateDialog`.
- [ ] Strings localized via `Properties/Strings*.resx`, `Strings.Designer.cs` hand-updated.
- [ ] Added to `CustomTools.slnx`.

For a complete, working reference, read through `CTRegistryTree/` end to
end — it exercises every point above.
