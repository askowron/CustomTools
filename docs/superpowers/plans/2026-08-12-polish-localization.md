# Polish Localization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Polish as a second UI language for CustomTools, alongside a normalized English baseline, using standard .NET satellite resource assemblies — no code changes needed to pick the language, it follows the OS's current UI culture automatically.

**Architecture:** Each of the three projects (`CustomTools`, `CTPlugins`, `CTRegistryTree`) gets its own `Properties\Strings.resx` (neutral culture, English, strongly-typed via `ResXFileCodeGenerator`) and `Properties\Strings.pl.resx` (Polish translations, same keys, no code generator). MSBuild's built-in satellite-assembly support (already active in these old-style csproj files, no extra config needed) turns `Strings.pl.resx` into a `pl\<AssemblyName>.resources.dll` next to each project's output DLL. `ResourceManager.GetString(key, null)` — what the generated `Strings.SomeKey` properties call — resolves to the Polish satellite automatically when `Thread.CurrentThread.CurrentUICulture` is Polish, and to the neutral English resx otherwise.

**Tech Stack:** .NET Framework 4.8, WinForms, MSBuild satellite resource assemblies (`ResXFileCodeGenerator`, `System.Resources.ResourceManager`). No test framework exists in this repo (confirmed in `CLAUDE.md`); verification here is (a) `msbuild` compiling cleanly and (b) a PowerShell reflection smoke check that reads the generated string properties under both the default and Polish `CurrentUICulture`, which stands in for a unit test since there's no test project to add one to.

## Global Constraints

- Target framework: .NET Framework 4.8 (`v4.8`), old-style (non-SDK) csproj — no `dotnet build`, only `msbuild`.
- Build command: `msbuild CustomTools.slnx /p:Configuration=Debug` (from repo root).
- `CTRegistryTree`'s Debug `OutputPath` is `..\CustomTools\bin\Debug\Plugins\` — its satellite assembly must land at `..\CustomTools\bin\Debug\Plugins\pl\CTRegistryTree.resources.dll`, not next to the project's own `bin\Debug\`.
- Neutral (`Strings.resx`) values are English; `Strings.pl.resx` values are Polish. Exact key/value pairs are listed per task below — do not invent new keys or reword the given translations.
- No in-app language switcher, no changes to `Program.cs`'s "Opcje" wiring beyond translating its existing placeholder text, no changes to dynamic/runtime text (user-typed item names, exception messages, `MessageBox` titles built from `item.Text`).
- Access modifier for every generated `Strings` class matches the existing `CustomTools.Properties.Resources` class: `internal`.

---

## Task 1: CustomTools tray menu strings

**Files:**
- Create: `CustomTools\Properties\Strings.resx`
- Create: `CustomTools\Properties\Strings.pl.resx`
- Create: `CustomTools\Properties\Strings.Designer.cs`
- Modify: `CustomTools\CustomTools.csproj`
- Modify: `CustomTools\Program.cs:63,65`

**Interfaces:**
- Produces: `CustomTools.Properties.Strings.TrayMenu_Options`, `CustomTools.Properties.Strings.TrayMenu_OptionsPlaceholder`, `CustomTools.Properties.Strings.TrayMenu_Exit` — all `internal static string` — for `Program.cs` to consume.

- [ ] **Step 1: Create the neutral (English) resx**

Create `CustomTools\Properties\Strings.resx`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>2.0</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <data name="TrayMenu_Options" xml:space="preserve">
    <value>Options</value>
  </data>
  <data name="TrayMenu_OptionsPlaceholder" xml:space="preserve">
    <value>Option 2 clicked</value>
  </data>
  <data name="TrayMenu_Exit" xml:space="preserve">
    <value>Exit</value>
  </data>
</root>
```

- [ ] **Step 2: Create the Polish resx**

Create `CustomTools\Properties\Strings.pl.resx`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>2.0</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <data name="TrayMenu_Options" xml:space="preserve">
    <value>Opcje</value>
  </data>
  <data name="TrayMenu_OptionsPlaceholder" xml:space="preserve">
    <value>Kliknięto opcję 2</value>
  </data>
  <data name="TrayMenu_Exit" xml:space="preserve">
    <value>Zamknij</value>
  </data>
