using FajneConfigurables.Interfaces;
using JetBrains.Annotations;
using LabApi.Events.Handlers;
using PluginModules.Attributes;
using PluginModules.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace PluginModules.Features;

/// <summary>
///     Element używany z <see cref="Module" /> aby rozbić dzialanośc na mniejsze częśći
/// </summary>
[PublicAPI]
public static class ModuleManager
{
    /// <summary>
    ///     Zarejestrowane moduły
    /// </summary>
    internal static readonly HashSet<Module> ModulesInternal = [];

    /// <summary>
    ///     Typy modułuów dla szybszego lookupu
    /// </summary>
    private static readonly Dictionary<Type, Module> Types = new();

    /// <summary>
    ///     Jakie assembly ma jaki moduł
    /// </summary>
    private static readonly Dictionary<Assembly, HashSet<Module>> AssembliesModules = new();

    /// <summary>
    ///     Moduły tylko do oczytu
    /// </summary>
    // ReSharper disable once ReturnTypeCanBeEnumerable.Global
    public static IReadOnlyCollection<Module> Modules => ModulesInternal;


    /// <summary>
    /// Lista uruchomionych modułów
    /// </summary>
    public static IReadOnlyList<Module> EnabledModules => Modules.Where(x => x.IsActive).ToList();


    /// <summary>
    ///     No wszystko
    /// </summary>
    public static void PrepareAndEnableAll(Assembly? assembly = null, Func<bool>? debugProvider = null)
    {
        RegisterModules(assembly, debugProvider);
        EnableModules(assembly);
    }

