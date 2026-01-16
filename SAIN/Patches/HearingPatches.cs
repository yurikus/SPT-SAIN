using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using CommonAssets.Scripts.Audio;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;
using SAIN.Components;
using SAIN.Components.Helpers;
using SAIN.Components.PlayerComponentSpace;
using SPT.Reflection.Patching;
using Systems.Effects;
using UnityEngine;

namespace SAIN.Patches.Hearing;

//Todo: Test all patches in this file and seperate them out undre .BotHearing
//Testing should be done on Headless fika to make sure that they are fika compatible.

public class GrenadeCollisionPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Grenade), nameof(Grenade.OnCollisionHandler));
    }

    [PatchPostfix]
    public static void Patch(Grenade __instance, SoundBank ___soundBank_0)
    {
        ___soundBank_0.Rolloff = _defaultRolloff * ROLLOFF_MULTI;
        //Logger.LogDebug($"Rolloff {_defaultRolloff} after {___soundBank_0.Rolloff}");
        BotManagerComponent.Instance?.GrenadeController.GrenadeCollided(__instance, 35);
    }

    private static float _defaultRolloff = 40;
    private const float ROLLOFF_MULTI = 1.5f;
}

public class GrenadeCollisionPatch2 : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Throwable), nameof(Throwable.OnCollisionHandler));
    }

    [PatchPostfix]
    public static void Patch(ref float ___IgnoreCollisionTrackingTimer, Throwable __instance)
    {
        ___IgnoreCollisionTrackingTimer = Time.time + 0.35f;
    }
}

public class HearingSensorPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotHearingSensor), nameof(BotHearingSensor.Init));
    }

    [PatchPrefix]
    public static bool PatchPrefix(BotHearingSensor __instance)
    {
        if (SAINEnableClass.IsSAINDisabledForBot(__instance.BotOwner))
        {
            return false;
        }
        return true;
    }
}

public class TryPlayShootSoundPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(PlayerAIDataClass), nameof(PlayerAIDataClass.TryPlayShootSound));
    }

    [PatchPrefix]
    public static bool PatchPrefix(PlayerAIDataClass __instance)
    {
        __instance.Boolean_0 = true;
        return false;
    }
}

public class OnWeaponModifiedPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player.FirearmController), nameof(Player.FirearmController.WeaponModified));
    }

    [PatchPrefix]
    public static void PatchPrefix(Player.FirearmController __instance, Player ____player)
    {
        if (GameWorldComponent.TryGetPlayerComponent(____player, out PlayerComponent PlayerComponent))
        {
            PlayerComponent.Equipment.WeaponModified(__instance.Weapon);
        }
    }
}

public class SoundClipNameCheckerPatch : ModulePatch
{
    private static MethodInfo _Player;
    private static FieldInfo _PlayerBridge;

    protected override MethodBase GetTargetMethod()
    {
        _PlayerBridge = AccessTools.Field(typeof(BaseSoundPlayer), "playersBridge");
        _Player = AccessTools.PropertyGetter(_PlayerBridge.FieldType, "iPlayer");
        return AccessTools.Method(typeof(BaseSoundPlayer), nameof(BaseSoundPlayer.SoundEventHandler));
    }

    [PatchPrefix]
    public static void PatchPrefix(string soundName, BaseSoundPlayer __instance)
    {
        if (BotManagerComponent.Instance != null)
        {
            object playerBridge = _PlayerBridge.GetValue(__instance);
            Player player = _Player.Invoke(playerBridge, null) as Player;
            SAINSoundTypeHandler.AISoundFileChecker(soundName, player);
        }
    }
}

public class SoundClipNameCheckerPatch2 : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BaseSoundPlayer), nameof(BaseSoundPlayer.SoundAtPointEventHandler));
    }

    [PatchPrefix]
    public static void PatchPrefix(string soundName, BaseSoundPlayer __instance)
    {
        if (soundName == FUSE)
        {
            BaseSoundPlayer.SoundElement soundElement = __instance.AdditionalSounds.Find(
                (BaseSoundPlayer.SoundElement elem) => elem.EventName == FUSE || elem.EventName == "Snd" + FUSE
            );
            if (soundElement != null)
            {
                soundElement.RollOff = 60;
                soundElement.Volume = 1;
            }
        }
    }

    private const string FUSE = "SndFuse";
}

//Todo: Doesn't work on fika
public class ToggleSoundPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player), nameof(Player.PlayToggleSound));
    }

    [PatchPostfix]
    public static void PatchPostfix(Player __instance, bool previousState, bool isOn, Vector3 ___SpeechLocalPosition)
    {
        if (previousState != isOn)
        {
            float baseRange = 10f;
            BotManagerComponent.Instance?.BotHearing.PlayAISound(
                __instance.ProfileId,
                SAINSoundType.GearSound,
                __instance.Position + ___SpeechLocalPosition,
                baseRange,
                1f
            );
        }
    }
}

public class SpawnInHandsSoundPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player), nameof(Player.SpawnInHands));
    }

    [PatchPostfix]
    public static void PatchPostfix(Player __instance, Item item)
    {
        //AudioClip itemClip = Singleton<GUISounds>.Instance.GetItemClip(item.ItemSound, EInventorySoundType.pickup);
        //if (itemClip != null) {
        //    SAINBotController.Instance?.BotHearing.PlayAISound(__instance.ProfileId, SAINSoundType.GearSound, __instance.Position, 30f, 1f);
        //}
    }
}

public class BulletImpactPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(EffectsCommutator), nameof(EffectsCommutator.PlayHitEffect));
    }

    [PatchPostfix]
    public static void PatchPostfix(EftBulletClass info)
    {
        if (BotManagerComponent.Instance != null)
        {
            BotManagerComponent.Instance.BotHearing.BulletImpacted(info);
        }
    }
}
