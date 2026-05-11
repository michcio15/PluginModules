using PluginModules.Interfaces;

namespace PluginModules.Features.Components;

/// <summary>
///     No component ale taki nie na monobehavioyr
/// </summary>
/// <typeparam name="TModule"><see cref="Module{T}" /> ktory jest tego parentem</typeparam>
public abstract class ModuleComponent<TModule> : IModuleComponent where TModule : Module
{
    public static TModule Module { get; internal set; } = null!;
    public static ModuleLog ModuleLog => Module.ModuleLog;
}

/// <summary>
///     <see cref="ModuleComponent{TModule}" /> z configiem
/// </summary>
/// <typeparam name="TModule">
///     <inheritdoc />
/// </typeparam>
/// <typeparam name="TConfig">Config modułu</typeparam>
public abstract class ModuleComponent<TModule, TConfig> : ModuleComponent<TModule> where TModule : Module<TConfig>
    where TConfig : class, IModuleConfig, new()
{
    public static TConfig Config => Module.Config;
}