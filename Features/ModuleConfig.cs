using FajneConfigurables;
using FajneConfigurables.Helpers;
using FajneConfigurables.Interfaces;
using PluginModules.Interfaces;

namespace PluginModules.Features;

public abstract class Module<T> : Module, IConfigurable where T : class, IModuleConfig, new()
{
    public T Config { get; private set; } = null!;

    public virtual string FileName => Name;

    /// <inheritdoc />
    public IConfigurableConfig BaseConfig => Config;

    /// <inheritdoc />
    public abstract string[]? Path { get; }

    /// <inheritdoc />
    public string ConfigPath
    {
        get
        {
            field ??= ConfigLoader.BuildPath(Name, Path);
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
        if (!YamlHelper.TryDeserialize(this.GetConfigPath(), out T? newConfig))
        {
            return false;
        }

        Config = newConfig;
        return true;
    }
}