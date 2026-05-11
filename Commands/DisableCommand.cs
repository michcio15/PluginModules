using CommandSystem;
using PluginModules.Features;
using System.Diagnostics.CodeAnalysis;

namespace PluginModules.Commands;

public class DisableCommand : ICommand, IUsageProvider
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        if (!sender.CheckPermission(PlayerPermissions.ServerConsoleCommands))
        {
            response = "Nie masz permisji";
            return false;
        }

        if (arguments.Count < 1)
        {
            response = $"Poprawne użycie: rputils {Command} {this.DisplayCommandUsage()}";
            return false;
        }

        string moduleName = string.Join(" ", arguments);
        Module? module = ModuleManager.Modules.FirstOrDefault(m => m.Name == moduleName);
        if (module == null)
        {
            response = $"Nie udało się znaleźć modułu o nazwie: \'{moduleName}\'";
            return false;
        }

        if (!module.IsActive)
        {
            response = "Moduł jest już nieaktywny";
            return false;
        }

        module.Disable();
        response = $"Wyłączyłeś moduł: {module.Name}";
        return true;
    }

    public string Command { get; } = "disable";
    public string[] Aliases { get; } = ["d"];
    public string Description { get; } = "Wyłącza moduł";
    public List<string> ExiledPermissions { get; } = ["rputils.disable"];
    public string[] Usage { get; } = ["nazwa modułu"];
}