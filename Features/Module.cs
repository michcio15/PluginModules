using CommandSystem;
using HarmonyLib;
using JetBrains.Annotations;
using LabApi.Features.Enums;
using MEC;
using PluginModules.Attributes;
using PluginModules.Features.Commands.Interfaces;
using PluginModules.Features.Components;
using PluginModules.Helpers;
using RemoteAdmin;
using System.Reflection;
using Console = GameCore.Console;

namespace PluginModules.Features;

/// <summary>
///     No to ten główny budulec
/// </summary>
[MeansImplicitUse]
[PublicAPI]
public abstract class Module
{
    private HashSet<ICommand> _clientCommands = new();

    private HashSet<ICommand> _consoleCommands = new();

    private HashSet<ICommand> _remoteAdminCommands = new();

    //private readonly ListCommand<RPFeature> _features = [];
    /// <summary>
    ///     <see cref="Harmony" /> tego modułu
    /// </summary>
    protected Harmony? HarmonyInstance;

    /// <summary>
    ///     Spatchowane metody przez <see cref="HarmonyInstance" />
    /// </summary>
    public IEnumerable<MethodBase>? Patches => HarmonyInstance?.GetPatchedMethods();

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
    ///     Namespace używany przez harmony modułu
    /// </summary>
    protected virtual string HarmonyNamespace => GetType().Namespace!;

    /// <summary>
    /// No czy ma byc debug
    /// </summary>
    internal Func<bool>? DebugProvider { get; set; }

    /// <summary>
    ///     Czy jest włączony debug
    /// </summary>
    public virtual bool IsDebugEnabled => DebugProvider?.Invoke() == true;

    /// <summary>
    ///     Tag korutyny do łatwiejszego usuwania.
    /// </summary>
    private string CoroutineTag => $"RPModule_{Name}";

    /// <summary>
    ///     Czy moduł ma szukać komend
    /// </summary>
    public virtual bool CommandsEnabled { get; } = false;

    /// <summary>
    ///     Namespace do szukania komend
    /// </summary>
    protected virtual string CommandsNamespace => GetType().Namespace!;

    /// <summary>
    ///     Czy moduł ma jakies <see cref="IModuleComponent" />
    /// </summary>
    public virtual bool ComponentsEnabled { get; } = false;

    /// <summary>
    ///     Namespace do szukania <see cref="IModuleComponent" />
    /// </summary>
    protected virtual string ComponentsNamespace => GetType().Namespace!;

    /// <summary>
    ///     <see cref="IReadOnlyCollection{T}" /> zarejestrowanych komend <see cref="RemoteAdminCommandHandler" />
    /// </summary>
    public IReadOnlyCollection<ICommand> RemoteAdminRegisteredCommands => _remoteAdminCommands;

    /// <summary>
    ///     <see cref="IReadOnlyCollection{T}" /> zarejestrowanych komend <see cref="ClientCommandHandler" />
    /// </summary>
    public IReadOnlyCollection<ICommand> ClientRegisteredCommands => _clientCommands;

    /// <summary>
    ///     <see cref="IReadOnlyCollection{T}" /> zarejestrowanych komend <see cref="GameConsoleCommandHandler" />
    /// </summary>
    public IReadOnlyCollection<ICommand> ConsoleRegisteredCommands => _consoleCommands;

    /// <summary>
    ///     Czy ma się wyswietlac w komendach
    /// </summary>
    public virtual bool HideFromCommands { get; } = false;

    /// <summary>
    /// Zwraca assembly tego modułu
    /// </summary>
    public Assembly Assembly => GetType().Assembly;