</root>
```

- [ ] **Step 3: Create the strongly-typed accessor**

Create `CustomTools\Properties\Strings.Designer.cs`:

```csharp
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CustomTools.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Strings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings() {
        }
        
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("CustomTools.Properties.Strings", typeof(Strings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        internal static string TrayMenu_Options {
            get {
                return ResourceManager.GetString("TrayMenu_Options", resourceCulture);
            }
        }
        
        internal static string TrayMenu_OptionsPlaceholder {
            get {
                return ResourceManager.GetString("TrayMenu_OptionsPlaceholder", resourceCulture);
            }
        }
        
        internal static string TrayMenu_Exit {
            get {
                return ResourceManager.GetString("TrayMenu_Exit", resourceCulture);
            }
        }
    }
}
```

- [ ] **Step 4: Wire the new files into the csproj**

In `CustomTools\CustomTools.csproj`, find this block (currently at lines 66-70):

```xml
    <Compile Include="Properties\Resources.Designer.cs">
      <AutoGen>True</AutoGen>
      <DependentUpon>Resources.resx</DependentUpon>
      <DesignTime>True</DesignTime>
    </Compile>
```

Replace it with:

```xml
    <Compile Include="Properties\Resources.Designer.cs">
      <AutoGen>True</AutoGen>
      <DependentUpon>Resources.resx</DependentUpon>
      <DesignTime>True</DesignTime>
    </Compile>
    <EmbeddedResource Include="Properties\Strings.resx">
      <Generator>ResXFileCodeGenerator</Generator>
      <LastGenOutput>Strings.Designer.cs</LastGenOutput>
      <SubType>Designer</SubType>
    </EmbeddedResource>
    <Compile Include="Properties\Strings.Designer.cs">
      <AutoGen>True</AutoGen>
      <DependentUpon>Strings.resx</DependentUpon>
      <DesignTime>True</DesignTime>
    </Compile>
    <EmbeddedResource Include="Properties\Strings.pl.resx">
      <DependentUpon>Strings.resx</DependentUpon>
    </EmbeddedResource>
```

- [ ] **Step 5: Use the strings in Program.cs**

In `CustomTools\Program.cs`, replace:

```csharp
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Opcje", null, (s, e) => MessageBox.Show("Kliknięto opcję 2"));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Zamknij", null, (s, e) => Application.Exit());
```

with:

```csharp
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Strings.TrayMenu_Options, null, (s, e) => MessageBox.Show(Strings.TrayMenu_OptionsPlaceholder));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Strings.TrayMenu_Exit, null, (s, e) => Application.Exit());
```

(`Program.cs` already has `using CustomTools.Properties;` at line 3, so `Strings` resolves without a new `using`. `Resources` and `Strings` are different classes in the same namespace, so there's no collision.)

- [ ] **Step 6: Build and verify**

Run: `msbuild CustomTools.slnx /p:Configuration=Debug /t:CustomTools`

Expected: `Build succeeded`, and `CustomTools\bin\Debug\pl\CustomTools.resources.dll` exists (the satellite assembly for the strings we just added).

- [ ] **Step 7: Commit**

```bash
git add CustomTools/Properties/Strings.resx CustomTools/Properties/Strings.pl.resx CustomTools/Properties/Strings.Designer.cs CustomTools/CustomTools.csproj CustomTools/Program.cs
git commit -m "Localize CustomTools tray menu strings (English/Polish)"
```

---

## Task 2: CTPlugins dialog button strings

**Files:**
- Create: `CTPlugins\Properties\Strings.resx`
- Create: `CTPlugins\Properties\Strings.pl.resx`
- Create: `CTPlugins\Properties\Strings.Designer.cs`
- Modify: `CTPlugins\CTPlugins.csproj`
- Modify: `CTPlugins\FrmTemplateDialog.Designer.cs:55,66`

**Interfaces:**
- Produces: `CTPlugins.Properties.Strings.Dialog_OK`, `CTPlugins.Properties.Strings.Dialog_Cancel` — `internal static string` — for `FrmTemplateDialog` (and any future dialog derived from it) to consume. Since `FrmManageItemsForm`/`FrmManageItemForm` in `CTRegistryTree` derive from `FrmTemplateDialog` but don't touch its OK/Cancel button text directly, no other task depends on this.

- [ ] **Step 1: Create the neutral (English) resx**

Create `CTPlugins\Properties\Strings.resx`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>2.0</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <data name="Dialog_OK" xml:space="preserve">
    <value>OK</value>
  </data>
  <data name="Dialog_Cancel" xml:space="preserve">
    <value>Cancel</value>
  </data>
</root>
```

