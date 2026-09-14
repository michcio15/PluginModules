namespace PluginModules.Features.Commands.Interfaces;

public interface IModuleCommand<T> : IHasModule where T : Module
{
    void IHasModule.SetModule(Module module)
    {
        Module = (T)module;
    }

    T Module { get; set; }

    ModuleLog ModuleLog => Module.Log;
}

public interface IHasModule
{
    internal void SetModule(Module module);
}