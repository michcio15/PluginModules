namespace PluginModules.Helpers;

public static class CoreLog
{
    private static string Format(string module, object msg)
    {
        return $"[MeteoriaRP] [{module}] {msg}";
    }

    public static void Info(string moduleName, object message)
    {
        ServerConsole.AddLog(Format(moduleName, message), ConsoleColor.Yellow);
    }

    public static void Warn(string moduleName, object message)
    {
        ServerConsole.AddLog(Format(moduleName, message), ConsoleColor.Magenta);
    }

    public static void Error(string moduleName, object message)
    {
        ServerConsole.AddLog(Format(moduleName, message), ConsoleColor.Red);
    }

    public static void Debug(string moduleName, object message)
    {
        ServerConsole.AddLog(Format(moduleName, message), ConsoleColor.DarkGreen);
    }
}