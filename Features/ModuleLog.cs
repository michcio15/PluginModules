using PluginModules.Helpers;

namespace PluginModules.Features;

public class ModuleLog
{
    public ModuleLog(string moduleName, string prefix, bool debugEnabled)
    {
        ModuleName = moduleName;
        Prefix = prefix;
        IsDebugEnabled = debugEnabled;
    }

    public string Prefix { get; }
    public string ModuleName { get; }
    public bool IsDebugEnabled { get; }

    public void Info(object value)
    {
        CoreLog.Info(ModuleName, value, Prefix);
    }

    public void Warn(object value)
    {
        CoreLog.Warn(ModuleName, value, Prefix);
    }

    public void Error(object value)
    {
        CoreLog.Error(ModuleName, value, Prefix);
    }

    public void Debug(object value)
    {
        if (!IsDebugEnabled)
        {
            return;
        }

        CoreLog.Debug(ModuleName, value, Prefix);
    }
}