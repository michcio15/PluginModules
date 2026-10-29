using Exiled.API.Features;
using PluginModules.Features.Interface;
using System.Diagnostics.CodeAnalysis;

namespace PluginModules.Features.Managers;

public class CustomManagersManager : ICustomManagersManager
{
    public CustomManagersManager(Module module)
    {
        Module = module;
        Managers = new List<IModuleManager>(Module.ManagersToRegister.Length);
    }

    public Module Module { get; }

    private ModuleLog Log => Module.Log;

    public void Enable()
    {
        foreach (Type type in Module.ManagersToRegister.Where(static t => typeof(IModuleManager).IsAssignableFrom(t)))
        {
            try
            {
                if (Activator.CreateInstance(type, Module) is not IModuleManager manager)
                {
                    Log.Error($"Couldn't create manager instance for {type.Name}: type does not implement {nameof(IModuleManager)}");
                    continue;
                }

                Managers.Add(manager);
            }
            catch (Exception e)
            {
                Log.Error($"Couldn't enable manager {type.Name}: {e}");
            }
        }

        foreach (IModuleManager manager in Managers)
        {
            try
            {
                manager.Enable();
            }
            catch (Exception e)
            {
                Log.Error($"Couldn't enable manager {manager.GetType().Name}: {e}");
            }
        }
    }

    public void Disable()
    {
        foreach (IModuleManager manager in Managers)
        {
            try
            {
                manager.Disable();
            }
            catch (Exception e)
            {
                Log.Error($"Couldn't disable manager {manager.GetType().Name}: {e}");
            }
        }

        Managers.Clear();
    }

    public bool TryGetManager<T>([NotNullWhen(true)] out T? manager) where T : class, IModuleManager
    {
        manager = null;

        foreach (IModuleManager moduleManager in Managers)
        {
            if (moduleManager is not T m)
            {
                continue;
            }

            manager = m;
            return true;
        }

        return false;
    }

    public T? GetManager<T>() where T : class, IModuleManager
    {
        return TryGetManager(out T? manager) ? manager : null;
    }

    public List<IModuleManager> Managers { get; }
}