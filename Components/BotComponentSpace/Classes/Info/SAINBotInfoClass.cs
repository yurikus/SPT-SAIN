using EFT;
using SAIN.Helpers;
using SAIN.Preset;
using SAIN.Preset.BotSettings.SAINSettings;
using SAIN.Preset.Personalities;
using System.Collections.Generic;
using System.Reflection;
using Random = UnityEngine.Random;

namespace SAIN.SAINComponent.Classes.Info
{
    public class SAINBotInfoClass : BotBase, IBotClass
    {
        public SAINSettingsClass FileSettings
        {
            get
            {
                if (_fileSettings == null)
                {
                    _fileSettings = SAINPresetClass.Instance.BotSettings.GetSAINSettings(Profile.WildSpawnType, Profile.BotDifficulty);
                }
                return _fileSettings;
            }
        }

        public BotDifficultyClass Difficulty { get; }
        public BotProfile Profile { get; private set; }
        public WeaponInfoClass WeaponInfo { get; private set; }
        public EPersonality Personality { get; private set; }
        public PersonalityBehaviorSettings PersonalitySettings => PersonalitySettingsClass?.Behavior;
        public PersonalitySettingsClass PersonalitySettingsClass { get; private set; }
        public float TimeBeforeSearch { get; private set; } = 0f;
        public float HoldGroundDelay { get; private set; }
        public float PercentageBeforeExtract { get; set; } = -1f;
        public bool ForceExtract { get; set; } = false;
        public float ForgetEnemyTime { get; private set; }
        public float AggressionMultiplier => Difficulty.AggressionModifier;

        public SAINBotInfoClass(BotComponent sain) : base(sain)
        {
            Profile = new BotProfile(sain);
            WeaponInfo = new WeaponInfoClass(sain);
            Personality = GetPersonality(out var settings);
            PersonalitySettingsClass = settings;
            Difficulty = new BotDifficultyClass(sain);
        }

        public void Init()
        {
            base.SubscribeToPreset(UpdatePresetSettings);
            WeaponInfo.Init();
            Difficulty.Init();
        }

        public void Update()
        {
            WeaponInfo.Update();
            Difficulty.Update();
        }

        public void Dispose()
        {
            WeaponInfo.Dispose();
            Difficulty.Dispose();
        }

        public void SetPersonality(EPersonality personality)
        {
            if (SAINPlugin.LoadedPreset.PersonalityManager.PersonalityDictionary.TryGetValue(personality, out var personalitySettings))
            {
                PersonalitySettingsClass = personalitySettings;
                Personality = personality;
            }
        }

        protected void UpdatePresetSettings(SAINPresetClass preset)
        {
            Difficulty.UpdateSettings(preset);
            CalcTimeBeforeSearch();
            CalcHoldGroundDelay();
            UpdateExtractTime();
            SetConfigValues(FileSettings);
        }

        public void CalcHoldGroundDelay()
        {
            var settings = PersonalitySettings;
            float baseTime = settings.General.HoldGroundBaseTime * AggressionMultiplier;

            float min = settings.General.HoldGroundMinRandom;
            float max = settings.General.HoldGroundMaxRandom;
            HoldGroundDelay = baseTime.Randomize(min, max).Round100();
        }

        public void CalcTimeBeforeSearch()
        {
            float searchTime;
            if (Profile.WildSpawnType == WildSpawnType.bossKilla || Profile.WildSpawnType == WildSpawnType.bossTagilla)
            {
                searchTime = 0.1f;
            }
            else if (Profile.IsFollower && Bot.Squad.BotInGroup)
            {
                searchTime = 10f;
            }
            else
            {
                searchTime = PersonalitySettings.Search.SearchBaseTime;
            }

            searchTime = (searchTime.Randomize(0.66f, 1.33f) / AggressionMultiplier).Round100();
            if (searchTime < 0.1f)
            {
                searchTime = 0.1f;
            }

            TimeBeforeSearch = searchTime;
            float random = 30f.Randomize(0.75f, 1.25f).Round100();
            float forgetTime = searchTime + random;
            if (forgetTime < 240f)
            {
                forgetTime = 240f.Randomize(0.9f, 1.1f).Round100();
            }

            BotOwner.Settings.FileSettings.Mind.TIME_TO_FORGOR_ABOUT_ENEMY_SEC = forgetTime;
            ForgetEnemyTime = forgetTime;
        }

