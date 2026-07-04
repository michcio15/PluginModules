using HarmonyLib;

namespace PluginModules.Features.Interface;

public interface IHarmonyManager
{
    Module Module { get; }
    Harmony? Harmony { get; }

    void PatchAll();

    void UnpatchAll();
}