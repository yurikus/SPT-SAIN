using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using SAIN.Components;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.Classes.Mover;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SAIN.Patches.Event;

public class EnableUsecPmcKhorovodPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(GClass350), nameof(GClass350.EventsPriority));
    }

    [PatchPrefix]
    public static bool Patch(ref GClass671 __result)
    {
        if (!SAINPlugin.LoadedPreset.GlobalSettings.General.Jokes.EnableKhorovodPMCs)
        {
            return true;
        }

        __result = new GClass671(77, 74, 55, 75, -1, 76);

        return false;
    }
}

public class EnableBearPmcKhorovodPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(GClass348), nameof(GClass348.EventsPriority));
    }

    [PatchPrefix]
    public static bool Patch(ref GClass671 __result)
    {
        if (!SAINPlugin.LoadedPreset.GlobalSettings.General.Jokes.EnableKhorovodPMCs)
        {
            return true;
        }

        __result = new GClass671(77, 74, 55, 75, -1, 76);

        return false;
    }
}
