using HarmonyLib;
using JetBrains.Annotations;
using MEC;
using PluginModules.Attributes;
using PluginModules.Features.Components;
using System.Reflection;

namespace PluginModules.Features;

/// <summary>
///     No to ten główny budulec
/// </summary>
[MeansImplicitUse]
[PublicAPI]
public abstract class Module
{
    /// <summary>
    ///     Nazwa modułu
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    ///     Priorytet
    /// </summary>
    public virtual ModulePriority Priority => ModulePriority.Medium;

    /// <summary>
    ///     Czy jest aktywny?
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    ///     Używane do logowania w module
    /// </summary>
    public ModuleLog ModuleLog { get; protected set; } = null!;
    
    /// <summary>
    ///     Czy moduł ma używać Harmony
    /// </summary>
    public virtual bool EnableHarmony { get; protected set; } = false;

    /// <summary>
    ///     <see cref="Harmony" /> tego modułu
    /// </summary>
    protected Harmony? HarmonyInstance => HarmonyManager.Harmony;

    /// <summary>
    ///     Spatchowane metody przez <see cref="HarmonyInstance" />
    /// </summary>
    public IEnumerable<MethodBase>? Patches => HarmonyInstance?.GetPatchedMethods();

    /// <summary>
    /// Harmony manager dla patchy
    /// </summary>
    public HarmonyManager HarmonyManager { get; private set; } = null!;

    /// <summary>
    ///     No czy ma byc debug
    /// </summary>
    internal Func<bool>? DebugProvider { get; set; }

    /// <summary>
    ///     Czy jest włączony debug
    /// </summary>
    public virtual bool IsDebugEnabled => DebugProvider?.Invoke() == true;

    /// <summary>
    /// Do komend
    /// </summary>
    public CommandsManager CommandsManager { get; private set; } = null!;

    /// <summary>
    ///     Czy moduł ma szukać komend
    /// </summary>
    public virtual bool CommandsEnabled => false;

    /// <summary>
    ///     Czy moduł ma jakies <see cref="IModuleComponent" />
    /// </summary>
    public virtual bool ComponentsEnabled { get; } = false;

    /// <summary>
    ///     Czy ma się wyswietlac w komendach
    /// </summary>
    public virtual bool HideFromCommands { get; } = false;

    /// <summary>
    ///     Zwraca assembly tego modułu
    /// </summary>
    public Assembly Assembly => GetType().Assembly;

    /// <summary>
    ///     Cached types for quicker enumeration
    /// </summary>
    internal IEnumerable<Type> CachedTypes = null!;

    /// <summary>
    /// No to namespace
    /// </summary>
    /// <exception cref="NullReferenceException">Jezeli null</exception>
    public virtual string Namespace => GetType().Namespace ??
                                       throw new NullReferenceException(
                                           "Namespace is null. Please contact plugin creator");

    internal void Init(Func<bool>? debugProvider = null)
    {
        DebugProvider = debugProvider;
        ModuleLog ??= new ModuleLog(Name, Assembly.GetName().Name, debugProvider != null && debugProvider.Invoke());
        CommandsManager = new CommandsManager(this);
        HarmonyManager = new HarmonyManager(this);
    }

    /// <summary>
    ///     Zajmuje się uruchamianiem i przygotowywaniem modułu
    /// </summary>
    public virtual void Enable()
    {
        if (IsActive)
        {
            return;
        }

        try
        {
            LookForTypes();
        }
        catch (Exception e)
        {
            ModuleLog.Error(e);
        }

        if (EnableHarmony)
        {
            try
            {
                HarmonyManager.PatchAll();
            }
            catch (Exception e)
            {
                ModuleLog.Error($"Couldn't patch harmony: {e}");
            }
        }


        if (CommandsEnabled)
        {
            try
            {
                CommandsManager.RegisterCommands();
            }
            catch (Exception e)
            {
                ModuleLog.Error(e);
                throw;
            }
        }


        if (ComponentsEnabled)
        {
            try
            {
                SetModuleInComponents();
            }
            catch (Exception e)
            {
                ModuleLog.Error(e);
            }
        }

        OnEnabled();


        IsActive = true;

        ModuleLog.Info($"Moduł {Name} włączony.");
    }

    /// <summary>
    ///     Cleanup
    /// </summary>
    public virtual void Disable()
    {
        if (!IsActive)
        {
            return;
        }

        OnDisabled();

        HarmonyInstance?.UnpatchAll(HarmonyInstance.Id);

        ResetRoundState();
        IsActive = false;

        HarmonyManager.UnpatchAll();

        CommandsManager.UnregisterCommands();


        ModuleLog.Debug($"Moduł {Name} wyłączony.");
    }


    /// <summary>
    ///     Ustawia te <see cref="IModuleComponent" /> moduły
    /// </summary>
    private void SetModuleInComponents()
    {
        IEnumerable<Type> componentTypes = CachedTypes
            .Where(t =>
                typeof(IModuleComponent).IsAssignableFrom(t));

        foreach (PropertyInfo? prop in componentTypes.Select(GetModuleProperty))
        {
            prop?.SetValue(null, this);
        }
    }

    private static PropertyInfo? GetModuleProperty(Type type)
    {
        Type? current = type;
        while (current != null)
        {
            PropertyInfo? prop = current.GetProperty("Module",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (prop != null)
            {
                return prop;
            }

            current = current.BaseType;
        }

        return null;
    }

    /// <summary>
    ///     Restart rundy
    /// </summary>
    public void ResetRoundState()
    {
        OnRoundCleanup();
    }

    /// <summary>
    ///     Uruchomienie modułu
    /// </summary>
    protected virtual void OnEnabled() { }

    /// <summary>
    ///     Wyłączenie modułu
    /// </summary>
    protected virtual void OnDisabled() { }

    /// <summary>
    ///     Restart rundy
    /// </summary>
    protected virtual void OnRoundCleanup() { }

    private void LookForTypes()
    {
        CachedTypes = Assembly.GetTypes()
            .Where(t => t.Namespace != null &&
                        t.Namespace.StartsWith(Namespace)
                        && t.GetCustomAttribute<DisableAutoRegister>() == null
                        && (!t.IsAbstract || t.IsSealed));
    }
}

/// <summary>
///     Priorytet rejestrowania modułu
/// </summary>
[PublicAPI]
public enum ModulePriority
{
    /// <summary>
    ///     Najniższy
    /// </summary>
    Lowest = 1,

    /// <summary>
    ///     Niski
    /// </summary>
    Low = 2,

    /// <summary>
    ///     Średni
    /// </summary>
    Medium = 3,

    /// <summary>
    ///     Wysoki
    /// </summary>
    High = 4,

    /// <summary>
    ///     Najwyższy
    /// </summary>
    Highest = 5
}