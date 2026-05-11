using PluginModules.Helpers;

namespace PluginModules.Features;

public class ModuleLog
{
    public ModuleLog(string prefix)
    {
        Prefix = prefix;
    }

    public string Prefix { get; }

    public void Info(object value)
    {
        CoreLog.Info(Prefix, value);
    }

    public void Warn(object value)
    {
        CoreLog.Warn(Prefix, value);
    }

    public void Error(object value)
    {
        CoreLog.Error(Prefix, value);
    }

    public void Debug(object value)
    {
        CoreLog.Debug(Prefix, value);
    }
}