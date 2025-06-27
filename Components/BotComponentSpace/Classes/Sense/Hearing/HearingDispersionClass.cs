using SAIN.Preset.GlobalSettings;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class HearingDispersionClass : BotSubClass<SAINHearingSensorClass>, IBotClass
    {
        public HearingDispersionClass(SAINHearingSensorClass hearing) : base(hearing)
        {
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        public Vector3 CalcRandomizedPosition(BotSound sound, float addDispersion)
        {
            float distance = sound.Distance;
            float baseDispersion = getBaseDispersion(distance, sound.Info.SoundType);
            float dispersionMod = getDispersionModifier(sound) * addDispersion;
            float finalDispersion = baseDispersion * dispersionMod;

            HearingSettings hearingSettings = GlobalSettingsClass.Instance.Hearing;
            finalDispersion = Mathf.Clamp(finalDispersion, 0f, hearingSettings.HEAR_DISPERSION_MAX_DISPERSION);
            sound.Dispersion.Dispersion = finalDispersion;
            float min = distance < hearingSettings.HEAR_DISPERSION_MIN_DISTANCE_THRESH ? 0f : hearingSettings.HEAR_DISPERSION_MIN;
            Vector3 randomdirection = getRandomizedDirection(finalDispersion, min);

            if (SAINPlugin.DebugSettings.Logs.DebugHearing)
                Logger.LogDebug($"Dispersion: [{randomdirection.magnitude}] Distance: [{distance}] Base Dispersion: [{baseDispersion}] DispersionModifier [{dispersionMod}] Final Dispersion: [{finalDispersion}] : SoundType: [{sound.Info.SoundType}]");

            Vector3 estimatedEnemyPos = sound.Info.Position + randomdirection;
            Vector3 dirToRandomPos = estimatedEnemyPos - Bot.Position;
            Vector3 result = Bot.Position + (dirToRandomPos.normalized * distance);
            return result;
        }

        private float getBaseDispersion(float enemyDistance, SAINSoundType soundType)
        {
            HearingSettings hearingSettings = GlobalSettingsClass.Instance.Hearing;
            if (hearingSettings.HEAR_DISPERSION_VALUES.TryGetValue(soundType, out float dispersionValue) == false)
            {
                dispersionValue = 12.5f;
                Logger.LogWarning($"Could not find [{soundType}] in Hearing Dispersion Dictionary!");
            }
            return enemyDistance / dispersionValue;
        }

        private float getDispersionModifier(BotSound sound)
        {
            float dotProduct = Vector3.Dot(Bot.LookDirection.normalized, sound.Enemy.EnemyDirectionNormal);
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