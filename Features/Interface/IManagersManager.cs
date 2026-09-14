using System.Diagnostics.CodeAnalysis;

namespace PluginModules.Features.Interface;

public interface IManagersManager : IModuleManager
{
    bool TryGetManager<T>([NotNullWhen(true)] out T? manager) where T : class, IModuleManager;

    T? GetManager<T>() where T : class, IModuleManager;

    List<IModuleManager> Managers { get; }
}