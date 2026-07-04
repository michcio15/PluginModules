using CommandSystem;
using FajneConfigurables;
using FajneConfigurables.Interfaces;
using NorthwoodLib.Pools;
using PluginModules.Features;
using PluginModules.Features.Interface;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Module = PluginModules.Features.Module;

namespace PluginModules.Commands;

public class InfoCommand : ICommand, IUsageProvider
{
    private static readonly Dictionary<string, string> TypeAliases = new()
    {
        { "Boolean", "bool" },
        { "Byte", "byte" },
        { "SByte", "sbyte" },
        { "Int16", "short" },
        { "UInt16", "ushort" },
        { "Int32", "int" },
        { "UInt32", "uint" },
        { "Int64", "long" },
        { "UInt64", "ulong" },
        { "Single", "float" },
        { "Double", "double" },
        { "Decimal", "decimal" },
        { "Char", "char" },
        { "String", "string" },
        { "Object", "object" },
        { "Void", "void" },
    };

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
        Module? module = ModuleManager.Modules.FirstOrDefault(m =>
            !m.HideFromCommands && string.Equals(m.Name, moduleName, StringComparison.InvariantCultureIgnoreCase));
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
    public string[] Usage { get; } = ["Nazwa modułu"];

    private static string GetInfo(Module module)
    {
        StringBuilder sb = StringBuilderPool.Shared.Rent();
        sb.AppendLine("Informacje o module:");
        sb.AppendLine($"<b><color=#666699>{module.Name}</color></b>");
        sb.AppendLine($"- Aktywny: {EnabledOrNo(module.IsActive)}");
        sb.AppendLine($"- Priorytet : {module.Priority} ({(int)module.Priority})");
        sb.AppendLine($"- Assembly : {module.Assembly.GetName().Name}");
        sb.AppendLine($"- Debug : {EnabledOrNo(module.IsDebugEnabled)}");
        sb.Append("- Komendy: ");

        ICommandsManager commandsManager = module.CommandsManager;

        if (commandsManager.ClientRegisteredCommands.Count + commandsManager.ConsoleRegisteredCommands.Count +
            commandsManager.RemoteAdminRegisteredCommands.Count > 0)
        {
            AppendCommands(ref sb, module);
        }
        else if (!module.CommandsEnabled)
        {
            sb.AppendLine(PluginModulesParentCommand.Bad("Wyłączone"));
        }
        else
        {
            sb.AppendLine(PluginModulesParentCommand.Bad("Brak"));
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
        return value ? PluginModulesParentCommand.Good("Tak") : PluginModulesParentCommand.Bad("Nie");
    }

    private static void AppendCommands(ref StringBuilder sb, Module module)
    {
        sb.AppendLine(PluginModulesParentCommand.Good("Tak"));
        sb.Append("<b>Komendy Clienta");
        AppendHandler(ref sb, module.CommandsManager.ClientRegisteredCommands);
        sb.Append("<b>Komendy RA");
        AppendHandler(ref sb, module.CommandsManager.RemoteAdminRegisteredCommands);
        sb.Append("<b>Komendy Serwera");
        AppendHandler(ref sb, module.CommandsManager.ConsoleRegisteredCommands);
    }

    private static void AppendHandler(ref StringBuilder sb, IEnumerable<ICommand> commands)
    {
        List<ICommand> enumerable = commands.ToList();
        if (commands == null || !enumerable.Any())
        {
            sb.AppendLine(" [0] : </b>");
            sb.AppendLine(PluginModulesParentCommand.Bad("    - Brak"));
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
            sb.AppendLine($"    - {FormatPatch(methodBase)}");
        }
    }

    private static string FormatPatch(MethodBase method)
    {
        string assembly = method.DeclaringType?.Assembly.GetName().Name ?? "?";
        string ns = method.DeclaringType?.FullName ?? "?";
        string prefix = method.IsStatic ? "." : "::";
        string name = $"{prefix}{method.Name}";
        string args = string.Join(", ", method.GetParameters().Select(p => $"{FormatTypeName(p.ParameterType)} {p.Name}"));
        return $"[{assembly}] {ns}{name}({args})";
    }

    private static string FormatTypeName(Type type)
        => TypeAliases.TryGetValue(type.Name, out string? alias) ? alias : type.Name;

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