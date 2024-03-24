using HarmonyLib;
using System;
using System.Reflection;
using Verse;

namespace Merthsoft.ExpandedContextMenu.CompatibilityWrappers; 
static class AchtungModCompatibilityWrapper {
    private const string PackageId = "brrainz.achtung";
    private const string GetWindowMethod = "AchtungMod.MultiActions:GetWindow";
    private static bool CompatibilityEnabled { get; set; } = false;

    public static void AttemptEnableCompatibility(Harmony harmony) {
        if (!LoadedModManager.RunningModsListForReading.Exists(m => m.PackageId.Equals(PackageId, StringComparison.InvariantCultureIgnoreCase))) {
            Log.Message($"Achtung Compatibility not enabled: No mod with package id {PackageId} was found.");
            return;
        }

        var method = AccessTools.Method(GetWindowMethod) as MethodBase;
        if (method == null) {
            Log.Message($"Achtung Compatibility not enabled: Could not load method {GetWindowMethod}.");
            return;
        }
        try {
            harmony.Patch(method, postfix: new HarmonyMethod(AccessTools.Method(typeof(AchtungModCompatibilityWrapper), "GetWindowPostfix")));
            CompatibilityEnabled = true;
            Log.Message("Achtung Compatibility enabled.");
        } catch (Exception ex) {
            Log.Error($"Achtung Compatibility not enabled: {ex}");
        }
    }

    public static void GetWindowPostfix(ref Window __result) {
        if (CompatibilityEnabled && __result is FloatMenu floatMenu) {
            floatMenu = ExpandedContextMenu.AddToFloatMenu(floatMenu, Find.MapUI.selector, true);
            if (floatMenu == null) {
                CompatibilityEnabled = false;
                Log.Error("Achtung Compatibility failed: AddToFloatMenu returned null.");
            }
            __result = floatMenu;
        }
    }
}
