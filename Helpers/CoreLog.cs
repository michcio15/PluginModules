namespace PluginModules.Helpers;

public static class CoreLog
{
    private const string DefaultPrefix = "PluginModules";

    private static string Format(string module, object message, string prefix)
        => $"[{prefix}] [{module}] {message}";

    private static void Log(
        string module,
        object message,
        ConsoleColor color,
        string? prefix = null)
    {
        ServerConsole.AddLog(
            Format(module, message, prefix ?? DefaultPrefix),
            color);
    }

    public static void Info(string module, object message, string? prefix = null)
        => Log(module, message, ConsoleColor.Yellow, prefix);

    public static void Warn(string module, object message, string? prefix = null)
        => Log(module, message, ConsoleColor.Magenta, prefix);

    public static void Error(string module, object message, string? prefix = null)
        => Log(module, message, ConsoleColor.Red, prefix);

    public static void Debug(string module, object message, string? prefix = null)
        => Log(module, message, ConsoleColor.DarkGreen, prefix);
}