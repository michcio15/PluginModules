using CommandSystem;
using JetBrains.Annotations;
using LabApi.Features.Enums;
using PluginModules.Attributes;
using PluginModules.Features.Commands.Interfaces;
using PluginModules.Features.Interface;
using RemoteAdmin;
using System.Reflection;
using Console = GameCore.Console;


namespace PluginModules.Features.Managers;

[PublicAPI]
public class CommandsManager : ICommandsManager
{
    private readonly HashSet<ICommand> _clientCommands = new();

    private readonly HashSet<ICommand> _consoleCommands = new();

    private readonly HashSet<ICommand> _remoteAdminCommands = new();

    public CommandsManager(Module module)
    {
        Module = module;
    }

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

    public void Enable()
    {
        foreach (Type type in GetCommandTypes())
        {
            Log.Debug($"Registering command {type.Name}");
            ModuleCommandAttribute attr = type.GetCustomAttribute<ModuleCommandAttribute>();
            if (Activator.CreateInstance(type) is not ICommand command)
            {
                Log.Error($"Typ {type.Name} nie implementuje ICommand");
                continue;
            }

            TryRegisterCommand(attr.CommandType, command);
        }
    }


    public void Disable()
    {
        foreach (ICommand clientCommand in _clientCommands.ToArray())
        {
            _clientCommands.Remove(clientCommand);
            QueryProcessor.DotCommandHandler.UnregisterCommand(clientCommand);
            Log.Debug($"Unregistered client command {clientCommand.Command}");
        }

        foreach (ICommand remoteAdminCommand in _remoteAdminCommands.ToArray())
        {
            _remoteAdminCommands.Remove(remoteAdminCommand);
            CommandProcessor.RemoteAdminCommandHandler.UnregisterCommand(remoteAdminCommand);
            Log.Debug($"Unregistered remote admin command {remoteAdminCommand.Command}");
        }

        foreach (ICommand consoleCommand in _consoleCommands.ToArray())
        {
            _consoleCommands.Remove(consoleCommand);
            Console.ConsoleCommandHandler.UnregisterCommand(consoleCommand);
            Log.Debug($"Unregistered console command {consoleCommand.Command}");
        }
    }

    public bool TryUnregisterCommand(ICommand command, ICommandHandler handler)
    {
        switch (handler)
        {
            case RemoteAdminCommandHandler remoteAdminCommandHandler:
                remoteAdminCommandHandler.UnregisterCommand(command);
                return _remoteAdminCommands.Remove(command);
            case ClientCommandHandler clientCommandHandler:
                clientCommandHandler.UnregisterCommand(command);
                return _clientCommands.Remove(command);
            case GameConsoleCommandHandler consoleCommandHandler:
                consoleCommandHandler.UnregisterCommand(command);
                return _consoleCommands.Remove(command);
            default:
                return false;
        }
    }

    /// <summary>
    ///     Registeruje komende do modułu
    /// </summary>
    /// <param name="commandType"><see cref="CommandType" /> tej komendy</param>
    /// <param name="command"><see cref="ICommand" /> komenda</param>
    public bool TryRegisterCommand(CommandType commandType, ICommand command)
    {
        bool success = false;
        try
        {
            switch (commandType)
            {
                case CommandType.RemoteAdmin:
                    CommandProcessor.RemoteAdminCommandHandler.RegisterCommand(command);
                    _remoteAdminCommands.Add(command);
                    success = true;
                    break;
                case CommandType.Console:
                    Console.ConsoleCommandHandler.RegisterCommand(command);
                    _consoleCommands.Add(command);
                    success = true;
                    break;
                case CommandType.Client:
                    QueryProcessor.DotCommandHandler.RegisterCommand(command);
                    _clientCommands.Add(command);
                    success = true;
                    break;
            }
        }
        catch (Exception e)
        {
            Log.Error($"Nie udało się zarejestrować {command.Command} stacktrace zjebania {e.Message}");
            success = false;
            return success;
        }

        if (command is IHasModule hasModule)
        {
            hasModule.SetModule(Module);
        }

        return success;
    }

    public Module Module { get; }
    [Obsolete("Use Log instead", true)]
    public ModuleLog ModuleLog => Log;

    public ModuleLog Log => Module.Log;

    private IEnumerable<Type> GetCommandTypes()
    {
        return Module.CachedTypes.Where(static t =>
            t.GetCustomAttribute<ModuleCommandAttribute>() != null && typeof(ICommand).IsAssignableFrom(t));
    }
}