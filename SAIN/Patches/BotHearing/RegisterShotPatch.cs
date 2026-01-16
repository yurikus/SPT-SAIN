using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SAIN.Components;
using SPT.Reflection.Patching;

namespace SAIN.Patches.BotHearing;

public class RegisterShotPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player.FirearmController), nameof(Player.FirearmController.RegisterShot));
    }

    [PatchPrefix]
    public static void PatchPrefix(Player ____player, Item weapon, EftBulletClass shot)
    {
        GameWorldComponent.Instance?.RegisterShot(____player, shot, weapon);
    }
}
