using System.Reflection;
using EFT;
using HarmonyLib;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SPT.Reflection.Patching;

namespace SAIN.Patches.GameWorld;

/// <summary>
/// This patch makes sure that whenever a late player update happens, it is also sent to SAIN's <see cref="PlayerComponent">PlayerComponent</see>
/// </summary>
public class PlayerLateUpdatePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player), nameof(Player.LateUpdate));
    }

    [PatchPostfix]
    public static void Patch(Player __instance)
    {
        if (GameWorldComponent.TryGetPlayerComponent(__instance, out PlayerComponent playerComponent))
        {
            playerComponent.ManualLateUpdate();
        }
    }
}
