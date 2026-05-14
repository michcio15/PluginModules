using CommandSystem;
using LabApi.Features.Enums;
using PluginModules.Attributes;
using PluginModules.Features.Commands.Interfaces;
using RemoteAdmin;
using System.Reflection;
using Console = GameCore.Console;


namespace PluginModules.Features;

public sealed class CommandsManager
{
    private readonly HashSet<ICommand> _clientCommands = new();

    private readonly HashSet<ICommand> _consoleCommands = new();

    private readonly HashSet<ICommand> _remoteAdminCommands = new();

    public CommandsManager(Module module)
    {
        Module = module;
    }

    public Module Module { get; }
    public ModuleLog ModuleLog => Module.ModuleLog;

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

    internal void RegisterCommands()
    {
        foreach (Type type in GetCommandTypes())
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


    internal void UnregisterCommands()
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

        foreach (ICommand consoleCommand in _consoleCommands.ToArray())
        {
            _consoleCommands.Remove(consoleCommand);
            Console.ConsoleCommandHandler.UnregisterCommand(consoleCommand);
            ModuleLog.Debug($"Unregistered console command {consoleCommand.Command}");
        }
    }

    private IEnumerable<Type> GetCommandTypes()
    {
        return Module.CachedTypes.Where(t =>
            t.GetCustomAttribute<ModuleCommandAttribute>() != null && typeof(ICommand).IsAssignableFrom(t));
    }

    /// <summary>
    ///     Registeruje komende do modułu
    /// </summary>
    /// <param name="commandType"><see cref="CommandType" /> tej komendy</param>
    /// <param name="command"><see cref="ICommand" /> komenda</param>
    public void TryRegisterCommand(CommandType commandType, ICommand command)
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
            hasModule.SetModule(Module);
        }
    }
}