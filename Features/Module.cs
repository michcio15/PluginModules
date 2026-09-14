using HarmonyLib;
using JetBrains.Annotations;
using PluginModules.Attributes;
using PluginModules.Features.Components;
using PluginModules.Features.Interface;
using PluginModules.Features.Managers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace PluginModules.Features;

/// <summary>
///     No to ten główny budulec
/// </summary>
[MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.Itself)]
[PublicAPI]
public abstract class Module
{
    /// <summary>
    ///     Cached types for quicker enumeration
    /// </summary>
    public IEnumerable<Type> CachedTypes = null!;

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
    public ModuleLog Log { get; protected set; } = null!;

    [Obsolete("Use Log instead", true)]
    public ModuleLog ModuleLog => Log;

    /// <summary>
    ///     Czy moduł ma jakies <see cref="IModuleComponent" />
    /// </summary>
    public virtual bool ComponentsEnabled { get; } = false;

    /// <summary>
    ///     Zwraca assembly tego modułu
    /// </summary>
    public Assembly Assembly => GetType().Assembly;

    /// <summary>
    /// No to namespace
    /// </summary>
    /// <exception cref="NullReferenceException">Jezeli null</exception>
    public virtual string Namespace => GetType().Namespace ??
                                       throw new NullReferenceException(
                                           "Namespace is null. Please contact plugin creator");

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

    #region Enable and Disable

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
            Log.Error(e);
        }

        SetupModuleManagers();

        OnEnabled();


        IsActive = true;

        Log.Info($"Moduł {Name} włączony.");
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

        HarmonyManager.Disable();

        CommandsManager.Disable();

        ManagersManager.Disable();


        Log.Debug($"Moduł {Name} wyłączony.");
    }

    #endregion

    /// <summary>
    ///     Restart rundy
    /// </summary>
    public void ResetRoundState()
    {
        OnRoundCleanup();
    }

    internal void Init(Func<bool>? debugProvider = null)
    {
        DebugProvider = debugProvider;
        Log ??= new ModuleLog(Name, Assembly.GetName().Name, () => IsDebugEnabled);
        CommandsManager = new CommandsManager(this);
        HarmonyManager = new HarmonyManager(this);
        ManagersManager = new ManagersManager(this);
    }

    private void SetupModuleManagers()
    {
        if (EnableHarmony)
        {
            try
            {
                HarmonyManager.Enable();
            }
            catch (Exception e)
            {
                Log.Error($"Couldn't patch harmony: {e}");
            }
        }

        if (CommandsEnabled)
        {
            try
            {
                CommandsManager.Enable();
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        if (ManagersToRegister.Length > 0)
        {
            try
            {
                ManagersManager.Enable();
            }
            catch (Exception e)
            {
                Log.Error($"Couldn't register managers: {e}");
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
                Log.Error(e);
            }
        }
    }


    /// <summary>
    ///     Ustawia te <see cref="IModuleComponent" /> moduły
    /// </summary>
    private void SetModuleInComponents()
    {
        IEnumerable<Type> componentTypes = CachedTypes
            .Where(static t =>
                typeof(IModuleComponent).IsAssignableFrom(t));

        foreach (PropertyInfo? prop in componentTypes.Select(GetModuleProperty))
        {
            prop?.SetValue(null, this);
        }
    }


    private void LookForTypes()
    {
        CachedTypes = Assembly.GetTypes()
            .Where(t => t.Namespace != null &&
                        t.Namespace.StartsWith(Namespace)
                        && t.GetCustomAttribute<DisableAutoRegister>() == null
                        && (!t.IsAbstract || t.IsSealed));
    }

    #region Custom Managers

    /// <summary>
    /// Jakie <see cref="IModuleManager"/> maja zostac dodane.
    /// </summary>
    public virtual Type[] ManagersToRegister => Array.Empty<Type>();

    #endregion

    #region Harmony

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
    public IHarmonyManager HarmonyManager { get; private set; } = null!;

    #endregion

    #region Debug

    /// <summary>
    ///     No czy ma byc debug
    /// </summary>
    internal Func<bool>? DebugProvider { get; set; }

    /// <summary>
    ///     Czy jest włączony debug
    /// </summary>
    public virtual bool IsDebugEnabled => DebugProvider != null && DebugProvider();

    #endregion

    #region Commands

    /// <summary>
    /// Do komend
    /// </summary>
    public ICommandsManager CommandsManager { get; private set; } = null!;

    /// <summary>
    ///     Czy moduł ma szukać komend
    /// </summary>
    public virtual bool CommandsEnabled => false;

    /// <summary>
    ///     Czy ma się wyswietlac w komendach
    /// </summary>
    public virtual bool HideFromCommands { get; } = false;

    #endregion

    #region SubManagers

    public IManagersManager ManagersManager { get; set; } = null!;

    public T? GetManager<T>() where T : class, IModuleManager
    {
        return ManagersManager.GetManager<T>();
    }


    public bool TryGetManager<T>([NotNullWhen(true)] out T? manager) where T : class, IModuleManager
    {
        return ManagersManager.TryGetManager(out manager);
    }

    #endregion


    #region User lifecycle

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

    #endregion
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
    Highest = 5,
}