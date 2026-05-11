using CommandSystem;
using LabApi.Features.Enums;

namespace PluginModules.Attributes;

/// <inheritdoc />
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class
    ModuleCommandAttribute : Attribute
{
    /// <inheritdoc />
    public ModuleCommandAttribute(Type commandHandler)
    {
        CommandType = commandHandler switch
        {
            not null when commandHandler == typeof(RemoteAdminCommandHandler)
                => CommandType.RemoteAdmin,

            not null when commandHandler == typeof(ClientCommandHandler)
                => CommandType.Client,

            not null when commandHandler == typeof(GameConsoleCommandHandler)
                => CommandType.Console,

            _ => throw new ArgumentException("Musi być command handler")
        };
    }

    public ModuleCommandAttribute(CommandType commandType)
    {
        CommandType = commandType;
    }

    /// <summary>
    ///     Typ <see cref="CommandHandler" />
    /// </summary>
    public CommandType CommandType { get; }
}