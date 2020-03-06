using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace Merthsoft.ExpandedContextMenu.CompatabilityWrappers {
    static class ReverseCommandsCompatabilityWrapper {
        private const string PackageId = "brrainz.reversecommands";

        private static class Types {
            private const string ToolsTypeName = "ReverseCommands.Tools";
            private const string FloatMenuLabelsTypeName = "ReverseCommands.FloatMenuLabels";
            private const string PathInfoTypeName = "PathInfo";

            private static Type toolsType;
            private static Type floatMenuLabelsType;
            private static Type pathInfoType;

            public static Type Tools => toolsType;
            public static Type FloatMenuLabels => floatMenuLabelsType;
            public static Type PathInfo => pathInfoType;

            private static bool CanLoad(string typeName, ref Type value) {
                value = AccessTools.TypeByName(typeName);
                if (value == null) {
                    Log.Error($"Reverse Commands compatability not enabled: Failed to get type {typeName}.");
                    return false;
                }
                return true;
            }

            public static bool Load()
                => CanLoad(ToolsTypeName, ref toolsType)
                && CanLoad(FloatMenuLabelsTypeName, ref floatMenuLabelsType)
                && CanLoad(PathInfoTypeName, ref pathInfoType);
        }

        private static class Methods {
            private const string CloseLabelMenuMethodName = "CloseLabelMenu";
            private const string GetPawnActionsMethodName = "GetPawnActions";
            private const string MakeMenuItemForLabelMethodName = "MakeMenuItemForLabel";
            private const string AddInfoMethodName = "AddInfo";
            private const string PawnUsableMethodName = "PawnUsable";

            private static Traverse makeMenuItemForLabelMethod;
            private static Traverse closeMenuLabelMethod;
            private static Traverse getPawnActionsMethod;
            private static Traverse addInfoMethod;
            private static Traverse pawnUsableMethod;

            public static Traverse MakeMenuItemForLabel => makeMenuItemForLabelMethod;
            public static Traverse CloseMenuLabel => closeMenuLabelMethod;
            public static Traverse GetPawnActions => getPawnActionsMethod;
            public static Traverse AddInfo => addInfoMethod;
            public static Traverse PawnUsable => pawnUsableMethod;

            private static bool CanLoad(Type type, string methodName, ref Traverse value, params Type[] paramTypes) {
                value = Traverse.Create(type).Method(methodName, paramTypes);
                if (value == null) {
                    Log.Error($"Reverse Commands compatability not enabled: Failed to get method {methodName}({string.Join(", ", paramTypes.Select(t => t.Name))}) on type {type.Name}.");
                    return false;
                }
                return true;
            }

            public static bool Load()
                => CanLoad(Types.Tools, CloseLabelMenuMethodName, ref closeMenuLabelMethod, typeof(bool))
                && CanLoad(Types.Tools, GetPawnActionsMethodName, ref getPawnActionsMethod)
                && CanLoad(Types.Tools, MakeMenuItemForLabelMethodName, ref makeMenuItemForLabelMethod, typeof(string), typeof(Dictionary<Pawn, FloatMenuOption>))
                && CanLoad(Types.PathInfo, AddInfoMethodName, ref addInfoMethod, typeof(Pawn), typeof(IntVec3))
                && CanLoad(Types.Tools, PawnUsableMethodName, ref pawnUsableMethod, typeof(Pawn));
        }

        private static class Fields {

            private const string LabelMenuFieldName = "labelMenu";

            public static FieldInfo LabelMenu { get; private set; }

            public static bool Load() {
                LabelMenu = AccessTools.Field(Types.Tools, LabelMenuFieldName);
                if (LabelMenu == null) {
                    Log.Error($"Reverse Commands compatability not enabled: Failed to get field named {LabelMenuFieldName} on type {Types.Tools.Name}.");
                    return false;
                }
                return true;
            }
        }

        private static bool CompatabilityEnabled { get; set; } = false;

        public static void AttemptEnableCompatability() {
            if (!LoadedModManager.RunningModsListForReading.Exists(m => m.PackageId.Equals(PackageId, StringComparison.InvariantCultureIgnoreCase))) {
                Log.Message($"Reverse Commands compatability not enabled: No mod with package id {PackageId} was found.");
                return;
            }

            if (Types.Load() && Methods.Load() && Fields.Load()) {

                CompatabilityEnabled = true;
                Log.Message("Reverse Commands compatability enabled.");
                return;
            }
        }

        public static FloatMenu GetFloatMenu((List<FloatMenuOption> options, string labelCap) item)
            => GetFloatMenu(item.options, item.labelCap);

        public static FloatMenu GetFloatMenu(List<FloatMenuOption> options, string labelCap) {
            if (options == null) { return null; }

            if (CompatabilityEnabled) {
                try {
                    var newFloatMenu = Activator.CreateInstance(Types.FloatMenuLabels, options);
                    Fields.LabelMenu.SetValue(null, newFloatMenu);
                    return newFloatMenu as FloatMenu ?? new FloatMenu(options, labelCap);
                } catch (Exception ex) {
                    Log.Error($"Reverse Commands compatability disabled: An exception occurred in GetFloatMenu: " + ex);
                    CompatabilityEnabled = false;
                    return new FloatMenu(options, labelCap);
                }
            } else {
                return new FloatMenu(options, labelCap);
            }
        }

        public static IEnumerable<FloatMenuOption> GetMenuItems() {
            if (!CompatabilityEnabled) { return Enumerable.Empty<FloatMenuOption>(); }

            try {
                var labeledPawnActions = Methods.GetPawnActions.GetValue<Dictionary<string, Dictionary<Pawn, FloatMenuOption>>>(null);
                if (!labeledPawnActions.Any()) { return Enumerable.Empty<FloatMenuOption>(); }

                var cell = UI.MouseCell();
                Find.CurrentMap.mapPawns.FreeColonists.Where(pawn => Methods.PawnUsable.GetValue<bool>(pawn)).Do(pawn => Methods.AddInfo.GetValue(pawn, cell));

                return labeledPawnActions.Keys.Select(label => {
                    var dict = labeledPawnActions[label];
                    return Methods.MakeMenuItemForLabel.GetValue<FloatMenuOption>(label, dict);
                });
            } catch (Exception ex) {
                Log.Error($"Reverse Commands compatability disabled: An exception occurred in GetMenuItems: " + ex);
                CompatabilityEnabled = false;
                return Enumerable.Empty<FloatMenuOption>();
            }
        }
    }
}
