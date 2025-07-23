using HarmonyLib;
using Merthsoft.ExpandedContextMenu.CompatibilityWrappers;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace Merthsoft.ExpandedContextMenu; 
[StaticConstructorOnStartup]
public class ExpandedContextMenu {
    private const string FloatMenuOptionsFieldName = "options";

    public static Harmony Harmony;

    static ExpandedContextMenu() {
        Harmony = new Harmony("Merthsoft.ExpandedContextMenus");
        Harmony.PatchAll(Assembly.GetExecutingAssembly());

        AllowToolCompatibilityWrapper.AttemptEnableCompatibility();
        AchtungModCompatibilityWrapper.AttemptEnableCompatibility(Harmony);
        ReverseCommandsCompatibilityWrapper.AttemptEnableCompatibility();
    }

    public static FloatMenu AddToFloatMenu(FloatMenu floatMenu, Selector selector, bool skipDefaultChoices) {
        try {
            var options = Traverse.Create(floatMenu).Field<List<FloatMenuOption>>(FloatMenuOptionsFieldName).Value;
            if (options == null) {
                Log.Error($"Unable to load field {FloatMenuOptionsFieldName} of type List<FloatMenuOption> on {floatMenu.GetType().Name}");
                return null;
            }
            var (menuItems, labelCap) = AttemptPatchInternal(selector, skipDefaultChoices);
            if (menuItems == null || menuItems.Count == 0) { 
                return ReverseCommandsCompatibilityWrapper.GetFloatMenu(options, null);
            }
            options.AddRange(menuItems);
            return ReverseCommandsCompatibilityWrapper.GetFloatMenu(options, labelCap);
        } catch (Exception ex) {
            Log.Error($"Error in AddToFloatMenu: {ex}"); 
            return null;
        }
    }

    public static FloatMenu AttemptPatch(Selector selector)
        => ReverseCommandsCompatibilityWrapper.GetFloatMenu(AttemptPatchInternal(selector, false));

    private static (List<FloatMenuOption>, string) AttemptPatchInternal(Selector selector, bool skipDefaultChoices) {
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
        List<FloatMenuOption> ret = [];
        if (things == null) { return ret; }

        var thing = things.FirstOrDefault();
        if (thing == null) { return ret; }

        if (thing is Pawn pawn && !skipDefaultChoices) {
            ret = FloatMenuMakerMap.GetOptions([pawn], UI.MouseMapPosition(), out var _);
        }
        
        foreach (var gizmo in thing.GetGizmos()) {
            if (gizmo is Command command) {
                try
                {
                    ret.Add(CommandToFloatMenuOption(command));
                }
                catch (Exception ex) {
                    Log.Error($"Unable to generate gizmo menu for: {gizmo} - {ex.Message}");
                }
            }
        }

        foreach (var designator in Find.ReverseDesignatorDatabase.AllDesignators) {
            if (designator.CanDesignateThing(thing).Accepted)
            {
                ret.Add(DesignatorToFloatMenuOption(things, thing, designator));
                ret.AddRange(AllowToolCompatibilityWrapper.GetMenuItems(designator));
            }
        }

        ret.AddRange(ReverseCommandsCompatibilityWrapper.GetMenuItems());

        if (ret.Count == 0)
            return null;

        return ret;
    }

    private static FloatMenuOption DesignatorToFloatMenuOption(IEnumerable<Thing> things, Thing thing, Designator designator)
        => new(
            label: designator.LabelCapReverseDesignating(thing),
            action: () =>
            {
                if (TutorSystem.AllowAction(designator.TutorTagDesignate))
                {
                    things.Do(t => designator.DesignateThing(t));
                    designator.Finalize(true);
                }
            })
        {
            Disabled = designator.Disabled
        };

    private static FloatMenuOption CommandToFloatMenuOption(Command command) 
        => new(
            label: command.LabelCap ?? command.Desc?.Split('\n')[0].CapitalizeFirst().Trim() ?? command.TutorTagSelect, 
            action: () =>
            {
                if (TutorSystem.AllowAction(command.TutorTagSelect))
                {
                    command.ProcessInput(null);
                }
            })
        { 
            Disabled = command.Disabled,
        };
}
