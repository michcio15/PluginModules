namespace PluginModules.Features.Commands.Interfaces;

public interface IModuleCommand<T> : IHasModule where T : Module
{
    void IHasModule.SetModule(Module module)
    {
        Module = (T)module;
    }

    T Module { get; set; }

    [Obsolete("Use Log instead", true)]
    ModuleLog ModuleLog => Log;

    ModuleLog Log => Module.Log;
}

public interface IHasModule
{
    internal void SetModule(Module module);
}