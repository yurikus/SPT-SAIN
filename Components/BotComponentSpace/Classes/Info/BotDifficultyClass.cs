using SAIN.Helpers;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings;

namespace SAIN.SAINComponent.Classes
{
    public class BotDifficultyClass : BotBase, IBotClass
    {
        public TemporaryStatModifiers GlobalDifficultyModifiers { get; }
        public TemporaryStatModifiers BotDifficultyModifiers { get; }
        public TemporaryStatModifiers PersonalityDifficultyModifiers { get; }
        public TemporaryStatModifiers LocationDifficultyModifiers { get; }

        public float AggressionModifier { get; private set; } = 1f;
        public float HearingDistanceModifier { get; private set; } = 1f;

        public BotDifficultyClass(BotComponent sain) : base(sain)
        {
            GlobalDifficultyModifiers = new TemporaryStatModifiers();
            BotDifficultyModifiers = new TemporaryStatModifiers();
            PersonalityDifficultyModifiers = new TemporaryStatModifiers();
            LocationDifficultyModifiers = new TemporaryStatModifiers();
        }

        public void Init()
        {
        }

        public void Update()
        {
        }

        public void Dispose()
        {
            dismiss();
        }

        public void UpdateSettings(SAINPresetClass preset)
        {
            dismiss();
            applyGlobal(preset);
            applyBot(preset);
            applyLocation(preset);
            applyPersonality(preset);
            apply();

            createCustomMods(preset);
        }

        private void createCustomMods(SAINPresetClass preset)
        {
            var globalSettings = preset.GlobalSettings.Difficulty;
            var botSettings = Bot.Info.FileSettings.Difficulty;
            var personalitySettings = Bot.Info.PersonalitySettingsClass.Difficulty;

            HearingDistanceModifier = 1f *
                globalSettings.HearingDistanceCoef *
                botSettings.HearingDistanceCoef *
                personalitySettings.HearingDistanceCoef;

            AggressionModifier = 1f *
                globalSettings.AggressionCoef *
                botSettings.AggressionCoef *
                personalitySettings.AggressionCoef;

            var locationSettings = preset.GlobalSettings.Location.Current();
            if (locationSettings == null)
            {
                return;
            }

            HearingDistanceModifier *= locationSettings.HearingDistanceCoef;
            AggressionModifier *= locationSettings.AggressionCoef;
        }

        private void applyGlobal(SAINPresetClass preset)
        {
            var globalSettings = preset.GlobalSettings.Difficulty;
            var mods = GlobalDifficultyModifiers.Modifiers;

            mods.AccuratySpeedCoef = globalSettings.AccuracySpeedCoef;
            mods.PrecicingSpeedCoef = globalSettings.PrecisionSpeedCoef;
            mods.VisibleDistCoef = globalSettings.VisibleDistCoef;
            mods.ScatteringCoef = globalSettings.ScatteringCoef;
            //mods.PriorityScatteringCoef = globalSettings.PriorityScatteringCoef;
            mods.GainSightCoef = globalSettings.GainSightCoef;
            mods.HearingDistCoef = globalSettings.HearingDistanceCoef;
        }

        private void apply(DifficultySettings settings, TemporaryStatModifiers mods)
        {
            mods.Modifiers.AccuratySpeedCoef = settings.AccuracySpeedCoef;
            mods.Modifiers.PrecicingSpeedCoef = settings.PrecisionSpeedCoef;
            mods.Modifiers.VisibleDistCoef = settings.VisibleDistCoef;
            mods.Modifiers.ScatteringCoef = settings.ScatteringCoef;
            //mods.PriorityScatteringCoef = botSettings.PriorityScatteringCoef;
            mods.Modifiers.GainSightCoef = settings.GainSightCoef;
            mods.Modifiers.HearingDistCoef = settings.HearingDistanceCoef;
        }

        private void applyBot(SAINPresetClass preset)
        {
            var botSettings = Bot.Info.FileSettings.Difficulty;
            var mods = BotDifficultyModifiers.Modifiers;

            mods.AccuratySpeedCoef = botSettings.AccuracySpeedCoef;
            mods.PrecicingSpeedCoef = botSettings.PrecisionSpeedCoef;
            mods.VisibleDistCoef = botSettings.VisibleDistCoef;
            mods.ScatteringCoef = botSettings.ScatteringCoef;
            //mods.PriorityScatteringCoef = botSettings.PriorityScatteringCoef;
            mods.GainSightCoef = botSettings.GainSightCoef;
            mods.HearingDistCoef = botSettings.HearingDistanceCoef;
        }

        private void applyLocation(SAINPresetClass preset)
        {
            var locationSettings = preset.GlobalSettings.Location.Current();
            if (locationSettings == null)
            {
                return;
            }
            var mods = LocationDifficultyModifiers.Modifiers;

            mods.AccuratySpeedCoef = locationSettings.AccuracySpeedCoef;
            mods.PrecicingSpeedCoef = locationSettings.PrecisionSpeedCoef;
            mods.VisibleDistCoef = locationSettings.VisibleDistCoef;
            mods.ScatteringCoef = locationSettings.ScatteringCoef;
            //mods.PriorityScatteringCoef = locationSettings.PriorityScatteringCoef;
            mods.GainSightCoef = locationSettings.GainSightCoef;
            mods.HearingDistCoef = locationSettings.HearingDistanceCoef;
        }

        private void applyPersonality(SAINPresetClass preset)
        {
            var personalitySettings = Bot.Info.PersonalitySettingsClass.Difficulty;
            var mods = PersonalityDifficultyModifiers.Modifiers;

            mods.AccuratySpeedCoef = personalitySettings.AccuracySpeedCoef;
            mods.PrecicingSpeedCoef = personalitySettings.PrecisionSpeedCoef;
            mods.VisibleDistCoef = personalitySettings.VisibleDistCoef;
            mods.ScatteringCoef = personalitySettings.ScatteringCoef;
            //mods.PriorityScatteringCoef = personalitySettings.PriorityScatteringCoef;
            mods.GainSightCoef = personalitySettings.GainSightCoef;
            mods.HearingDistCoef = personalitySettings.HearingDistanceCoef;
        }

        private void apply()
        {
            var current = BotOwner.Settings.Current;
            current.Apply(GlobalDifficultyModifiers.Modifiers);
            current.Apply(BotDifficultyModifiers.Modifiers);
            current.Apply(PersonalityDifficultyModifiers.Modifiers);
            current.Apply(LocationDifficultyModifiers.Modifiers);
        }

        private void dismiss()
        {
            var current = BotOwner.Settings.Current;
            current.Dismiss(GlobalDifficultyModifiers.Modifiers);
            current.Dismiss(BotDifficultyModifiers.Modifiers);
            current.Dismiss(PersonalityDifficultyModifiers.Modifiers);
            current.Dismiss(LocationDifficultyModifiers.Modifiers);
        }

        private void applyMods(TemporaryStatModifiers mods)
        {
            BotOwner.Settings.Current.Apply(mods.Modifiers);
        }
    }
}