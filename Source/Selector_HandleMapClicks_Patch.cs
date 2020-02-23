using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Merthsoft.ExpandedContextMenu {
    [HarmonyPatch(typeof(Selector), "HandleMapClicks")]
    static class Selector_HandleMapClicks_Patch {
        public static bool Prefix(Selector __instance) {
            if (Event.current.type != EventType.MouseDown || Event.current.button != 1) { return true; }
            if (Find.CurrentMap == null) { return true; }

            var (menuItems, labelCap) = ExpandedContextMenu.AttemptPatch(__instance);
            if (menuItems == null || menuItems.Count == 0) {
                Log.Message("- No menu items returned.");
                return false;
            }

            FloatMenu floatMenu = new FloatMenu(menuItems, labelCap);
            Find.WindowStack.Add(floatMenu);
            Event.current.Use();
            return false;
        }
    }
}