- [ ] **Step 2: Create the Polish resx**

Create `CTPlugins\Properties\Strings.pl.resx`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>2.0</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <data name="Dialog_OK" xml:space="preserve">
    <value>OK</value>
  </data>
  <data name="Dialog_Cancel" xml:space="preserve">
    <value>Anuluj</value>
  </data>
</root>
```

- [ ] **Step 3: Create the strongly-typed accessor**

Create `CTPlugins\Properties\Strings.Designer.cs`:

```csharp
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CTPlugins.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Strings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings() {
        }
        
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("CTPlugins.Properties.Strings", typeof(Strings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        internal static string Dialog_OK {
            get {
                return ResourceManager.GetString("Dialog_OK", resourceCulture);
            }
        }
        
        internal static string Dialog_Cancel {
            get {
                return ResourceManager.GetString("Dialog_Cancel", resourceCulture);
            }
        }
    }
}
```

- [ ] **Step 4: Wire the new files into the csproj**

In `CTPlugins\CTPlugins.csproj`, find this line (currently line 58):

```xml
    <Compile Include="Properties\AssemblyInfo.cs" />
```

Replace it with:

```xml
    <Compile Include="Properties\AssemblyInfo.cs" />
    <Compile Include="Properties\Strings.Designer.cs">
      <AutoGen>True</AutoGen>
      <DependentUpon>Strings.resx</DependentUpon>
      <DesignTime>True</DesignTime>
    </Compile>
```

Then find this block (currently lines 61-65):

```xml
  <ItemGroup>
    <EmbeddedResource Include="FrmTemplateDialog.resx">
      <DependentUpon>FrmTemplateDialog.cs</DependentUpon>
    </EmbeddedResource>
  </ItemGroup>
```

Replace it with:

```xml
  <ItemGroup>
    <EmbeddedResource Include="FrmTemplateDialog.resx">
      <DependentUpon>FrmTemplateDialog.cs</DependentUpon>
    </EmbeddedResource>
    <EmbeddedResource Include="Properties\Strings.resx">
      <Generator>ResXFileCodeGenerator</Generator>
      <LastGenOutput>Strings.Designer.cs</LastGenOutput>
      <SubType>Designer</SubType>
    </EmbeddedResource>
    <EmbeddedResource Include="Properties\Strings.pl.resx">
      <DependentUpon>Strings.resx</DependentUpon>
    </EmbeddedResource>
  </ItemGroup>
```

- [ ] **Step 5: Use the strings in FrmTemplateDialog.Designer.cs**

In `CTPlugins\FrmTemplateDialog.Designer.cs`, replace:

```csharp
            this.btnCancel.Text = "Cancel";
```

with:

```csharp
            this.btnCancel.Text = CTPlugins.Properties.Strings.Dialog_Cancel;
```

And replace:

```csharp
            this.btnAccept.Text = "OK";
```

with:

```csharp
            this.btnAccept.Text = CTPlugins.Properties.Strings.Dialog_OK;
