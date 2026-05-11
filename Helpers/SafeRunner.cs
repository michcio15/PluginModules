namespace PluginModules.Helpers;

public static class SafeRunner
{
    public static void Execute(Action action, string moduleName, string actionName = "Unknown")
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            CoreLog.Error("SafeRunner", $"BŁĄD w module '{moduleName}' podczas '{actionName}':\n{ex}");
        }
    }
}