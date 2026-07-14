using FajneConfigurables;
using FajneConfigurables.Interfaces;

namespace PluginModules.Interfaces;

public interface IModuleConfig : IConfigurableConfig, ITogglable
{
    bool DebugEnabled { get; set; }
}