using CommandSystem;
using LabApi.Features.Permissions;
using PluginModules.Features;
using System.Diagnostics.CodeAnalysis;

namespace PluginModules.Commands;

public class EnableCommand : ICommand, IUsageProvider
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

        if (module.IsActive)
        {
            response = "Moduł jest już aktywny";
            return false;
        }

        module.Enable();
        response = $"Włączyłeś moduł: {module.Name}";
        return true;
    }

    public string Command { get; } = "enable";
    public string[] Aliases { get; } = ["e"];
    public string Description { get; } = "Włącza moduł";
    public List<string> ExiledPermissions { get; } = ["rputils.enable"];
    public string[] Usage { get; } = ["nazwa modułu"];
}