namespace PluginModules.Features.Commands.Interfaces;

public interface IModuleCommand<T> : IHasModule where T : Module
{
    T Module { get; set; }

    ModuleLog ModuleLog => Module.ModuleLog;

    void IHasModule.SetModule(Module module)
    {
        Module = (T)module;
    }
}

public interface IHasModule
{
    internal void SetModule(Module module);
}