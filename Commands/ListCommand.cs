using CommandSystem;
using NorthwoodLib.Pools;
using PluginModules.Features;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Module = PluginModules.Features.Module;

namespace PluginModules.Commands;

public class ListCommand : ICommand
{
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
        foreach ((Assembly assembly, HashSet<Module> modules) in ModuleManager.AssembliesModules)
        {
            sb.AppendLine($"<color=#a8dcff>{assembly.GetName().Name}:</color>");
            foreach (Module module in modules.Where(m => !m.HideFromCommands)
                         .OrderByDescending(m => m.Priority).ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
            {
                AppendModule(ref sb, module);
            }
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
            commands = PluginModulesParentCommand.Good($"Komendy: {commandsSum}");
        }
        else if (!module.CommandsEnabled)
        {
            commands = PluginModulesParentCommand.Bad("Komendy: Wyłączone");
        }
        else
        {
            commands = PluginModulesParentCommand.Bad("Komendy: Brak");
        }

        sb.AppendLine($"- {module.Name} [{priority}] | {active} | {commands} | {harmony}");
    }

    private static string Good(string text)
    {
        return PluginModulesParentCommand.Good(text);
    }

    private static string Bad(string text)
    {
        return PluginModulesParentCommand.Bad(text);
    }
}