    /// <summary>
    ///     Rejestruje wszystkie moduły
    /// </summary>
    public static void RegisterModules(Assembly? assembly = null, Func<bool>? debugProvider = null)
    {
        ModulesInternal.Clear();
        Types.Clear();
        assembly ??= Assembly.GetCallingAssembly();
        IEnumerable<Type> types = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Module)) && !t.IsAbstract &&
                        t.GetCustomAttribute<DisableAutoRegister>() == null);
        CoreLog.Info("LOADER", "------------- Rejestrowanie modułów -------------");
        int registered = 0;
        foreach (Type type in types)
        {
            try
            {
                Module module = (Module)Activator.CreateInstance(type);
                ModulesInternal.Add(module);
                Types.Add(type, module);
                AddModuleToAssembly(module, assembly);
                module.DebugProvider = debugProvider;
                registered++;
                CoreLog.Info("LOADER", $"Zarejestrowano: {module.Name} [Prio: {module.Priority}]");
            }
            catch (Exception e)
            {
                CoreLog.Error("LOADER", $"Błąd rejestracji {type.Name}: {e}");
            }
        }

        CoreLog.Info("LOADER", $"------------- Zarejestrowano {registered} modułów -------------");
    }

    /// <summary>
    ///     Włacza wszystkie moduły
    /// </summary>
    public static void EnableModules(Assembly? assembly = null)
    {
        CoreLog.Info("LOADER", "------------- Włączanie modułów -------------");
        assembly ??= Assembly.GetCallingAssembly();
        if (!AssembliesModules.TryGetValue(assembly, out HashSet<Module>? modules))
        {
            CoreLog.Warn("LOADER", $"Nie ma zadnego modułu zarejestrowanego w {assembly.FullName}");
            return;
        }

        int enabled = modules.OrderByDescending(x => x.Priority).Count(m => TryEnableModule(m));

        CoreLog.Info("LOADER", $"------------- Włączono {enabled} modułów -------------");
    }

    public static bool TryEnableModule(Module module, bool force = false)
    {
        if (module is IConfigurable configurable)
        {
            try
            {
                configurable.LoadConfig();
            }
            catch (Exception e)
            {
                CoreLog.Error("LOADER", $"Nie udało się załadować configu dla {module.Name}: {e}");
                return false;
            }

            if (!force && configurable.BaseConfig is ITogglable { Enabled: false })
            {
                configurable.UnloadConfig();
                return false;
            }
        }

        try
        {
            module.Enable();
        }
        catch
        {
            return false;
        }

        return true;
    }


    /// <summary>
    ///     Wyłącza wszystko
    /// </summary>
    internal static void DisableAll()
    {
        UnregisterEvents();
        DisableAllModules();
        UnregisterAllModules();
    }

    internal static void EnableAllModules()
    {
        foreach (Module module in Modules)
        {
            TryEnableModule(module);
        }
    }

    /// <summary>
    ///     Wyłącza wszystkie danego assembly
    /// </summary>
    public static void DisableModules(Assembly? assembly = null)
    {
        if (!AssembliesModules.TryGetValue(assembly ?? Assembly.GetCallingAssembly(), out HashSet<Module>? modules))
        {
            return;
        }

        foreach (Module module in modules)
        {
            module.Disable();
        }
    }

    internal static void DisableAllModules()
    {
        foreach (Module module in Modules)
        {
            module.Disable();
        }
    }

    public static void UnregisterModules(Assembly? assembly = null)
    {
        Assembly callingAssembly = assembly ?? Assembly.GetCallingAssembly();
        if (!AssembliesModules.TryGetValue(callingAssembly, out HashSet<Module>? modules))
        {
            return;
        }

        foreach (Module module in modules)
        {
            ModulesInternal.Remove(module);
            Types.Remove(module.GetType());
        }

        AssembliesModules.Remove(callingAssembly);
    }

    private static void UnregisterAllModules()
    {
        ModulesInternal.Clear();
        Types.Clear();
        AssembliesModules.Clear();
    }

    /// <summary>
    ///     Sprawdza, czy jest zarejestwoany moduł
    /// </summary>
    /// <param name="module">Zarejestrowana instancja <see cref="Module" /></param>
    /// <param name="ignoreDisabled">
    ///     Czy jeżeli moduł ma <see cref="Module.IsActive" /> na <see langword="false" /> czy ma go
    ///     ignorowac
    /// </param>
    /// <typeparam name="T" />
    /// <returns>Jest czy nie </returns>
    public static bool TryGetModule<T>([NotNullWhen(true)] out T? module, bool ignoreDisabled = true) where T : Module
    {
        if (Types.TryGetValue(typeof(T), out Module? m))
        {
            if (ignoreDisabled && !m.IsActive)
            {
                module = null;
                return false;
            }

            module = (T)m;
            return true;
        }

        module = null;
        return false;
    }


    /// <summary>
    ///     Zwraca nullable
    /// </summary>
    /// <param name="ignoreDisabled">Czy ma no tam wyzej</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? GetModule<T>(bool ignoreDisabled = true) where T : Module
    {
        return TryGetModule(out T? module, ignoreDisabled) ? module : null;
    }

    /// <summary>
    ///     Czy moduł jest włączony
    /// </summary>
    /// <typeparam name="T">Moduł, który sprawdzamy</typeparam>
    /// <returns></returns>
    public static bool ModuleEnabled<T>() where T : Module
    {
        return TryGetModule(out T? module) && module.IsActive;
    }

    /// <summary>
    ///     Eventy
    /// </summary>
    internal static void RegisterEvents()
    {
        ServerEvents.RoundRestarted += OnRestartingRound;
    }

    /// <summary>
    ///     Eventy
    /// </summary>
    internal static void UnregisterEvents()
    {
        ServerEvents.RoundRestarted -= OnRestartingRound;
    }

    /// <summary>
    ///     Wywołuje <see cref="Module.ResetRoundState" />
    /// </summary>
    private static void OnRestartingRound()
    {
        CoreLog.Info("CLEANUP", "Restart rundy, czyszczenie modułów...");

        foreach (Module? module in Modules.Where(module => module.IsActive))
        {
            module.ResetRoundState();
        }
    }

    private static void AddModuleToAssembly(Module module, Assembly assembly)
    {
        if (!AssembliesModules.TryGetValue(assembly, out HashSet<Module>? modules))
        {
            modules = new HashSet<Module>();
            AssembliesModules[assembly] = modules;
        }

        modules.Add(module);
    }

    private static void RemoveModuleFromAssembly(Module module, Assembly assembly)
    {
        if (AssembliesModules.TryGetValue(assembly, out HashSet<Module>? modules))
        {
            modules.Remove(module);
        }
    }
}