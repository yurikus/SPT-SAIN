using EFT;
using SAIN.Helpers;
using SAIN.Preset;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Plugin
{
    internal static class SAINDifficultyClass
    {
        private const string PresetNameEasy = "Baby Bots";
        private const string PresetNameNormal = "Less Difficult";
        private const string PresetNameHard = "Default";
        private const string PresetNameHarderPMCs = "Default with Harder PMCs";
        private const string DefaultPresetDescription = "Bots are difficult but fair, the way SAIN was meant to played.";
        private const string PresetNameVeryHard = "I Like Pain";
        private const string PresetNameImpossible = "Death Wish";

        public static readonly Dictionary<SAINDifficulty, SAINPresetDefinition> DefaultPresetDefinitions = new();

        static SAINDifficultyClass()
        {
            DefaultPresetDefinitions.Add(
                SAINDifficulty.easy,
                SAINPresetDefinition.CreateDefaultDefinition(
                    PresetNameEasy,
                    SAINDifficulty.easy,
                    "Bots react slowly and are incredibly inaccurate."));

            DefaultPresetDefinitions.Add(
                SAINDifficulty.lesshard,
                SAINPresetDefinition.CreateDefaultDefinition(
                    PresetNameNormal,
                    SAINDifficulty.lesshard,
                    "Bots react more slowly, and are less accurate than usual."));

            DefaultPresetDefinitions.Add(
                SAINDifficulty.hard,
                SAINPresetDefinition.CreateDefaultDefinition(
                    PresetNameHard,
                    SAINDifficulty.hard,
                    DefaultPresetDescription));

            DefaultPresetDefinitions.Add(
                SAINDifficulty.harderpmcs,
                SAINPresetDefinition.CreateDefaultDefinition(
                    PresetNameHarderPMCs,
                    SAINDifficulty.harderpmcs,
                    "Default Settings, but PMCs are harder than normal."));

            DefaultPresetDefinitions.Add(
                SAINDifficulty.veryhard,
                SAINPresetDefinition.CreateDefaultDefinition(
                    PresetNameVeryHard,
                    SAINDifficulty.veryhard,
                    "Bots react faster, are more accurate, and can see further."));

            DefaultPresetDefinitions.Add(
                SAINDifficulty.deathwish,
                SAINPresetDefinition.CreateDefaultDefinition(
                    PresetNameImpossible,
                    SAINDifficulty.deathwish,
                    "Prepare To Die. Bots have almost no scatter, get less recoil from their weapon while shooting, are more accurate, and react deadly fast."));
        }

        public static SAINPresetClass GetDefaultPreset(SAINDifficulty difficulty)
        {
            SAINPresetClass result;
            switch (difficulty)
            {
                case SAINDifficulty.easy:
                    result = SAINDifficultyClass.CreateEasyPreset();
                    break;

                case SAINDifficulty.lesshard:
                    result = SAINDifficultyClass.CreateNormalPreset();
                    break;

                case SAINDifficulty.hard:
                    result = SAINDifficultyClass.CreateHardPreset();
                    break;

                case SAINDifficulty.harderpmcs:
                    result = SAINDifficultyClass.CreateHarderPMCsPreset();
                    break;

                case SAINDifficulty.veryhard:
                    result = SAINDifficultyClass.CreateVeryHardPreset();
                    break;

                case SAINDifficulty.deathwish:
                    result = SAINDifficultyClass.CreateImpossiblePreset();
                    break;

                default:
                    return null;
            }
            return result;
        }

        private static SAINPresetClass CreateEasyPreset()
        {
            var preset = new SAINPresetClass(SAINDifficulty.easy);

            var global = preset.GlobalSettings;
            global.Shoot.RecoilMultiplier = 3f;
            global.Difficulty.ScatteringCoef = 5f;
            global.Difficulty.PrecisionSpeedCoef = 2f;
            global.Difficulty.AccuracySpeedCoef = 2f;
            global.Difficulty.HearingDistanceCoef = 0.6f;
            global.Aiming.FasterCQBReactionsGlobal = false;
            global.Difficulty.VisibleDistCoef = 0.66f;
            global.Difficulty.GainSightCoef = 2.5f;

            foreach (var bot in preset.BotSettings.SAINSettings)
            {
                bot.Value.DifficultyModifier = Mathf.Clamp(bot.Value.DifficultyModifier * 0.5f, 0.01f, 2f).Round100();
                foreach (var setting in bot.Value.Settings)
                {
                    setting.Value.Core.VisibleAngle = 120f;
                    setting.Value.Shoot.FireratMulti *= 0.4f;
                    setting.Value.Shoot.BurstMulti *= 0.5f;
                    setting.Value.Look.MinimumVisionSpeed = 0.4f;
                    setting.Value.Aiming.DistanceAimTimeMultiplier = 1.5f;
                    setting.Value.Aiming.AngleAimTimeMultiplier = 1.5f;
                    if (setting.Value.Aiming.MAX_AIM_TIME < 1f)
                    {
                        setting.Value.Aiming.MAX_AIM_TIME = 1f;
                    }
                    if (setting.Value.Aiming.MAX_AIMING_UPGRADE_BY_TIME < 0.4f)
                    {
                        setting.Value.Aiming.MAX_AIMING_UPGRADE_BY_TIME = 0.4f;
                    }
                    setting.Value.Core.ScatteringPerMeter += 0.05f;
                    setting.Value.Core.ScatteringClosePerMeter += 0.1f;
                }
            }
            return preset;
        }

        private static SAINPresetClass CreateNormalPreset()
        {
            var preset = new SAINPresetClass(SAINDifficulty.lesshard);

            var global = preset.GlobalSettings;
            global.Shoot.RecoilMultiplier = 1.6f;
            global.Difficulty.ScatteringCoef = 1.25f;
            global.Difficulty.PrecisionSpeedCoef = 1.25f;
            global.Difficulty.AccuracySpeedCoef = 1.25f;
            global.Difficulty.VisibleDistCoef = 0.85f;
            global.Difficulty.GainSightCoef = 1.25f;
            global.Difficulty.HearingDistanceCoef = 0.85f;
            global.Aiming.FasterCQBReactionsGlobal = false;

            foreach (var bot in preset.BotSettings.SAINSettings)
            {
                bot.Value.DifficultyModifier = Mathf.Clamp(bot.Value.DifficultyModifier * 0.85f, 0.01f, 2f).Round100();
                foreach (var setting in bot.Value.Settings)
                {
                    setting.Value.Core.VisibleAngle = 150f;
                    setting.Value.Shoot.FireratMulti *= 0.8f;
                    setting.Value.Look.MinimumVisionSpeed = 0.1f;
                }
            }
            return preset;
        }

        private static SAINPresetClass CreateHardPreset()
        {
            var preset = new SAINPresetClass(SAINDifficulty.hard);
            return preset;
        }

        private static SAINPresetClass CreateHarderPMCsPreset()
        {
            var preset = new SAINPresetClass(SAINDifficulty.harderpmcs);
            ApplyHarderPMCs(preset);
            return preset;
        }

        private static void ApplyHarderPMCs(SAINPresetClass preset)
        {
            var botSettings = preset.BotSettings;
            foreach (var botsetting in botSettings.SAINSettings)
            {
                if (botsetting.Key == WildSpawnType.pmcUSEC || botsetting.Key == WildSpawnType.pmcBEAR)
                {
                    var pmcSettings = botsetting.Value.Settings;

                    // Set for all difficulties
                    foreach (var diff in pmcSettings.Values)
                    {
                        //diff.Core.ScatteringPerMeter = 0.03f;
                        //diff.Core.ScatteringClosePerMeter = 0.080f;
                        diff.Mind.WeaponProficiency = 0.75f;
                        diff.Difficulty.ScatteringCoef = 0.8f;
                        diff.Difficulty.PrecisionSpeedCoef = 0.8f;
                        diff.Difficulty.AccuracySpeedCoef = 0.8f;
                        diff.Difficulty.GainSightCoef = 0.8f;
                        diff.Difficulty.VisibleDistCoef = 1.25f;
                        diff.Difficulty.AggressionCoef = 1.2f;
                    }

                    var easy = pmcSettings[BotDifficulty.easy];
                    easy.Aiming.FasterCQBReactionsDistance = 20f;
                    easy.Aiming.FasterCQBReactionsMinimum = 0.3f;
                    easy.Aiming.MAX_AIMING_UPGRADE_BY_TIME = 0.35f;
                    easy.Aiming.MAX_AIM_TIME = 1.5f;
                    easy.Aiming.BASE_HIT_AFFECTION_DELAY_SEC = 0.65f;
                    easy.Core.VisibleDistance = 200f;

                    var normal = pmcSettings[BotDifficulty.normal];
                    normal.Aiming.FasterCQBReactionsDistance = 35f;
                    normal.Aiming.FasterCQBReactionsMinimum = 0.25f;
                    normal.Aiming.MAX_AIMING_UPGRADE_BY_TIME = 0.4f;
                    normal.Aiming.MAX_AIM_TIME = 1.35f;
                    normal.Aiming.BASE_HIT_AFFECTION_DELAY_SEC = 0.5f;
                    normal.Core.VisibleDistance = 225f;

                    var hard = pmcSettings[BotDifficulty.hard];
                    hard.Aiming.FasterCQBReactionsDistance = 50f;
                    hard.Aiming.FasterCQBReactionsMinimum = 0.2f;
                    hard.Aiming.MAX_AIMING_UPGRADE_BY_TIME = 0.2f;
                    hard.Aiming.MAX_AIM_TIME = 1.15f;
                    hard.Aiming.BASE_HIT_AFFECTION_DELAY_SEC = 0.35f;
                    hard.Core.VisibleDistance = 250f;

                    var impossible = pmcSettings[BotDifficulty.impossible];
                    impossible.Aiming.FasterCQBReactionsDistance = 60f;
                    impossible.Aiming.FasterCQBReactionsMinimum = 0.15f;
                    impossible.Aiming.MAX_AIMING_UPGRADE_BY_TIME = 0.15f;
                    impossible.Aiming.MAX_AIM_TIME = 1.0f;
                    impossible.Aiming.BASE_HIT_AFFECTION_DELAY_SEC = 0.25f;
                    impossible.Core.VisibleDistance = 275f;
                }
            }
        }

        private static SAINPresetClass CreateVeryHardPreset()
        {
            var preset = new SAINPresetClass(SAINDifficulty.veryhard);

            var global = preset.GlobalSettings;
            global.Shoot.RecoilMultiplier = 0.66f;
            global.Difficulty.ScatteringCoef = 0.85f;
            global.Aiming.AimCenterMassGlobal = false;
            global.Difficulty.VisibleDistCoef = 1.33f;
            global.Difficulty.GainSightCoef = 0.8f;
            global.Difficulty.PrecisionSpeedCoef = 0.8f;
            global.Difficulty.AccuracySpeedCoef = 0.8f;

            foreach (var bot in preset.BotSettings.SAINSettings)
            {
                bot.Value.DifficultyModifier = Mathf.Clamp(bot.Value.DifficultyModifier * 1.33f, 0.01f, 2f).Round100();
                foreach (var setting in bot.Value.Settings)
                {
                    setting.Value.Core.VisibleAngle = 170f;
                    setting.Value.Shoot.FireratMulti = 1.5f;
                    setting.Value.Shoot.BurstMulti = 2f;
                    setting.Value.Aiming.AimCenterMass = false;
                }
            }
            return preset;
        }

        private static SAINPresetClass CreateImpossiblePreset()
        {
            var preset = new SAINPresetClass(SAINDifficulty.deathwish);

            var global = preset.GlobalSettings;
            global.Shoot.RecoilMultiplier = 0.25f;

            global.Difficulty.ScatteringCoef = 0.01f;
            global.Difficulty.VisibleDistCoef = 3f;
            global.Difficulty.GainSightCoef = 0.65f;
            global.Difficulty.PrecisionSpeedCoef = 0.5f;
            global.Difficulty.AccuracySpeedCoef = 0.5f;

            global.Aiming.AimCenterMassGlobal = false;
            global.Look.NotLooking.NotLookingToggle = false;
            global.Aiming.PMCSAimForHead = true;

            foreach (var bot in preset.BotSettings.SAINSettings)
            {
                foreach (var setting in bot.Value.Settings)
                {
                    setting.Value.Core.VisibleAngle = 180f;
                    setting.Value.Shoot.FireratMulti = 3f;
                    setting.Value.Shoot.BurstMulti = 3f;
                    setting.Value.Aiming.AimCenterMass = false;
                    setting.Value.Core.VisibleAngle = 180;
                    setting.Value.Core.GainSightCoef *= 0.66f;
                }
            }
            return preset;
        }
    }
}