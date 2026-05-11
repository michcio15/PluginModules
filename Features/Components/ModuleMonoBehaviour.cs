using PluginModules.Interfaces;
using UnityEngine;

namespace PluginModules.Features.Components;

public abstract class ModuleMonoBehaviour<TModule> : MonoBehaviour, IModuleComponent where TModule : Module
{
    public static TModule Module { get; internal set; } = null!;
    public static ModuleLog ModuleLog => Module.ModuleLog;
}

public abstract class ModuleMonoBehaviour<TModule, TConfig> : ModuleMonoBehaviour<TModule>
    where TModule : Module<TConfig> where TConfig : class, IModuleConfig, new()
{
    public static TConfig Config => Module.Config;
}