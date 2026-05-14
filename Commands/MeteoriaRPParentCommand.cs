using CommandSystem;
using JetBrains.Annotations;
using LabApi.Loader.Features.Plugins;
using System.Diagnostics.CodeAnalysis;

namespace PluginModules.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[UsedImplicitly]
public class MeteoriaRPParentCommand : ParentCommand, IUsageProvider
{
    private const string GoodColor = "#658C58";
    private const string BadColor = "#BF1A1A";

    public MeteoriaRPParentCommand()
    {
        LoadGeneratedCommands();
    }

    private static Plugin ParentPlugin => PluginModules.Instance;

    public override string Command { get; } = "meteoriarp";
    public override string[] Aliases { get; } = [];
    public override string Description { get; } = "Komenda od PluginModules";
    public string[] Usage { get; } = ["list / info / enable / disable / reload"];

    public sealed override void LoadGeneratedCommands()
    {
        RegisterCommand(new ListCommand());
        RegisterCommand(new InfoCommand());
        RegisterCommand(new DisableCommand());
        RegisterCommand(new EnableCommand());
        RegisterCommand(new ReloadCommand());
    }

    protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender,
        [UnscopedRef] out string response)
    {
        response = $"{ParentPlugin} by {ParentPlugin.Author} | Version : {ParentPlugin.Version}";
        return true;
    }

    public static string Good(string text)
    {
        return $"<color={GoodColor}>{text}</color>";
    }

    public static string Bad(string text)
    {
        return $"<color={BadColor}>{text}</color>";
    }
}