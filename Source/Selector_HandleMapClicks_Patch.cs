using HarmonyLib;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace Merthsoft.ExpandedContextMenu {
    [HarmonyPatch(typeof(Selector), "HandleMapClicks")]
    static class Selector_HandleMapClicks_Patch {
        public static bool Prefix(Selector __instance) {
            if (Event.current.type != EventType.MouseDown || Event.current.button != 1) { return true; }
            if (Find.CurrentMap == null) { return true; }

            var things = __instance.SelectedObjectsListForReading.Where(s => s is Thing).Select(s => s as Thing).ToArray();

            if (things.Length == 0) { return true; }

            var selected = __instance.FirstSelectedObject as Thing;

            if (selected == null) { return true; }

            if (things.Any(s => (s as Thing)?.def != selected.def)) { return true; }

            switch (selected) {
                case null:
                    return true;
                case Pawn pawn when pawn.IsColonistPlayerControlled
                                    && Find.CurrentMap.thingGrid.ThingsListAtFast(UI.MouseCell())?.Contains(pawn) == false:
                    return true;
                case Thing thing:
                    if (!Find.CurrentMap.thingGrid.CellContains(UI.MouseCell(), selected.def)) { return true; }

                    var menuItems = ExpandedContextMenu.GetMenuItems(things);
                    if (menuItems?.Count == 0) { return true; }
                        
                    FloatMenu floatMenu = new FloatMenu(menuItems, thing.LabelCap);
                    Find.WindowStack.Add(floatMenu);
                    Event.current.Use();
                    return false;
            }
        }
    }
}