```

(Fully qualifying `CTPlugins.Properties.Strings` avoids adding a `using` to the designer file, matching how every other type in that file is already fully qualified, e.g. `System.Windows.Forms.Button`.)

- [ ] **Step 6: Build and verify**

Run: `msbuild CustomTools.slnx /p:Configuration=Debug /t:CTPlugins`

Expected: `Build succeeded`, and `CTPlugins\bin\Debug\pl\CTPlugins.resources.dll` exists.

- [ ] **Step 7: Commit**

```bash
git add CTPlugins/Properties/Strings.resx CTPlugins/Properties/Strings.pl.resx CTPlugins/Properties/Strings.Designer.cs CTPlugins/CTPlugins.csproj CTPlugins/FrmTemplateDialog.Designer.cs
git commit -m "Localize CTPlugins template dialog buttons (English/Polish)"
```

---

## Task 3: CTRegistryTree plugin strings and the ActionType dropdown

**Files:**
- Create: `CTRegistryTree\Properties\Strings.resx`
- Create: `CTRegistryTree\Properties\Strings.pl.resx`
- Create: `CTRegistryTree\Properties\Strings.Designer.cs`
- Modify: `CTRegistryTree\CTRegistryTree.csproj`
- Modify: `CTRegistryTree\CTRegistryTree.cs:56`
- Modify: `CTRegistryTree\FrmManageItemsForm.cs:19`
- Modify: `CTRegistryTree\FrmManageItemsForm.Designer.cs:46,60,72,84,99`
- Modify: `CTRegistryTree\FrmManageItemForm.cs:1-34`
- Modify: `CTRegistryTree\FrmManageItemForm.Designer.cs:48,57,82,101,111,134`

**Interfaces:**
- Consumes: nothing from Task 1/2 (this project doesn't reference `CustomTools.Properties.Strings` or `CTPlugins.Properties.Strings`).
- Produces: `CTRegistryTree.Properties.Strings.{Menu_Manage, Tree_Root, Form_ManageItems_Title, Button_Add, Button_Edit, Button_Remove, Form_ManageItem_Title, Label_Text, Label_Action, Label_Command, Button_Find, Button_Test, ActionType_RunCommand, ActionType_OpenUrl, ActionType_OpenFile}` — all `internal static string`.

- [ ] **Step 1: Create the neutral (English) resx**

Create `CTRegistryTree\Properties\Strings.resx`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>2.0</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <data name="Menu_Manage" xml:space="preserve">
    <value>⚙ Manage</value>
  </data>
  <data name="Tree_Root" xml:space="preserve">
    <value>Root</value>
  </data>
  <data name="Form_ManageItems_Title" xml:space="preserve">
    <value>Manage Items</value>
  </data>
  <data name="Button_Add" xml:space="preserve">
    <value>Add</value>
  </data>
  <data name="Button_Edit" xml:space="preserve">
    <value>Edit</value>
  </data>
  <data name="Button_Remove" xml:space="preserve">
    <value>Remove</value>
  </data>
  <data name="Form_ManageItem_Title" xml:space="preserve">
    <value>Add / Edit Item</value>
  </data>
  <data name="Label_Text" xml:space="preserve">
    <value>Text</value>
  </data>
  <data name="Label_Action" xml:space="preserve">
    <value>Action</value>
  </data>
  <data name="Label_Command" xml:space="preserve">
    <value>Command</value>
  </data>
  <data name="Button_Find" xml:space="preserve">
    <value>Find</value>
  </data>
  <data name="Button_Test" xml:space="preserve">
    <value>Test</value>
  </data>
  <data name="ActionType_RunCommand" xml:space="preserve">
    <value>Run command</value>
  </data>
  <data name="ActionType_OpenUrl" xml:space="preserve">
    <value>Open URL</value>
  </data>
  <data name="ActionType_OpenFile" xml:space="preserve">
    <value>Open file</value>
  </data>
</root>
```

- [ ] **Step 2: Create the Polish resx**

Create `CTRegistryTree\Properties\Strings.pl.resx`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>2.0</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <data name="Menu_Manage" xml:space="preserve">
    <value>⚙ Zarządzaj</value>
  </data>
  <data name="Tree_Root" xml:space="preserve">
    <value>Główny</value>
  </data>
  <data name="Form_ManageItems_Title" xml:space="preserve">
    <value>Zarządzaj elementami</value>
  </data>
  <data name="Button_Add" xml:space="preserve">
    <value>Dodaj</value>
  </data>
  <data name="Button_Edit" xml:space="preserve">
    <value>Edytuj</value>
  </data>
  <data name="Button_Remove" xml:space="preserve">
    <value>Usuń</value>
  </data>
  <data name="Form_ManageItem_Title" xml:space="preserve">
    <value>Dodaj / Edytuj element</value>
  </data>
  <data name="Label_Text" xml:space="preserve">
    <value>Tekst</value>
  </data>
  <data name="Label_Action" xml:space="preserve">
    <value>Akcja</value>
  </data>
  <data name="Label_Command" xml:space="preserve">
    <value>Polecenie</value>
  </data>
  <data name="Button_Find" xml:space="preserve">
    <value>Znajdź</value>
  </data>
  <data name="Button_Test" xml:space="preserve">
    <value>Testuj</value>
  </data>
  <data name="ActionType_RunCommand" xml:space="preserve">
    <value>Uruchom polecenie</value>
  </data>
  <data name="ActionType_OpenUrl" xml:space="preserve">
    <value>Otwórz URL</value>
  </data>
  <data name="ActionType_OpenFile" xml:space="preserve">
    <value>Otwórz plik</value>
  </data>