        private void UpdateExtractTime()
        {
            float percentage = Random.Range(FileSettings.Mind.MinExtractPercentage, FileSettings.Mind.MaxExtractPercentage);

            var squad = Bot?.Squad;
            var members = squad?.Members;
            if (squad != null && squad.BotInGroup && members != null && members.Count > 0)
            {
                if (squad.IAmLeader)
                {
                    PercentageBeforeExtract = percentage;
                    foreach (var member in members)
                    {
                        var infocClass = member.Value?.Info;
                        if (infocClass != null)
                        {
                            infocClass.PercentageBeforeExtract = percentage;
                        }
                    }
                }
                else if (PercentageBeforeExtract == -1f)
                {
                    var Leader = squad?.LeaderComponent?.Info;
                    if (Leader != null)
                    {
                        PercentageBeforeExtract = Leader.PercentageBeforeExtract;
                    }
                }
            }
            else
            {
                PercentageBeforeExtract = percentage;
            }
        }

        public EPersonality GetPersonality(out PersonalitySettingsClass settings)
        {
            var dictionary = SAINPlugin.LoadedPreset.PersonalityManager.PersonalityDictionary;
            return dictionary.GetPersonality(this, out settings);
        }

        private void SetConfigValues(SAINSettingsClass sainFileSettings)
        {
            var eftFileSettings = BotOwner.Settings.FileSettings;
            if (EFTSettingsCategories == null)
            {
                var flags = BindingFlags.Instance | BindingFlags.Public;

                EFTSettingsCategories = eftFileSettings.GetType().GetFields(flags);
                foreach (FieldInfo field in EFTSettingsCategories)
                {
                    EFTSettingsFields.Add(field, field.FieldType.GetFields(flags));
                }

                SAINSettingsCategories = sainFileSettings.GetType().GetFields(flags);
                foreach (FieldInfo field in SAINSettingsCategories)
                {
                    SAINSettingsFields.Add(field, field.FieldType.GetFields(flags));
                }
            }

            foreach (FieldInfo sainCategoryField in SAINSettingsCategories)
            {
                FieldInfo eftCategoryField = Reflection.FindFieldByName(sainCategoryField.Name, EFTSettingsCategories);
                if (eftCategoryField != null)
                {
                    object sainCategory = sainCategoryField.GetValue(sainFileSettings);
                    object eftCategory = eftCategoryField.GetValue(eftFileSettings);

                    FieldInfo[] sainFields = SAINSettingsFields[sainCategoryField];
                    FieldInfo[] eftFields = EFTSettingsFields[eftCategoryField];

                    foreach (FieldInfo sainVarField in sainFields)
                    {
                        FieldInfo eftVarField = Reflection.FindFieldByName(sainVarField.Name, eftFields);
                        if (eftVarField != null)
                        {
                            object sainValue = sainVarField.GetValue(sainCategory);
                            if (SAINPlugin.DebugMode)
                            {
                                //string message = $"[{eftVarField.Name}] : Default Value = [{eftVarField.GetValue(eftCategory)}] New Value = [{sainValue}]";
                                //Logger.LogInfo(message);
                                //Logger.NotifyInfo(message);
                            }
                            eftVarField.SetValue(eftCategory, sainValue);
                        }
                    }
                }
            }
        }

        private SAINSettingsClass _fileSettings;

        private static FieldInfo[] EFTSettingsCategories;
        private static FieldInfo[] SAINSettingsCategories;

        private static readonly Dictionary<FieldInfo, FieldInfo[]> EFTSettingsFields = new();
        private static readonly Dictionary<FieldInfo, FieldInfo[]> SAINSettingsFields = new();
    }
}