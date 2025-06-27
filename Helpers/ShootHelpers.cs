using EFT;
using SAIN.Models.Enums;
using SAIN.SAINComponent;
using UnityEngine;

namespace SAIN.Helpers
{
    public class Shoot
    {
        public static float FullAutoBurstLength(BotOwner BotOwner, float distance)
        {
            var component = BotOwner.GetComponent<BotComponent>();

            if (component == null)
            {
                return 0.1f;
            }

            if (component.IsCheater)
            {
                return 1f;
            }

            if (component.ManualShoot.Reason != EShootReason.None && component.Info.WeaponInfo.EWeaponClass == EWeaponClass.machinegun)
            {
                return 0.75f;
            }

            //float k = 0.08f; // How fast for the burst length to falloff with Distance
            //float scaledDistance = InverseScaleWithLogisticFunction(distance, k, 20f);

            float scaledBurstLength = 1f - (Mathf.Clamp(distance, 0f, 30f) / 30f);
            scaledBurstLength /= component.Info.WeaponInfo.FinalModifier;
            scaledBurstLength *= component.Info.FileSettings.Shoot.BurstMulti;
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