using HarmonyLib;
using JetBrains.Annotations;
using PluginModules.Features.Interface;
using System.Reflection;

namespace PluginModules.Features.Managers;

[PublicAPI]
public class HarmonyManager : IHarmonyManager
{
    public HarmonyManager(Module module)
    {
        Module = module;
    }

    public Harmony? Harmony { get; set; } = null;

    public Module Module { get; }

    public void Enable()
    {
        bool patchesFound = false;

        IEnumerable<Type> patchTypes = Module.CachedTypes.Where(static t =>
            t.GetCustomAttribute<HarmonyPatch>() != null);

        Harmony ??= new Harmony(HarmonyName);

        foreach (Type type in patchTypes)
        {
            TryPatch(type, Harmony, ref patchesFound);
        }


        Log.Debug(patchesFound
            ? $"Załadowano patche z namespace: {Namespace}."
            : $"Nie znaleziono patchy w namespace: {Namespace}.");
    }

    public void Disable()
    {
        Harmony?.UnpatchAll(HarmonyName);
        Harmony = null;
    }

    [Obsolete("Use Log instead", true)]
    public ModuleLog ModuleLog => Log;

    public ModuleLog Log => Module.Log;
    public string Namespace => Module.Namespace!;
    public string HarmonyName => $"com.module.{Module.Name}";

    protected virtual bool TryPatch(Type type, Harmony harmony, ref bool patchesFound)
    {
        try
        {
            harmony.CreateClassProcessor(type).Patch();
            patchesFound = true;
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Nie udało się załadować patcha {type.Name}: {e}");
            return false;
        }
    }
}