using HarmonyLib;
using Merthsoft.ExpandedContextMenu.CompatibilityWrappers;
using RimWorld;
using UnityEngine;
using Verse;

namespace Merthsoft.ExpandedContextMenu; 
[HarmonyPatch(typeof(Selector), "HandleMapClicks")]
static class Selector_HandleMapClicks_Patch {
    [HarmonyBefore("net.pardeike.reversecommands")]
    public static bool Prefix(Selector __instance) {
        if (Event.current.type != EventType.MouseDown || Event.current.button != 1) { return true; }
        if (Find.CurrentMap == null) { return true; }

        var floatMenu = ExpandedContextMenu.AttemptPatch(__instance);
        if (floatMenu == null) {
            return true;
        }

        Find.WindowStack.Add(floatMenu);
        Event.current.Use();
        return false;
    }
}
