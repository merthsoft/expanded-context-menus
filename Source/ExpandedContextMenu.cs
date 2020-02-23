using HarmonyLib;
using Merthsoft.ExpandedContextMenu.CompatabilityWrappers;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace Merthsoft.ExpandedContextMenu {
    [StaticConstructorOnStartup]
    public class ExpandedContextMenu {        
        public static Harmony Harmony;

        static ExpandedContextMenu() {
            Harmony = new Harmony("Merthsoft.ExpandedContextMenus");
            Harmony.PatchAll(Assembly.GetExecutingAssembly());

            AllowToolCompatabilityWrapper.AttemptEnableCompatability();
            AchtungModCompatabilityWrapper.AttemptEnableCompatability(Harmony);
        }

        public static (List<FloatMenuOption> menuItems, string labelCap) AttemptPatch(Selector selector, bool skipDefaultChoices = false) {
            var things = selector.SelectedObjectsListForReading.Where(s => s is Thing).Select(s => s as Thing);
            if (things.Count() == 0) {
                return (null, null);
            }

            if (!(selector.FirstSelectedObject is Thing selected)) {
                return (null, null);
            }

            if (things.Any(s => (s as Thing)?.def != selected.def)) {
                return (null, null);
            }

            switch (selected) {
                case Pawn pawn when pawn.IsColonistPlayerControlled
                                    && Find.CurrentMap.thingGrid.ThingsListAtFast(UI.MouseCell())?.Exists(p => p.ThingID == pawn.ThingID) == false:
                    return (null, null);
                case Thing thing:
                    if (!Find.CurrentMap.thingGrid.CellContains(UI.MouseCell(), selected.def)) {
                        return (null, null);
                    }

                    return (GetMenuItems(things, skipDefaultChoices), thing.LabelCap);
                default:
                    return (null, null);
            }
        }

        private static List<FloatMenuOption> GetMenuItems(IEnumerable<Thing> things, bool skipDefaultChoices) {
            List<FloatMenuOption> ret = new List<FloatMenuOption>();
            if (things == null) { return ret; }

            var thing = things.FirstOrDefault();
            if (thing == null) { return ret; }

            if (thing is Pawn pawn && !skipDefaultChoices) {
                ret = FloatMenuMakerMap.ChoicesAtFor(UI.MouseMapPosition(), pawn);
            }

            foreach (var gizmo in thing.GetGizmos()) {
                var command = gizmo as Command;
                if (command == null) { continue; }
                try {
                    ret.Add(new FloatMenuOption(command.LabelCap ?? command.Desc?.Split('\n')[0].CapitalizeFirst().Trim() ?? command.TutorTagSelect, () => {
                        if (TutorSystem.AllowAction(command.TutorTagSelect)) {
                            command.ProcessInput(null);
                        }                        
                    }));
                } catch (Exception ex) {
                    Log.Error($"Unable to generate gizmo menu for: {gizmo} - {ex.Message}");
                }
            }

            foreach (var designator in Find.ReverseDesignatorDatabase.AllDesignators) {
                if (designator.CanDesignateThing(thing).Accepted) {
                    ret.Add(new FloatMenuOption(designator.LabelCapReverseDesignating(thing), () => {
                        if (TutorSystem.AllowAction(designator.TutorTagDesignate)) {
                            things.Do(t => designator.DesignateThing(t));
                            designator.Finalize(true);
                        }
                    }));
                    ret.AddRange(AllowToolCompatabilityWrapper.GetMenuItems(designator));
                }
            }
            
            return ret;
        }
    }
}
