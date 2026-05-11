using PluginModules.Features.Commands.Interfaces;

namespace PluginModules.Features.Commands;

public abstract class ModuleParentCommand<T> : ParentCommand, IHasModule where T : Module
{
    public T Module { get; private set; } = null!;
    public ModuleLog ModuleLog => Module.ModuleLog;

    public void SetModule(Module module)
    {
        Module = (T)module;
    }
}