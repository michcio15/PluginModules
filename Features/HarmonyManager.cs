using HarmonyLib;
using System.Reflection;

namespace PluginModules.Features;

public sealed class HarmonyManager
{
    internal Harmony? Harmony = null;

    public HarmonyManager(Module module)
    {
        Module = module;
    }

    public Module Module { get; }
    public ModuleLog ModuleLog => Module.ModuleLog;
    public string Namespace => Module.Namespace!;
    public string HarmonyName => $"com.module.{Module.Name}";

    public void PatchAll()
    {
        bool patchesFound = false;

        IEnumerable<Type> patchTypes = Module.CachedTypes.Where(t =>
            t.GetCustomAttribute<HarmonyPatch>() != null);

        Harmony ??= new Harmony(HarmonyName);

        foreach (Type type in patchTypes)
        {
            TryPatch(type, Harmony, ref patchesFound);
        }


        ModuleLog.Debug(patchesFound
            ? $"Załadowano patche z namespace: {Namespace}."
            : $"Nie znaleziono patchy w namespace: {Namespace}.");
    }

    private bool TryPatch(Type type, Harmony harmony, ref bool patchesFound)
    {
        try
        {
            harmony.CreateClassProcessor(type).Patch();
            patchesFound = true;
            return true;
        }
        catch (Exception e)
        {
            ModuleLog.Error($"Nie udało się załadować patcha {type.Name}: {e}");
            return false;
        }
    }

    public void UnpatchAll()
    {
        Harmony?.UnpatchAll(HarmonyName);
        Harmony = null;
    }
}