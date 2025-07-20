using EFT;
using SAIN.Components;
using SAIN.Models.Enums;
using UnityEngine;

namespace SAIN.Helpers
{
    public class Shoot
    {
        public static float FullAutoBurstLength(BotComponent bot, float distance)
        {
            if (bot.IsCheater)
            {
                return 1f;
            }

            if (bot.ManualShoot.Reason != EShootReason.None && bot.Info.WeaponInfo.EWeaponClass == EWeaponClass.machinegun)
            {
                return 0.75f;
            }

            float scaledBurstLength = 1f - (Mathf.Clamp(distance, 0f, 30f) / 30f);
            scaledBurstLength /= bot.Info.WeaponInfo.FinalModifier;
            scaledBurstLength *= bot.Info.FileSettings.Shoot.BurstMulti;
            scaledBurstLength = Mathf.Clamp(scaledBurstLength, 0.001f, 1f);

            if (distance > 30f)
            {
                scaledBurstLength = 0.001f;
            }
            else if (distance < 5f)
            {
                scaledBurstLength = 1f;
            }

            return scaledBurstLength;
        }

        public static float FullAutoTimePerShot(int bFirerate)
        {
            float roundspersecond = bFirerate / 60;

            float secondsPerShot = 1f / roundspersecond;

            return secondsPerShot;
        }

        public static float InverseScaleWithLogisticFunction(float originalValue, float k, float x0 = 20f)
        {
            float scaledValue = 1f - 1f / (1f + Mathf.Exp(k * (originalValue - x0)));
            return (float)System.Math.Round(scaledValue, 3);
        }
    }
}