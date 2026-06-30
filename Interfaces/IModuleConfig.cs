using FajneConfigurables;
using FajneConfigurables.Interfaces;

namespace PluginModules.Interfaces;

public interface IModuleConfig : IConfigurableConfig, ITogglable, IDebugConfig;

public interface IDebugConfig
{
    public bool DebugEnabled { get; set; }
}