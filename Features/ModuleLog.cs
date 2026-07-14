using PluginModules.Helpers;

namespace PluginModules.Features;

public class ModuleLog
{
    public ModuleLog(string moduleName, string prefix, Func<bool> debugEnabled)
    {
        ModuleName = moduleName;
        Prefix = prefix;
        _isDebugEnabled = debugEnabled;
    }

    public ModuleLog(string moduleName, string prefix, bool debugEnabled)
        : this(moduleName, prefix, () => debugEnabled)
    {
    }

    public string Prefix { get; }
    public string ModuleName { get; }

    private readonly Func<bool> _isDebugEnabled;

    public bool IsDebugEnabled => _isDebugEnabled();

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