using LabApi.Features;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader.Features.Plugins.Enums;
using PluginModules.Features;

namespace PluginModules;

public class PluginModules : Plugin<Config>
{
    public override string Name { get; } = "PluginModules";
    public override string Description { get; } = "Plugin for modules";
    public override string Author { get; } = "michcio";
    public override Version Version { get; } = new Version(1, 0, 0);
    public override Version RequiredApiVersion { get; } = new Version(LabApiProperties.CompiledVersion);
    public static new Config Config { get; private set; } = null!;
    public static PluginModules Instance { get; private set; } = null!;
    public override LoadPriority Priority { get; } = LoadPriority.Highest;

    public override void Enable()
    {
        ModuleManager.RegisterEvents();
    }

    public override void Disable()
    {
        ModuleManager.DisableAll();
    }
}