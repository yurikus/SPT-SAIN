using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINHearingSensorClass : BotComponentClassBase
    {
        public event Action<AISoundData, Enemy> OnEnemySoundHeard;

        public HearingInputClass SoundInput { get; }
        public HearingAnalysisClass Analysis { get; }
        public HearingBulletAnalysisClass BulletAnalysis { get; }
        public HearingDispersionClass Dispersion { get; }

        public SAINHearingSensorClass(BotComponent sain) : base(sain)
        {
            TickRequirement = ESAINTickState.OnlyNoSleep;
            SoundInput = new HearingInputClass(this);
            Analysis = new HearingAnalysisClass(this);
            BulletAnalysis = new HearingBulletAnalysisClass(this);
            Dispersion = new HearingDispersionClass(this);
        }

        public override void Init()
        {
            SoundInput.Init();
            Analysis.Init();
            BulletAnalysis.Init();
            Dispersion.Init();
            base.Init();
        }

        public override void ManualUpdate()
        {
            SoundInput.ManualUpdate();
            Analysis.ManualUpdate();
            BulletAnalysis.ManualUpdate();
            Dispersion.ManualUpdate();
            base.ManualUpdate();
        }

        public override void Dispose()
        {
            SoundInput.Dispose();
            Analysis.Dispose();
            BulletAnalysis.Dispose();
            Dispersion.Dispose();
            base.Dispose();
        }

        public void ReactToBulletFlyBy(AISoundData sound, float FlyByDistance)
        {
            Vector3 EstimatedPosition;
            bool underFire = FlyByDistance <= SAINPlugin.LoadedPreset.GlobalSettings.Mind.MaxUnderFireDistance;
            if (!SoundInput.IgnoreHearing || underFire)
            {
                EstimatedPosition = Dispersion.CalcRandomizedPosition(sound, 1f);
                ReactToBulletFlyBy(sound, FlyByDistance, EstimatedPosition, underFire);
                OnEnemySoundHeard?.Invoke(sound, sound.Enemy);
                if (sound.HeardPlayer.IsYourPlayer)
                    Logger.LogDebug($"Bullet FlyBy. SoundType: [{sound.Sound.SoundType}] Delay: [{Time.time - sound.Sound.TimeCreated}] Distance: [{sound.PlayerDistance}] Player: [{sound.Sound.PlayerComponent?.Player?.Profile?.Nickname}]");
            }
        }

        public void ReactToHeardSound(AISoundData sound)
        {
            Vector3 EstimatedPosition;
            //bool Heard = Analysis.CheckIfSoundHeard(sound);
            //if (sound.IsGunShot &&
            //    BulletAnalysis.DoIFeelBullet(sound) &&
            //    BulletAnalysis.DidShotFlyByMe(sound, out Vector3 ProjectionPoint, out float ProjectionPointDistance))
            //{
            //    bool underFire = ProjectionPointDistance <= SAINPlugin.LoadedPreset.GlobalSettings.Mind.MaxUnderFireDistance;
            //    if (!SoundInput.IgnoreHearing || underFire)
            //    {
            //        float unheardDispersionModifier = Heard ? 1f : GlobalSettingsClass.Instance.Hearing.HEAR_DISPERSION_BULLET_FELT_MOD;
            //        EstimatedPosition = Dispersion.CalcRandomizedPosition(sound, unheardDispersionModifier);
            //        ReactToBulletFlyBy(sound, ProjectionPointDistance, EstimatedPosition, underFire);
            //        OnEnemySoundHeard?.Invoke(sound, sound.Enemy);
            //        if (sound.HeardPlayer.IsYourPlayer)
            //            Logger.LogDebug($"Bullet FlyBy. SoundType: [{sound.Sound.SoundType}] Delay: [{Time.time - sound.Sound.TimeCreated}] Distance: [{sound.PlayerDistance}] Player: [{sound.Sound.PlayerComponent?.Player?.Profile?.Nickname}]");
            //        return;
            //    }
            //}
            if (Analysis.CheckIfSoundHeard(sound))
            {
                if (sound.IsGunShot && !ShallChaseGunshot(sound.PlayerDistance))
                {
                    return;
                }
                EstimatedPosition = Dispersion.CalcRandomizedPosition(sound, 1f);
                Bot.Squad.SquadInfo?.AddPointToSearch(sound.Enemy, EstimatedPosition, sound, Bot);
                CheckCalcGoal();
                OnEnemySoundHeard?.Invoke(sound, sound.Enemy);
                //if (sound.HeardPlayer.IsYourPlayer)
                //    Logger.LogDebug($"Enemy Heard. SoundType: [{sound.Sound.SoundType}] Delay: [{Time.time - sound.Sound.TimeCreated}] Distance: [{sound.PlayerDistance}] Player: [{sound.Sound.PlayerComponent?.Player?.Profile?.Nickname}]");
            }
        }

        private bool ShallChaseGunshot(float Distance)
        {
            var searchSettings = Bot.Info.PersonalitySettings.Search;
            if (searchSettings.WillChaseDistantGunshots)
            {
                return true;
            }
            if (Distance > searchSettings.AudioStraightDistanceToIgnore)
            {
                return false;
            }
            return true;
        }

        private void ReactToBulletFlyBy(AISoundData sound, float ProjectionPointDistance, Vector3 EstimatedPosition, bool UnderFire)
        {
            Enemy enemy = sound.Enemy;
            if (UnderFire)
            {
                BotOwner?.HearingSensor?.OnEnemySounHearded?.Invoke(EstimatedPosition, sound.PlayerDistance, sound.SoundType.Convert());
                Bot.Memory.SetUnderFire(enemy, EstimatedPosition);
                enemy.SetEnemyAsSniper(enemy.RealDistance > GlobalSettings.Mind.ENEMYSNIPER_DISTANCE);
            }
            Bot.Suppression.CheckAddSuppression(enemy, ProjectionPointDistance);
            enemy.Status.ShotAtMeRecently = true;
            Bot.Squad.SquadInfo?.AddPointToSearch(enemy, EstimatedPosition, sound, Bot);
            CheckCalcGoal();
        }

        private void CheckCalcGoal()
        {
            if (BotOwner.Memory.GoalEnemy == null)
            {
                try
                {
                    BotOwner.BotsGroup.CalcGoalForBot(BotOwner);
                }
                catch { /* Gotta love eft code throwing errors randomly */ }
            }
        }
    }
}