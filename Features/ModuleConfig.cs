using FajneConfigurables;
using FajneConfigurables.Interfaces;
using JetBrains.Annotations;
using PluginModules.Interfaces;

namespace PluginModules.Features;

[PublicAPI]
[MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.Itself)]
public abstract class Module<T> : Module, IConfigurable, IReloadableModule where T : class, IModuleConfig, new()
{
    public T Config { get; private set; } = null!;

    public virtual string FileName => Name;

    /// <inheritdoc />
    public IConfigurableConfig BaseConfig => Config;

    public override bool IsDebugEnabled => Config.DebugEnabled || base.IsDebugEnabled;

    /// <inheritdoc />
    public virtual string[]? Path { get; } = null;

    /// <inheritdoc />
    public string ConfigPath
    {
        get
        {
            field ??= ConfigLoader.BuildPath(Name, Assembly, Path);
            return field;
        }
    }

    public void LoadConfig()
    {
        Config = ConfigLoader.Load<T>(ConfigPath);
    }

    public void UnloadConfig()
    {
        Config = null!;
    }

    public bool TryReloadConfig()
    {
        UnloadConfig();
        LoadConfig();
        return true;
    }
}