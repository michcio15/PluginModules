using CommandSystem;
using PluginModules.Features;
using PluginModules.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace PluginModules.Commands;

public class ReloadCommand : ICommand, IUsageProvider
{
    private static readonly HashSet<string> ConfigCases = ["c", "config", "conf"];

    private static readonly HashSet<string> AllCases = ["a", "all", "wszystko"];

    private static readonly HashSet<string> ModuleCases = ["m", "module", "mod"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        if (!sender.CheckPermission(PlayerPermissions.ServerConsoleCommands))
        {
            response = "Nie masz permisji";
            return false;
        }

        if (arguments.Count < 2)
        {
            response = $"Poprawne użycie: pluginmodules {Command} {this.DisplayCommandUsage()}";
            return false;
        }

        if (ConfigCases.Contains(arguments.At(0).ToLower()))
        {
            return ConfigReload(arguments.Skip(1), out response);
        }

        if (AllCases.Contains(arguments.At(0).ToLower()))
        {
            return AllReload(out response);
        }

        if (ModuleCases.Contains(arguments.At(0).ToLower()))
        {
            return ModuleReload(arguments.Skip(1), out response);
        }

        response = $"Poprawne użycie: pluginmodules {Command} {this.DisplayCommandUsage()}";
        return false;
    }

    public string Command { get; } = "reload";

    public string[] Aliases { get; } = ["r"];

    public string Description { get; } = "Ładuje ponownie config / cały moduł";

    public string[] Usage { get; } = ["all / config / module", "nazwa modułu"];


    private static bool ModuleReload(IEnumerable<string> arguments, out string response)
    {
        string moduleName = string.Join(" ", arguments);
        Module? module = ModuleManager.Modules.FirstOrDefault(m => m.Name == moduleName);
        if (module == null)
        {
            response = $"Nie udało się znaleźć modułu o nazwie: \'{moduleName}\'";
            return false;
        }

        module.Disable();
        if (!ModuleManager.TryEnableModule(module, force: true))
        {
            response = $"Nie udało się ponownie załadować modułu: '{moduleName}'";
            return false;
        }

        response = $"Pomyślnie załadowano ponownie moduł: '{moduleName}'";
        return true;
    }

    private static bool ConfigReload(IEnumerable<string> arguments, out string response)
    {
        string moduleName = string.Join(" ", arguments);
        Module? first = ModuleManager.Modules
            .FirstOrDefault(m => m.Name == moduleName);

        if (first is not IReloadableModule module)
        {
            response = $"Nie udało się znaleźć modułu o nazwie: \'{moduleName}\', który da sie zrealoadowac";
            return false;
        }

        if (!module.TryReloadConfig())
        {
            response = $"Nie udało się załadować configu modułu: '{first.Name}'";
            return false;
        }

        response = $"Pomyślnie załadowano config modułu: '{first.Name}'";
        return true;
    }

    private static bool AllReload(out string response)
    {
        ModuleManager.DisableAllModules();
        ModuleManager.EnableModules();
        response = "Załadowano ponownie wszystkie moduły";
        return true;
    }
}