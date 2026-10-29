# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`PluginModules` is a shared **framework library**, not a standalone gameplay plugin. It's a LabAPI/Harmony plugin for
the SCP: Secret Laboratory dedicated server that gives *other* SCP:SL plugins a "module" system: a base class +
registry + auto-wired Harmony patching, commands, and Unity components, keyed per consuming assembly.

Multiple sibling Rider projects in `../` (e.g. `ShadowRP`, `JustRolePlay.Core`, `JustRolePlay.Combat`, `Bojowka.RP`,
`Bojowka.RPCommands`) reference `PluginModules.dll` and build their own `Module` subclasses on top of it. Changes here
ripple out to all of them — check call sites in sibling repos before changing public APIs (`Module`, `Module<T>`,
`ModuleManager`, `ICommandsManager`, `IHarmonyManager`).

It also depends on a sibling project, `../FajneConfigurables/FajneConfigurables.csproj` (referenced via the `.slnx` and
relative `HintPath`s), for YAML-based per-module config loading (`ConfigLoader`, `IConfigurableConfig`, `ITogglable`).

## Build

- Target framework: `net48`, nullable enabled, `LangVersion=latest`.
- Requires the `SL_REFERENCES` environment variable pointing at a folder with the SCP:SL game/game-dependency assemblies
  (`Assembly-CSharp`, `Mirror`, `UnityEngine`, etc.) — the `.csproj` resolves all game references via
  `$(SL_REFERENCES)\*.dll`.
- Requires the sibling checkout `../FajneConfigurables` to exist (referenced by relative path, not a NuGet package).
- Build with `dotnet build PluginModules.csproj` or via Rider using `PluginModules.slnx`. A post-build target copies the
  built DLL to `BUILD/$(Configuration)/`.
- There is no test project and no lint/format command configured in this repo; `.editorconfig` defines style rules that
  Rider/ReSharper enforce on save.

## Core architecture

### `Module` — the base building block (`Features/Module.cs`)

Every feature is a subclass of `Module` (or the generic `Module<T>` for config-backed modules,
`Features/ModuleConfig.cs`). Key members:

- `Name`, `Priority` (`ModulePriority`: Lowest..Highest, used for enable ordering), `IsActive`.
- Lifecycle: `Enable()` / `Disable()` call the protected overridable hooks `OnEnabled()`, `OnDisabled()`,
  `OnRoundCleanup()` (the last fires on every round restart via `ModuleManager`).
- Opt-in subsystems, toggled by overriding a `virtual bool`: `EnableHarmony`, `CommandsEnabled`, `ComponentsEnabled`.
- `Module<T>` adds `Config` (`T : IModuleConfig`), loaded/saved through FajneConfigurables' `ConfigLoader`, and
  implements `IConfigurable` + `IReloadableModule` (`TryReloadConfig`).

**Convention-based discovery is central to this framework.** On `Enable()`, `Module.LookForTypes()` reflects over the
module's own assembly for every type whose namespace starts with the module's `Namespace` (default: the module class's
own namespace) and caches them as `CachedTypes`. That cache then drives:

- **Harmony** (`Features/HarmonyManager.cs`): any cached type with `[HarmonyPatch]` gets patched into a `Harmony`
  instance named `com.module.{ModuleName}`.
- **Commands** (`Features/CommandsManager.cs`): any cached type implementing `ICommand` with a
  `[ModuleCommandAttribute(CommandType)]` gets registered to the matching handler (`RemoteAdmin`/`Console`/`Client`).
- **Components** (`Features/Components/*`): any cached type implementing `IModuleComponent` gets its static `Module`
  property (found by walking up the type hierarchy) set to the owning module instance.

Mark a type with `[DisableAutoRegister]` (`Attributes/DisableAutoRegister.cs`) to exclude it from this scanning.
**Implication:** patches, module-specific commands, and components must live in a namespace nested under their owning
`Module`'s namespace to be picked up automatically.

### `ModuleManager` — global registry (`Features/ModuleManager.cs`)

Static registry keyed **per calling assembly** (`AssembliesModules: Dictionary<Assembly, HashSet<Module>>`), which is
what lets each consuming plugin register/enable/disable only its own modules without stepping on others. Entry points a
consuming plugin calls from its own assembly:

- `PrepareAndEnableAll(assembly?, debugProvider?)` = `RegisterModules` (reflect assembly for `Module` subclasses,
  instantiate via `Activator.CreateInstance`, call `Init`) + `EnableModules` (enable in descending `Priority` order,
  skipping modules whose config is `ITogglable { Enabled: false }`).
- `TryGetModule<T>` / `GetModule<T>` / `ModuleEnabled<T>` — typed lookup across all registered modules (any assembly).
- `DisableAllModules`, `DisableModules(assembly)`, `UnregisterModules(assembly)`.
- Subscribes to `LabApi.Events.Handlers.ServerEvents.RoundRestarted` to call `ResetRoundState()` (→ `OnRoundCleanup`) on
  every active module.

### Commands (`Features/Commands/*`, `Commands/*`)

`ModuleCommand<T>` / `ModuleParentCommand<T>` are base classes for a module's own commands; they receive a typed
back-reference to their owning `Module` via `IHasModule.SetModule` (called by `CommandsManager` right after
registration). `Features/Commands/Interfaces/IModuleCommand.cs` offers a default-interface-method variant of the same
pattern.

The framework itself ships one RA/console command, `pluginmodules` (alias `plmod`,
`Commands/PluginModulesParentCommand.cs`), with subcommands for introspecting/managing modules across *all* registered
assemblies at runtime: `list`, `info <name>`, `enable <name>`, `disable <name>`, `reload all|config|module <name>`.

### Logging

`Helpers/CoreLog.cs` is a static wrapper around SCP:SL's `ServerConsole.AddLog` with per-level colors and
`[prefix][module]` formatting. `Features/ModuleLog.cs` wraps it per-module (bound to the module's name and a
debug-enabled predicate — either `Module<T>.Config.DebugEnabled` or a fallback `DebugProvider`) and is accessed as
`module.Log`.

### Plugin entry point

`PluginModulesPlugin.cs` defines `PluginModules : Plugin<Config>`, the LabAPI plugin itself (`LoadPriority.Highest`, so
it loads before dependent plugins). `Config.cs` is this plugin's own config (just a `Debug` flag) — distinct from the
per-module `IModuleConfig` used by `Module<T>`.

## Conventions to preserve

- XML doc comments in this codebase are largely written in Polish; match that style when adding docs to existing files
  rather than switching to English mid-file.
- Namespace-based auto-discovery (see above) means moving a patch/command/component type to a different namespace
  silently unregisters it — verify namespace placement after refactors.
- `[PublicAPI]`-annotated types are the intended extension surface for consumer plugins; treat changes to their
  signatures as breaking changes for sibling repos.