    /// <summary>
    ///     Zajmuje się uruchamianiem i przygotowywaniem modułu
    /// </summary>
    public virtual void Enable()
    {
        if (IsActive)
        {
            return;
        }

        ModuleLog ??= new ModuleLog(Name);


        SafeRunner.Execute(() =>
        {
            if (EnableHarmony)
            {
                HarmonyInstance = new Harmony($"com.rp.{Name}");
                bool patchesFound = false;

                IEnumerable<Type> patchTypes = GetType().Assembly.GetTypes()
                    .Where(t => t.Namespace != null &&
                                t.Namespace.StartsWith(HarmonyNamespace) &&
                                t.GetCustomAttribute<HarmonyPatch>() != null &&
                                t.GetCustomAttribute<DisableAutoRegister>() == null);

                foreach (Type type in patchTypes)
                {
                    try
                    {
                        HarmonyInstance.CreateClassProcessor(type).Patch();
                        patchesFound = true;
                    }
                    catch (Exception e)
                    {
                        ModuleLog.Error($"Nie udało się załadować patcha {type.Name}: {e}");
                    }
                }

                if (IsDebugEnabled)
                {
                    ModuleLog.Debug(patchesFound
                        ? $"Załadowano patche z namespace: {HarmonyNamespace}."
                        : $"Nie znaleziono patchy w namespace: {HarmonyNamespace}.");
                }
            }

            if (CommandsEnabled)
            {
                try
                {
                    RegisterCommands();
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

            if (IsDebugEnabled)
            {
                ModuleLog.Info($"Moduł {Name} włączony.");
            }
        }, Name, "Enable");
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

        SafeRunner.Execute(() =>
        {
            OnDisabled();

            HarmonyInstance?.UnpatchAll(HarmonyInstance.Id);
            HarmonyInstance = null;

            ResetRoundState();
            IsActive = false;

            if (IsDebugEnabled)
            {
                ModuleLog.Debug($"Moduł {Name} wyłączony.");
            }

            UnregisterCommands();
        }, Name, "Disable");
    }


    /// <summary>
    ///     Uruchamia korutynę z tagiem
    /// </summary>
    /// <param name="coroutine"><see cref="IEnumerator{float}" />, do uruchomienia</param>
    /// <returns><see cref="CoroutineHandle" /> korutyny</returns>
    internal CoroutineHandle RunCoroutine(IEnumerator<float> coroutine)
    {
        return Timing.RunCoroutine(coroutine, CoroutineTag);
    }

    /// <summary>
    ///     Ustawia te <see cref="IModuleComponent" /> moduły
    /// </summary>
    private void SetModuleInComponents()
    {
        IEnumerable<Type> componentTypes = GetType().Assembly.GetTypes()
            .Where(t => t.Namespace != null &&
                        t.GetInterfaces().Contains(typeof(IModuleComponent)) &&
                        t.Namespace.StartsWith(ComponentsNamespace) &&
                        t.GetCustomAttribute<DisableAutoRegister>() == null);

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
        Timing.KillCoroutines(CoroutineTag);


        SafeRunner.Execute(OnRoundCleanup, Name, "OnRoundCleanup");
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

    /// <summary>
    ///     Rejestrowanie featurów
    /// </summary>
    protected virtual void RegisterFeatures() { }

    private void RegisterCommands()
    {
        IEnumerable<Type> commandTypes = GetType().Assembly.GetTypes()
            .Where(t => t.Namespace != null &&
                        t.Namespace.StartsWith(CommandsNamespace) &&
                        t.GetCustomAttribute<ModuleCommandAttribute>() != null &&
                        t.GetCustomAttribute<DisableAutoRegister>() == null);
        foreach (Type type in commandTypes)
        {
            ModuleCommandAttribute attr = type.GetCustomAttribute<ModuleCommandAttribute>();
            if (Activator.CreateInstance(type) is not ICommand command)
            {
                ModuleLog.Error($"Typ {type.Name} nie implementuje ICommand");
                continue;
            }

            TryRegisterCommand(attr.CommandType, command);
        }
    }


    private void UnregisterCommands()
    {
        foreach (ICommand clientCommand in _clientCommands.ToArray())
        {
            _clientCommands.Remove(clientCommand);
            QueryProcessor.DotCommandHandler.UnregisterCommand(clientCommand);
            ModuleLog.Debug($"Unregistered client command {clientCommand.Command}");
        }

        foreach (ICommand remoteAdminCommand in _remoteAdminCommands.ToArray())
        {
            _remoteAdminCommands.Remove(remoteAdminCommand);
            CommandProcessor.RemoteAdminCommandHandler.UnregisterCommand(remoteAdminCommand);
            ModuleLog.Debug($"Unregistered remote admin command {remoteAdminCommand.Command}");
        }

        foreach (ICommand consoleCommand in _consoleCommands)
        {
            _consoleCommands.Remove(consoleCommand);
            Console.ConsoleCommandHandler.UnregisterCommand(consoleCommand);
            ModuleLog.Debug($"Unregistered console command {consoleCommand.Command}");
        }
    }

    /// <summary>
    ///     Registeruje komende do modułu
    /// </summary>
    /// <param name="commandType"><see cref="CommandType" /> tej komendy</param>
    /// <param name="command"><see cref="ICommand" /> komenda</param>
    protected void TryRegisterCommand(CommandType commandType, ICommand command)
    {
        try
        {
            switch (commandType)
            {
                case CommandType.RemoteAdmin:
                    CommandProcessor.RemoteAdminCommandHandler.RegisterCommand(command);
                    _remoteAdminCommands.Add(command);
                    break;
                case CommandType.Console:
                    Console.ConsoleCommandHandler.RegisterCommand(command);
                    _consoleCommands.Add(command);
                    break;
                case CommandType.Client:
                    QueryProcessor.DotCommandHandler.RegisterCommand(command);
                    _clientCommands.Add(command);
                    break;
            }
        }
        catch (Exception e)
        {
            ModuleLog.Error($"Nie udało się zarejestrować {command.Command} stacktrace zjebania {e.Message}");
        }

        if (command is IHasModule hasModule)
        {
            hasModule.SetModule(this);
        }
    }

    protected Harmony CreateHarmonyInstance()
    {
        HarmonyInstance?.UnpatchAll(HarmonyInstance.Id);
        return new Harmony($"com.rp.{Name}");
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