</root>
```

- [ ] **Step 3: Create the strongly-typed accessor**

Create `CTRegistryTree\Properties\Strings.Designer.cs`:

```csharp
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CTRegistryTree.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Strings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings() {
        }
        
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("CTRegistryTree.Properties.Strings", typeof(Strings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        internal static string Menu_Manage {
            get {
                return ResourceManager.GetString("Menu_Manage", resourceCulture);
            }
        }
        
        internal static string Tree_Root {
            get {
                return ResourceManager.GetString("Tree_Root", resourceCulture);
            }
        }
        
        internal static string Form_ManageItems_Title {
            get {
                return ResourceManager.GetString("Form_ManageItems_Title", resourceCulture);
            }
        }
        
        internal static string Button_Add {
            get {
                return ResourceManager.GetString("Button_Add", resourceCulture);
            }
        }
        
        internal static string Button_Edit {
            get {
                return ResourceManager.GetString("Button_Edit", resourceCulture);
            }
        }
        
        internal static string Button_Remove {
            get {
                return ResourceManager.GetString("Button_Remove", resourceCulture);
            }
        }
        
        internal static string Form_ManageItem_Title {
            get {
                return ResourceManager.GetString("Form_ManageItem_Title", resourceCulture);
            }
        }
        
        internal static string Label_Text {
            get {
                return ResourceManager.GetString("Label_Text", resourceCulture);
            }
        }
        
        internal static string Label_Action {
            get {
                return ResourceManager.GetString("Label_Action", resourceCulture);
            }
        }
        
        internal static string Label_Command {
            get {
                return ResourceManager.GetString("Label_Command", resourceCulture);
            }
        }
        
        internal static string Button_Find {
            get {
                return ResourceManager.GetString("Button_Find", resourceCulture);
            }
        }
        
        internal static string Button_Test {
            get {
                return ResourceManager.GetString("Button_Test", resourceCulture);
            }
        }
        
        internal static string ActionType_RunCommand {
            get {
                return ResourceManager.GetString("ActionType_RunCommand", resourceCulture);
            }
        }
        
        internal static string ActionType_OpenUrl {
            get {
                return ResourceManager.GetString("ActionType_OpenUrl", resourceCulture);
            }
        }
        
        internal static string ActionType_OpenFile {
            get {
                return ResourceManager.GetString("ActionType_OpenFile", resourceCulture);
            }
        }
    }
}
```

- [ ] **Step 4: Wire the new files into the csproj**

In `CTRegistryTree\CTRegistryTree.csproj`, find this line (currently line 58):

```xml
    <Compile Include="Properties\AssemblyInfo.cs" />
```

Replace it with:

```xml
    <Compile Include="Properties\AssemblyInfo.cs" />
    <Compile Include="Properties\Strings.Designer.cs">
      <AutoGen>True</AutoGen>
      <DependentUpon>Strings.resx</DependentUpon>
      <DesignTime>True</DesignTime>
    </Compile>
```

Then find this block (currently lines 68-75):

```xml
  <ItemGroup>
    <EmbeddedResource Include="FrmManageItemForm.resx">
      <DependentUpon>FrmManageItemForm.cs</DependentUpon>
    </EmbeddedResource>
    <EmbeddedResource Include="FrmManageItemsForm.resx">
      <DependentUpon>FrmManageItemsForm.cs</DependentUpon>
    </EmbeddedResource>
  </ItemGroup>
