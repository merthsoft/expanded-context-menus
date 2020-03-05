﻿using HarmonyLib;
using Merthsoft.ExpandedContextMenu.CompatabilityWrappers;
using RimWorld;
using UnityEngine;
using Verse;

namespace Merthsoft.ExpandedContextMenu {
    [HarmonyPatch(typeof(Selector), "HandleMapClicks")]
    static class Selector_HandleMapClicks_Patch {
        [HarmonyAfter(new[] { "net.pardeike.reversecommands" })]
        public static bool Prefix(Selector __instance) {
            if (Event.current.type == EventType.Used && ReverseCommandsCompatabilityWrapper.Patch(__instance)) {
                return false;
            }

            if (Event.current.type != EventType.MouseDown || Event.current.button != 1) { return true; }
            if (Find.CurrentMap == null) { return true; }

            var (menuItems, labelCap) = ExpandedContextMenu.AttemptPatch(__instance);
            if (menuItems == null || menuItems.Count == 0) {
                return true;
            }

            FloatMenu floatMenu = new FloatMenu(menuItems, labelCap);
            Find.WindowStack.Add(floatMenu);
            Event.current.Use();
            return false;
        }
    }
}
