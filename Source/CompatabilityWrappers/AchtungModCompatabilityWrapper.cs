using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Merthsoft.ExpandedContextMenu.CompatabilityWrappers {
    static class AchtungModCompatabilityWrapper {
        private static bool CompatabilityEnabled { get; set; } = false;

        public static void AttemptEnableCompatability(Harmony harmony) {
            var method = AccessTools.Method("AchtungMod.MultiActions:GetWindow") as MethodBase;
            if (method == null) {
                Log.Message("Could not load AchtungMod, compatability not enabled.");
                return;
            }
            try {
                harmony.Patch(method, postfix: new HarmonyMethod(AccessTools.Method(typeof(AchtungModCompatabilityWrapper), "GetWindowPostfix")));
                CompatabilityEnabled = true;
                Log.Message("AchtungMod compatability enabled.");
            } catch (Exception ex) {
                Log.Error($"AchtungMod compatability failed: {ex}");
            }
        }

        public static void GetWindowPostfix(ref Window __result) {
            if (CompatabilityEnabled && __result is FloatMenu floatMenu) {
                var options = Traverse.Create(floatMenu).Field<List<FloatMenuOption>>("options").Value;
                if (options == null) {
                    CompatabilityEnabled = false;
                    Log.Error("AchtungMod compatability failed: Failed to get private field `options` on floatMenu");
                    return;
                }
                var (menuItems, labelCap) = ExpandedContextMenu.AttemptPatch(Find.MapUI.selector, true);
                if (menuItems == null || menuItems.Count == 0) { return; }
                options.AddRange(menuItems);
                __result = new FloatMenu(options, labelCap);
            }
        }
    }
}
