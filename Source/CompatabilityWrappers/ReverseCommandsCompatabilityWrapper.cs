using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using Verse;

namespace Merthsoft.ExpandedContextMenu.CompatabilityWrappers {
    static class ReverseCommandsCompatabilityWrapper {
        private const string PackageId = "brrainz.reversecommands";
        private const string ToolsTypeName = "ReverseCommands.Tools";
        private const string LabelMenuFieldName = "labelMenu";
        private const string FloatMenuLabelsTypeName = "ReverseCommands.FloatMenuLabels";
        private const string CloseLabelMenuMethodName = "CloseLabelMenu";

        private static bool CompatabilityEnabled { get; set; } = false;
        
        private static Type ToolsType { get; set; }
        private static FieldInfo LabelMenuField { get; set; }
        private static Type FloatMenuLabelsType { get; set; }
        private static Traverse CloseMenuLabelMethod { get; set; }

        public static void AttemptEnableCompatability() {
            if (!LoadedModManager.RunningModsListForReading.Exists(m => m.PackageId.Equals(PackageId, StringComparison.InvariantCultureIgnoreCase))) {
                Log.Message($"Reverse Commands compatability not enabled: No mod with package id {PackageId} was found.");
                return;
            }

            if ((ToolsType = AccessTools.TypeByName(ToolsTypeName)) == null) {
                Log.Error($"Reverse Commands compatability not enabled: Failed to get type {ToolsTypeName}.");
                return;
            }

            if ((LabelMenuField = AccessTools.Field(ToolsType, LabelMenuFieldName)) == null) {
                Log.Error($"Reverse Commands compatability not enabled: Failed to get field named {LabelMenuFieldName} on type {ToolsTypeName}.");
                return;
            }

            if ((FloatMenuLabelsType = AccessTools.TypeByName(FloatMenuLabelsTypeName)) == null) {
                Log.Error($"Reverse Commands compatability not enabled: Failed to get field named {FloatMenuLabelsTypeName} on type {ToolsTypeName}.");
                return;
            }

            if ((CloseMenuLabelMethod = Traverse.Create(ToolsType).Method(CloseLabelMenuMethodName, new[] { typeof(bool) })) == null) {
                Log.Error($"Reverse Commands compatability not enabled: Failed to get method {CloseLabelMenuMethodName}(bool) on type {ToolsTypeName}.");
                return;
            }

            CompatabilityEnabled = true;
            Log.Message("Reverse Commands compatability enabled.");
        }


        public static bool Patch(Selector selector) {
            if (!CompatabilityEnabled) { return false; }

            var floatMenu = LabelMenuField.GetValue(null) as FloatMenu;
            if (floatMenu == null) { return false; }

            var (options, _) = ExpandedContextMenu.AddToFloatMenu(floatMenu, selector, false);
            if (options == null) { 
                CompatabilityEnabled = false;
                Log.Error("Reverse Commands compatability failed: AddToFloatMenu returned null.");
                return false;
            }

            try {
                CloseMenuLabelMethod.GetValue(false);
                Find.WindowStack.TryRemove(FloatMenuLabelsType);
                var newFloatMenu = Activator.CreateInstance(FloatMenuLabelsType, options);
                LabelMenuField.SetValue(null, newFloatMenu);
                Find.WindowStack.Add(LabelMenuField.GetValue(null) as Window);
                //Log.Message("Removed");
            } catch (Exception ex) {
                CompatabilityEnabled = false;
                Log.Error($"Reverse Commands compatability failed: {ex}");
                return false;
            }
            return true;
        }
    }
}
