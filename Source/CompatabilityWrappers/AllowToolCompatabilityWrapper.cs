﻿using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Merthsoft.ExpandedContextMenu.CompatibilityWrappers; 
static class AllowToolCompatibilityWrapper {
    private const string PackageId = "UnlimitedHugs.AllowTool";
    private const string DesignatorContextMenuControllerTypeName = "AllowTool.Context.DesignatorContextMenuController";
    private const string MenuProvidersFieldName = "menuProviders";
    private const string HandleDesignatorTypePropertyName = "HandledDesignatorType";
    private const string EntriesFieldName = "entries";
    private const string EnabledPropertyName = "Enabled";
    private const string MakeMenuOptionMethod = "MakeMenuOption";

    private static readonly List<FloatMenuOption> EmptyMenu = [];

    private static bool CompatibilityEnabled { get; set; } = false;
    private static Type AllowToolMenuController { get; set; }

    public static void AttemptEnableCompatibility() {
        if (!LoadedModManager.RunningModsListForReading.Exists(m => m.PackageId.Equals(PackageId, StringComparison.InvariantCultureIgnoreCase))) {
            Log.Message($"Achtung Compatibility not enabled: No mod with package id {PackageId} was found.");
            return;
        }

        AllowToolMenuController = AccessTools.TypeByName(DesignatorContextMenuControllerTypeName);
        if (AllowToolMenuController == null) {
            Log.Message("Failed to load AllowTool, Compatibility not enabled.");
            return;
        }
        CompatibilityEnabled = true;
        Log.Message("AllowTool Compatibility enabled.");
    }

    public static List<FloatMenuOption> GetMenuItems(Designator designator) {
        if (!CompatibilityEnabled) {
            return EmptyMenu;
        }

        var step = "getting providers";
        try {
            var providers = Traverse.Create(AllowToolMenuController).Field(MenuProvidersFieldName)?.GetValue() as IEnumerable;
            if (providers == null) {
                CompatibilityEnabled = false;
                Log.Error($"AllowTool Compatibility failed: Could not get field {MenuProvidersFieldName} on {DesignatorContextMenuControllerTypeName}.");
                return EmptyMenu;
            }

            var ret = new List<FloatMenuOption>();
            foreach (var provider in providers) {
                step = "getting designator type";
                var traverse = Traverse.Create(provider);
                var designatorType = traverse.Property(HandleDesignatorTypePropertyName)?.GetValue() as Type;
                if (designatorType == null) {
                    CompatibilityEnabled = false;
                    Log.Error($"AllowTool Compatibility failed: Could not get property {HandleDesignatorTypePropertyName} on provider.");
                    continue;
                }

                if (designatorType.IsInstanceOfType(designator)) {
                    step = "getting entries field";
                    var listMenuEntriesField = traverse.Field(EntriesFieldName);

                    if (listMenuEntriesField != null) {
                        step = "getting extra options";
                        var extraOptions = listMenuEntriesField.GetValue() as IEnumerable<object>;

                        if (extraOptions == null) {
                            Log.Error($"Could not get value of method {EntriesFieldName} with designator {designator.Label}");
                            continue;
                        }

                        step = "transforming menu options";
                        var menuOptions = extraOptions.Select(o => Traverse.Create(o))
                            .Where(t => t.Property<bool>(EnabledPropertyName).Value)
                            .Select(t => t.Method(MakeMenuOptionMethod, designator).GetValue<FloatMenuOption>())
                            .Concat(designator.RightClickFloatMenuOptions);

                        step = "adding options";
                        foreach (var option in menuOptions) {
                            option.Label = $"- {option.Label}";
                            ret.Add(option);
                        }
                    }
                }
            }
            return ret;
        } catch (Exception ex) {
            CompatibilityEnabled = false;
            Log.Error($"AllowTool Compatibility failed in step {step}: {ex}");
            return EmptyMenu;
        }
    }
}
