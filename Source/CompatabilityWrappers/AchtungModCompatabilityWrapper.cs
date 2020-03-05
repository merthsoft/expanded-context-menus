using HarmonyLib;
using System;
using System.Reflection;
using Verse;

namespace Merthsoft.ExpandedContextMenu.CompatabilityWrappers {
    static class AchtungModCompatabilityWrapper {
        private const string PackageId = "brrainz.achtung";
        private const string GetWindowMethod = "AchtungMod.MultiActions:GetWindow";
        private static bool CompatabilityEnabled { get; set; } = false;

        public static void AttemptEnableCompatability(Harmony harmony) {
            if (!LoadedModManager.RunningModsListForReading.Exists(m => m.PackageId.Equals(PackageId, StringComparison.InvariantCultureIgnoreCase))) {
                Log.Message($"Achtung compatability not enabled: No mod with package id {PackageId} was found.");
                return;
            }

            var method = AccessTools.Method(GetWindowMethod) as MethodBase;
            if (method == null) {
                Log.Message($"Achtung compatability not enabled: Could not load method {GetWindowMethod}.");
                return;
            }
            try {
                harmony.Patch(method, postfix: new HarmonyMethod(AccessTools.Method(typeof(AchtungModCompatabilityWrapper), "GetWindowPostfix")));
                CompatabilityEnabled = true;
                Log.Message("Achtung compatability enabled.");
            } catch (Exception ex) {
                Log.Error($"Achtung compatability not enabled: {ex}");
            }
        }

        public static void GetWindowPostfix(ref Window __result) {
            if (CompatabilityEnabled && __result is FloatMenu floatMenu) {
                floatMenu = ExpandedContextMenu.AddToFloatMenu(floatMenu, Find.MapUI.selector, true);
                if (floatMenu == null) {
                    CompatabilityEnabled = false;
                    Log.Error("Achtung compatability failed: AddToFloatMenu returned null.");
                }
                __result = floatMenu;
            }
        }
    }
}
