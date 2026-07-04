using CommandSystem;
using LabApi.Features.Enums;

namespace PluginModules.Features.Interface;

public interface ICommandsManager
{
    void RegisterCommands();

    void UnregisterCommands();

    bool TryRegisterCommand(CommandType commandType, ICommand command);

    bool TryUnregisterCommand(ICommand command, ICommandHandler handler);
    IReadOnlyCollection<ICommand> ClientRegisteredCommands { get; }
    IReadOnlyCollection<ICommand> RemoteAdminRegisteredCommands { get; }
    IReadOnlyCollection<ICommand> ConsoleRegisteredCommands { get; }
}