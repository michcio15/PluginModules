using CommandSystem;
using NorthwoodLib.Pools;
using PluginModules.Features;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace PluginModules.Commands;

public class ListCommand : ICommand
{
    public List<string> ExiledPermissions { get; } = ["rputils.list"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        if (!sender.CheckPermission(PlayerPermissions.ServerConsoleCommands))
        {
            response = "Nie masz permisji";
            return false;
        }

        response = GetList();
        return true;
    }

    public string Command { get; } = "list";
    public string[] Aliases { get; } = ["l"];
    public string Description { get; } = "Wypisuje wszystkie moduły i ich stan";

    public static string GetList()
    {
        StringBuilder sb = StringBuilderPool.Shared.Rent();
        sb.AppendLine("<color=#98FB98>Zarejestrowane moduły: </color>");
        foreach (Module module in ModuleManager.Modules.Where(m => !m.HideFromCommands)
                     .OrderByDescending(m => m.Priority).ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
        {
            AppendModule(ref sb, module);
        }

        return StringBuilderPool.Shared.ToStringReturn(sb);
    }

    private static void AppendModule(ref StringBuilder sb, Module module)
    {
        string active = module.IsActive ? Good("Aktywny") : Bad("Nieaktywny");
        string priority = module.Priority.ToString();
        string harmony = module.EnableHarmony ? Good("Harmony Aktywne") : Bad("Harmony Nieaktywne");
        string commands;
        CommandsManager commandsManager = module.CommandsManager;
        int commandsSum = commandsManager.ClientRegisteredCommands.Count +
                          commandsManager.ConsoleRegisteredCommands.Count +
                          commandsManager.RemoteAdminRegisteredCommands.Count;
        if (commandsSum > 0)
        {
            commands = MeteoriaRPParentCommand.Good($"Komendy: {commandsSum}");
        }
        else if (!module.CommandsEnabled)
        {
            commands = MeteoriaRPParentCommand.Bad("Komendy: Wyłączone");
        }
        else
        {
            commands = MeteoriaRPParentCommand.Bad("Komendy: Brak");
        }

        sb.AppendLine($"- {module.Name} [{priority}] | {active} | {commands} | {harmony}");
    }

    private static string Good(string text)
    {
        return MeteoriaRPParentCommand.Good(text);
    }

    private static string Bad(string text)
    {
        return MeteoriaRPParentCommand.Bad(text);
    }
}