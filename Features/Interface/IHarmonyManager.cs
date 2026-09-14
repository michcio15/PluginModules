using HarmonyLib;

namespace PluginModules.Features.Interface;

public interface IHarmonyManager : IModuleManager
{
    Harmony? Harmony { get; }
}