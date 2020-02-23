using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using Verse;

namespace Merthsoft.ExpandedContextMenu.CompatabilityWrappers {
    static class AllowToolCompatabilityWrapper {
        private static readonly List<FloatMenuOption> EmptyMenu = new List<FloatMenuOption>();

        private static bool CompatabilityEnabled { get; set; } = false;
        private static Type AllowToolMenuController { get; set; }

        public static void AttemptEnableCompatability() {
            AllowToolMenuController = AccessTools.TypeByName("AllowTool.Context.DesignatorContextMenuController");
            if (AllowToolMenuController == null) {
                Log.Message("Failed to load AllowTool, compatability not enabled.");
                return;
            }
            CompatabilityEnabled = true;
            Log.Message("AllowTool compatability enabled.");
        }

        public static List<FloatMenuOption> GetMenuItems(Designator designator) {
            if (!CompatabilityEnabled) {
                return EmptyMenu;
            }

            try {
                var providers = Traverse.Create(AllowToolMenuController).Property("MenuProviderInstances")?.GetValue() as IEnumerable;
                if (providers == null) {
                    CompatabilityEnabled = false;
                    Log.Error("AllowTool compatability failed: Could not get property `MenuProviderInstances` on controllerType");
                    return EmptyMenu;
                }

                var ret = new List<FloatMenuOption>();
                foreach (var provider in providers) {
                    var traverse = Traverse.Create(provider);
                    var designatorType = traverse.Property("HandledDesignatorType")?.GetValue() as Type;
                    if (designatorType == null) {
                        CompatabilityEnabled = false;
                        Log.Error("AllowTool compatability failed: Could not get property `HandledDesignatorType` on provider");
                        continue;
                    }

                    if (designatorType.IsInstanceOfType(designator)) {
                        var listMenuEntriesMethod = traverse.Method("ListMenuEntries");

                        if (listMenuEntriesMethod == null) {
                            listMenuEntriesMethod = Traverse.Create(traverse.GetType().BaseType).Method("ListMenuEntries");
                        }

                        if (listMenuEntriesMethod != null) {
                            var extraOptions = listMenuEntriesMethod.GetValue(provider, new[] { designator }) as IEnumerable<FloatMenuOption>;

                            if (extraOptions == null) {
                                Log.Error($"Could not get value of method `ListMenuEntries` with designator {designator.Label}");
                                continue;
                            }

                            foreach (var option in extraOptions) {
                                option.Label = $"- {option.Label}";
                                ret.Add(option);
                            }
                        }
                    }
                }
                return ret;
            } catch (Exception ex) {
                CompatabilityEnabled = false;
                Log.Error($"AllowTool compatability failed: {ex}");
                return EmptyMenu;
            }
        }
    }
}