```

Replace it with:

```xml
  <ItemGroup>
    <EmbeddedResource Include="FrmManageItemForm.resx">
      <DependentUpon>FrmManageItemForm.cs</DependentUpon>
    </EmbeddedResource>
    <EmbeddedResource Include="FrmManageItemsForm.resx">
      <DependentUpon>FrmManageItemsForm.cs</DependentUpon>
    </EmbeddedResource>
    <EmbeddedResource Include="Properties\Strings.resx">
      <Generator>ResXFileCodeGenerator</Generator>
      <LastGenOutput>Strings.Designer.cs</LastGenOutput>
      <SubType>Designer</SubType>
    </EmbeddedResource>
    <EmbeddedResource Include="Properties\Strings.pl.resx">
      <DependentUpon>Strings.resx</DependentUpon>
    </EmbeddedResource>
  </ItemGroup>
```

- [ ] **Step 5: Use the strings in CTRegistryTree.cs**

In `CTRegistryTree\CTRegistryTree.cs`, replace:

```csharp
            var manageItem = new ToolStripMenuItem("⚙ Zarządzaj", null, delegate {
```

with:

```csharp
            var manageItem = new ToolStripMenuItem(Properties.Strings.Menu_Manage, null, delegate {
```

- [ ] **Step 6: Use the strings in FrmManageItemsForm.cs**

In `CTRegistryTree\FrmManageItemsForm.cs`, replace:

```csharp
            var rootNode = new TreeNode("Root");
```

with:

```csharp
            var rootNode = new TreeNode(Properties.Strings.Tree_Root);
```

- [ ] **Step 7: Use the strings in FrmManageItemsForm.Designer.cs**

In `CTRegistryTree\FrmManageItemsForm.Designer.cs`, replace:

```csharp
            treeNode1.Name = "Root";
            treeNode1.Text = "Root";
```

with:

```csharp
            treeNode1.Name = "Root";
            treeNode1.Text = Properties.Strings.Tree_Root;
```

Replace:

```csharp
            this.btnAdd.Text = "Add";
```

with:

```csharp
            this.btnAdd.Text = Properties.Strings.Button_Add;
```

Replace:

```csharp
            this.btnEdit.Text = "Edit";
```

with:

```csharp
            this.btnEdit.Text = Properties.Strings.Button_Edit;
```

Replace:

```csharp
            this.btnRemove.Text = "Remove";
```

with:

```csharp
            this.btnRemove.Text = Properties.Strings.Button_Remove;
```

Replace:

```csharp
            this.Text = "Manage Items";
```

with:

```csharp
            this.Text = Properties.Strings.Form_ManageItems_Title;
```

(`treeNode1.Name` stays the literal `"Root"` — it's an internal WinForms node identifier, not displayed text.)

- [ ] **Step 8: Use the strings in FrmManageItemForm.Designer.cs**

In `CTRegistryTree\FrmManageItemForm.Designer.cs`, replace:

```csharp
            this.label2.Text = "Text";
```

with:

```csharp
            this.label2.Text = Properties.Strings.Label_Text;
```

Replace:

```csharp
            this.label1.Text = "Action";
```

with:

```csharp
            this.label1.Text = Properties.Strings.Label_Action;
```

Replace:

```csharp
            this.label3.Text = "Command";
```

with:

```csharp
            this.label3.Text = Properties.Strings.Label_Command;
```

Replace:

```csharp
            this.button1.Text = "Find";
```

with:

```csharp
            this.button1.Text = Properties.Strings.Button_Find;
```

Replace:

```csharp
            this.btnTest.Text = "Testuj";
```

with:

```csharp
            this.btnTest.Text = Properties.Strings.Button_Test;
```

Replace:

```csharp
            this.Text = "Add / Edit Item";
```

with:

```csharp
            this.Text = Properties.Strings.Form_ManageItem_Title;
```

- [ ] **Step 9: Replace the raw enum-name dropdown with localized display text**

In `CTRegistryTree\FrmManageItemForm.cs`, replace the whole file's top (class fields and both constructors) — currently:

```csharp
using CTPlugins;
using System;
using System.Windows.Forms;

namespace CTRegistryTree
{
    public partial class FrmManageItemForm : FrmTemplateDialog
    {
        public RegistryTreeItem Item { get; private set; }

        private readonly string parentPath;

        public FrmManageItemForm(string path = "")
        {
            InitializeComponent();

            parentPath = path;
            cbAction.Items.AddRange(Enum.GetNames(typeof(RegistryTreeItem.ActionType)));
            cbAction.SelectedIndex = 0;

            OKClicked += delegate { Item = BuildItem(new RegistryTreeItem()); };
            CancelClicked += delegate { Item = null; };
        }

        public FrmManageItemForm(RegistryTreeItem item)
        {
            InitializeComponent();

            cbAction.Items.AddRange(Enum.GetNames(typeof(RegistryTreeItem.ActionType)));
            SetItem(item);

            OKClicked += delegate { Item = BuildItem(Item); };
            CancelClicked += delegate { Item = item; };
        }
```

with:

```csharp
using CTPlugins;
using System;
using System.Windows.Forms;

namespace CTRegistryTree
{
    public partial class FrmManageItemForm : FrmTemplateDialog
    {
        /// <summary>
        /// Action types in the exact order they're added to <c>cbAction</c>, so that
        /// <c>cbAction.SelectedIndex + 1</c> / <c>(int)Item.Action - 1</c> keep mapping
        /// correctly to the enum's declared values (RunCommand=1, OpenUrl=2, OpenFile=3).
        /// </summary>
        private static readonly RegistryTreeItem.ActionType[] actionOrder = new[]
        {
            RegistryTreeItem.ActionType.RunCommand,
            RegistryTreeItem.ActionType.OpenUrl,
            RegistryTreeItem.ActionType.OpenFile
        };

        public RegistryTreeItem Item { get; private set; }

        private readonly string parentPath;

        public FrmManageItemForm(string path = "")
        {
            InitializeComponent();

            parentPath = path;
            PopulateActionItems();
            cbAction.SelectedIndex = 0;

            OKClicked += delegate { Item = BuildItem(new RegistryTreeItem()); };
            CancelClicked += delegate { Item = null; };
        }

        public FrmManageItemForm(RegistryTreeItem item)
        {
            InitializeComponent();

            PopulateActionItems();
            SetItem(item);

            OKClicked += delegate { Item = BuildItem(Item); };
            CancelClicked += delegate { Item = item; };
        }

        private void PopulateActionItems()
        {
            foreach (var action in actionOrder)
            {
                cbAction.Items.Add(GetActionDisplayText(action));
            }
        }

        private static string GetActionDisplayText(RegistryTreeItem.ActionType action)
        {
            switch (action)
            {
                case RegistryTreeItem.ActionType.RunCommand:
                    return Properties.Strings.ActionType_RunCommand;
                case RegistryTreeItem.ActionType.OpenUrl:
                    return Properties.Strings.ActionType_OpenUrl;
                case RegistryTreeItem.ActionType.OpenFile:
                    return Properties.Strings.ActionType_OpenFile;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }
```

Leave the rest of the file (`BuildItem`, `SetItem`, `button1_Click`, `btnTest_Click`) unchanged — their `cbAction.SelectedIndex + 1` / `(int)Item.Action - 1` / `(cbAction.SelectedIndex + 1)` index math already matches `actionOrder`'s order, which mirrors the enum's declared values.

- [ ] **Step 10: Build and verify**

Run: `msbuild CustomTools.slnx /p:Configuration=Debug /t:CTRegistryTree`

Expected: `Build succeeded`, and `CustomTools\bin\Debug\Plugins\pl\CTRegistryTree.resources.dll` exists (note: under `CustomTools\bin\Debug\Plugins\`, not `CTRegistryTree\bin\Debug\`, per `CTRegistryTree`'s `OutputPath` redirect).

- [ ] **Step 11: Commit**

```bash
git add CTRegistryTree/Properties/Strings.resx CTRegistryTree/Properties/Strings.pl.resx CTRegistryTree/Properties/Strings.Designer.cs CTRegistryTree/CTRegistryTree.csproj CTRegistryTree/CTRegistryTree.cs CTRegistryTree/FrmManageItemsForm.cs CTRegistryTree/FrmManageItemsForm.Designer.cs CTRegistryTree/FrmManageItemForm.cs CTRegistryTree/FrmManageItemForm.Designer.cs
git commit -m "Localize CTRegistryTree plugin strings and ActionType dropdown (English/Polish)"
```

---

## Task 4: Solution-wide build and runtime verification

**Files:** none created or modified — this task only builds and verifies Tasks 1-3.

**Interfaces:**
- Consumes: `CustomTools.Properties.Strings`, `CTPlugins.Properties.Strings`, `CTRegistryTree.Properties.Strings` (all three from Tasks 1-3).

- [ ] **Step 1: Full solution build**

Run: `msbuild CustomTools.slnx /p:Configuration=Debug`

Expected: `Build succeeded`, 0 errors, for all three projects.

- [ ] **Step 2: Runtime smoke check — default culture (English)**

There's no test project in this repo to add a unit test to (per `CLAUDE.md`), so this step uses PowerShell reflection against the freshly-built assemblies as a substitute — it proves the satellite-resource wiring actually resolves strings at runtime, not just that the code compiles.

Run this PowerShell (from the repo root, after Step 1's build):

```powershell
Add-Type -Path "CustomTools\bin\Debug\CustomTools.exe"
Add-Type -Path "CustomTools\bin\Debug\Plugins\CTRegistryTree.dll"
Add-Type -Path "CTPlugins\bin\Debug\CTPlugins.dll"

function Get-StringProp($typeName, $propName, $assemblyPath) {
    $asm = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
    $type = $asm.GetType($typeName)
    $prop = $type.GetProperty($propName, [System.Reflection.BindingFlags]'Static,NonPublic,Public')
    return $prop.GetValue($null)
}

[System.Threading.Thread]::CurrentThread.CurrentUICulture = [System.Globalization.CultureInfo]::InvariantCulture

Write-Output (Get-StringProp "CustomTools.Properties.Strings" "TrayMenu_Options" "CustomTools\bin\Debug\CustomTools.exe")
Write-Output (Get-StringProp "CTPlugins.Properties.Strings" "Dialog_Cancel" "CTPlugins\bin\Debug\CTPlugins.dll")
Write-Output (Get-StringProp "CTRegistryTree.Properties.Strings" "Form_ManageItems_Title" "CustomTools\bin\Debug\Plugins\CTRegistryTree.dll")
```

Expected output (three lines): `Options`, `Cancel`, `Manage Items`.

- [ ] **Step 3: Runtime smoke check — Polish culture**

Run the same script, but set the culture to Polish first:

```powershell
function Get-StringProp($typeName, $propName, $assemblyPath) {
    $asm = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
    $type = $asm.GetType($typeName)
    $prop = $type.GetProperty($propName, [System.Reflection.BindingFlags]'Static,NonPublic,Public')
    return $prop.GetValue($null)
}

[System.Threading.Thread]::CurrentThread.CurrentUICulture = New-Object System.Globalization.CultureInfo("pl")

Write-Output (Get-StringProp "CustomTools.Properties.Strings" "TrayMenu_Options" "CustomTools\bin\Debug\CustomTools.exe")
Write-Output (Get-StringProp "CTPlugins.Properties.Strings" "Dialog_Cancel" "CTPlugins\bin\Debug\CTPlugins.dll")
Write-Output (Get-StringProp "CTRegistryTree.Properties.Strings" "Form_ManageItems_Title" "CustomTools\bin\Debug\Plugins\CTRegistryTree.dll")
```

Expected output (three lines): `Opcje`, `Anuluj`, `Zarządzaj elementami`.

If either check prints the wrong-language string (e.g. Polish culture still prints English), the most likely causes are: the `pl.resx` file wasn't given the `.pl.resx` filename exactly (culture is parsed from the filename), the satellite DLL didn't get copied to the same directory as the main assembly, or a stale build — rerun Step 1 with a clean rebuild (`msbuild CustomTools.slnx /p:Configuration=Debug /t:Rebuild`) and retry.

- [ ] **Step 4: Manual visual QA note**

This PowerShell check confirms the resource wiring is correct, but doesn't visually exercise the tray menu or dialogs. Tell the user: to see it for real, switch Windows' display language to Polish (Settings → Time & Language → Language & Region), run `CustomTools.exe`, and confirm the tray context menu shows `Opcje`/`Zamknij`, the plugin section shows `⚙ Zarządzaj`, and the manage-items dialogs show the Polish button/label text from the table in the design spec. This step can't be automated from here since it requires changing the OS-level display language.

- [ ] **Step 5: Final commit check**

Run: `git status`

Expected: working tree clean (Tasks 1-3 already committed their changes; this task made no file changes).
