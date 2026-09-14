using CommandSystem;
using LabApi.Features.Enums;

namespace PluginModules.Features.Interface;

public interface ICommandsManager : IModuleManager
{
    IReadOnlyCollection<ICommand> ClientRegisteredCommands { get; }
    IReadOnlyCollection<ICommand> RemoteAdminRegisteredCommands { get; }
    IReadOnlyCollection<ICommand> ConsoleRegisteredCommands { get; }
    bool TryRegisterCommand(CommandType commandType, ICommand command);

    bool TryUnregisterCommand(ICommand command, ICommandHandler handler);
}