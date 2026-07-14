using CommandSystem;
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
            response = $"Poprawne użycie: pluginmodules {Command} {this.DisplayCommandUsage()}";
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

        if (!ModuleManager.TryEnableModule(module, force: true))
        {
            response = $"Nie udało się włączyć modułu: {module.Name}";
            return false;
        }

        response = $"Włączyłeś moduł: {module.Name}";
        return true;
    }

    public string Command { get; } = "enable";
    public string[] Aliases { get; } = ["e"];
    public string Description { get; } = "Włącza moduł";
    public string[] Usage { get; } = ["nazwa modułu"];
}