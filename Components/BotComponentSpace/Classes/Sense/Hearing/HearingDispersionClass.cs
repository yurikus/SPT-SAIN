using SAIN.Components.PlayerComponentSpace;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class HearingDispersionClass(SAINHearingSensorClass hearing) : BotSubClass<SAINHearingSensorClass>(hearing), IBotClass
    {
        public Vector3 CalcRandomizedPosition(AISoundData Sound, float addDispersion)
        {
            float baseDispersion = getBaseDispersion(Sound.PlayerDistance, Sound.SoundType);
            float dispersionMod = getDispersionModifier(Sound.Enemy) * addDispersion;
            float finalDispersion = baseDispersion * dispersionMod;

            HearingSettings hearingSettings = GlobalSettingsClass.Instance.Hearing;
            finalDispersion = Mathf.Clamp(finalDispersion, 0f, hearingSettings.HEAR_DISPERSION_MAX_DISPERSION);
            float min = Sound.PlayerDistance < hearingSettings.HEAR_DISPERSION_MIN_DISTANCE_THRESH ? 0f : hearingSettings.HEAR_DISPERSION_MIN;
            Vector3 randomdirection = getRandomizedDirection(finalDispersion, min);

            if (SAINPlugin.DebugSettings.Logs.DebugHearing)
                Logger.LogDebug($"Dispersion: [{randomdirection.magnitude}] Distance: [{ Sound.PlayerDistance}] Base Dispersion: [{baseDispersion}] DispersionModifier [{dispersionMod}] Final Dispersion: [{finalDispersion}] : SoundType: [{Sound.SoundType}]");

            Vector3 estimatedEnemyPos = Sound.Position + randomdirection;
            Vector3 dirToRandomPos = estimatedEnemyPos - Bot.Position;
            Vector3 result = Bot.Position + (dirToRandomPos.normalized * Sound.PlayerDistance);
            return result;
        }

        private float getBaseDispersion(float enemyDistance, SAINSoundType soundType)
        {
            HearingSettings hearingSettings = GlobalSettingsClass.Instance.Hearing;
            if (hearingSettings.HEAR_DISPERSION_VALUES.TryGetValue(soundType, out float dispersionValue) == false)
            {
                dispersionValue = 12.5f;
                //Logger.LogWarning($"Could not find [{soundType}] in Hearing Dispersion Dictionary!");
            }
            return enemyDistance / dispersionValue;
        }

        private float getDispersionModifier(Enemy Enemy)
        {
            float dotProduct = Vector3.Dot(Bot.LookDirection.normalized, Enemy.EnemyDirectionNormal);
            float scaled = (dotProduct + 1) / 2;

            HearingSettings hearingSettings = GlobalSettingsClass.Instance.Hearing;
            float dispersionModifier = Mathf.Lerp(hearingSettings.HEAR_DISPERSION_ANGLE_MULTI_MAX, hearingSettings.HEAR_DISPERSION_ANGLE_MULTI_MIN, scaled);

            //Logger.LogInfo($"Dispersion Modifier for Sound [{dispersionModifier}] Dot Product [{dotProduct}]");
            return dispersionModifier;
        }

        private Vector3 getRandomizedDirection(float dispersion, float min = 0.5f)
        {
            float randomX = UnityEngine.Random.Range(-dispersion, dispersion);
            float randomZ = UnityEngine.Random.Range(-dispersion, dispersion);
            Vector3 randomdirection = new(randomX, 0, randomZ);
            if (min > 0 && randomdirection.sqrMagnitude < min * min)
            {
                randomdirection = Vector3.Normalize(randomdirection) * min;
            }
            return randomdirection;
        }

        public Vector3 GetEstimatedPoint(Vector3 source, float distance)
        {
            Vector3 randomPoint = UnityEngine.Random.onUnitSphere;
            randomPoint.y = 0;
            randomPoint *= (distance / 10f);
            return source + randomPoint;
        }
    }
}