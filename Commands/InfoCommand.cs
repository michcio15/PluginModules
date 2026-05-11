using CommandSystem;
using FajneConfigurables;
using FajneConfigurables.Interfaces;
using NorthwoodLib.Pools;
using PluginModules.Features;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Module = PluginModules.Features.Module;

namespace PluginModules.Commands;

public class InfoCommand : ICommand, IUsageProvider
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


        response = GetInfo(module);
        return true;
    }

    public string Command { get; } = "info";
    public string[] Aliases { get; } = ["i"];
    public string Description { get; } = "Wysyła wszystkie informacje na temat modułu";
    public List<string> ExiledPermissions { get; } = ["rputils.info"];
    public string[] Usage { get; } = ["Nazwa modułu"];

    private static string GetInfo(Module module)
    {
        StringBuilder sb = StringBuilderPool.Shared.Rent();
        sb.AppendLine("Informacje o module:");
        sb.AppendLine($"<b><color=#666699>{module.Name}</color></b>");
        string active = module.IsActive ? MeteoriaRPParentCommand.Good("Tak") : MeteoriaRPParentCommand.Bad("Nie");
        sb.AppendLine($"- Aktywny: {active}");
        sb.AppendLine($"- Priorytet : {module.Priority} ({(int)module.Priority})");
        sb.Append("- Komendy: ");
        if (module.ClientRegisteredCommands.Count + module.ConsoleRegisteredCommands.Count +
            module.RemoteAdminRegisteredCommands.Count > 0)
        {
            AppendCommands(ref sb, module);
        }
        else if (!module.CommandsEnabled)
        {
            sb.AppendLine(MeteoriaRPParentCommand.Bad("Wyłączone"));
        }
        else
        {
            sb.AppendLine(MeteoriaRPParentCommand.Bad("Brak"));
        }

        AppendHarmony(ref sb, module);
        if (module is IConfigurable configurable)
        {
            AppendConfigPath(ref sb, module, configurable);
        }


        return StringBuilderPool.Shared.ToStringReturn(sb);
    }

    private static string EnabledOrNo(bool value)
    {
        return value ? MeteoriaRPParentCommand.Good("Tak") : MeteoriaRPParentCommand.Bad("Nie");
    }

    private static void AppendCommands(ref StringBuilder sb, Module module)
    {
        sb.AppendLine(MeteoriaRPParentCommand.Good("Tak"));
        sb.Append("<b>Komendy Clienta");
        AppendHandler(ref sb, module.ClientRegisteredCommands);
        sb.Append("<b>Komendy RA");
        AppendHandler(ref sb, module.RemoteAdminRegisteredCommands);
        sb.Append("<b>Komendy Servera");
        AppendHandler(ref sb, module.ConsoleRegisteredCommands);
    }

    private static void AppendHandler(ref StringBuilder sb, IEnumerable<ICommand> commands)
    {
        List<ICommand> enumerable = commands.ToList();
        if (commands == null || !enumerable.Any())
        {
            sb.AppendLine(" [0] : </b>");
            sb.AppendLine(MeteoriaRPParentCommand.Bad("    - Brak"));
            return;
        }

        sb.AppendLine($" [{enumerable.Count}] : </b>");
        foreach (ICommand command in enumerable)
        {
            sb.AppendLine($"    - {command.Command} | {command.Description}");
            if (command is ParentCommand parentCommand && parentCommand.AllCommands.Any())
            {
                AppendChildCommands(ref sb, parentCommand.AllCommands);
            }
        }
    }

    private static void AppendChildCommands(ref StringBuilder sb, IEnumerable<ICommand> commands, int depth = 0)
    {
        if (depth > 10)
        {
            return;
        }

        foreach (ICommand command in commands)
        {
            if (command is ParentCommand parentCommand && parentCommand.AllCommands.Any())
            {
                AppendChildCommands(ref sb, parentCommand.AllCommands, depth + 1);
            }

            sb.AppendLine($"        - {command.Command} | {command.Description}");
        }
    }

    private static void AppendHarmony(ref StringBuilder sb, Module module)
    {
        sb.Append($"- Harmony: {EnabledOrNo(module.EnableHarmony)}");
        if (!module.EnableHarmony || module.Patches == null || !module.Patches.Any())
        {
            sb.AppendLine();
            return;
        }

        sb.AppendLine($" [{module.Patches.Count()}] : ");
        foreach (MethodBase methodBase in module.Patches)
        {
            sb.AppendLine($"    - {methodBase.DeclaringType?.Name}.{methodBase.Name}");
        }
    }

    private static void AppendConfigPath(ref StringBuilder sb, Module module, IConfigurable configurable)
    {
        /*string path =
            $"{MeteoriaRPPlugin.Instance.Prefix ?? MeteoriaRPPlugin.Instance.Name.ToLower()}/{MeteoriaRPPlugin.ModulesConfigFolderName}";*/
        string toAdd = $"{ConfigLoader.FormatFileName(module.Name)}.yml";
        string path = "";

        path = configurable.Path == null ? $"{path}/{toAdd}" : $"{path}/{string.Join("/", configurable.Path)}/{toAdd}";

        sb.AppendLine($"Ścieżka do configu: {path}");
    }
}