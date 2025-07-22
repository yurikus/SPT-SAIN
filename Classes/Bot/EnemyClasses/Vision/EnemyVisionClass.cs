using SAIN.Preset.GlobalSettings;
using SAIN.Preset;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using SAIN.Classes.Transform;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyVisionClass(EnemyData enemy) : EnemyBase(enemy), IBotEnemyClass
    {
        private const float _repeatContactMinSeenTime = 12f;
        private const float _lostContactMinSeenTime = 12f;

        public float EnemyVelocity => EnemyTransform.VelocityData.VelocityMagnitudeNormal;
        public bool FirstContactOccured { get; private set; }
        public bool ShallReportRepeatContact { get; set; }
        public bool ShallReportLostVisual { get; set; }
        public bool IsVisible { get; private set; }
        public bool InLineOfSight => EnemyParts.LineOfSight;
        public bool CanShoot => EnemyParts.CanShoot;
        public float VisibleStartTime { get; private set; }
        public float TimeSinceSeen => Seen ? Time.time - TimeLastSeen : -1;
        public bool Seen { get; private set; }
        public float TimeFirstSeen { get; private set; }
        public float TimeLastSeen { get; private set; }
        public float LastChangeVisionTime { get; private set; }
        public float LastGainSightResult { get; set; }
        public float GainSightCoef { get; private set; }
        public float VisionDistance => _visionDistance.Value;

        public EnemyAnglesClass Angles { get; } = new();

        public KeyValuePair<EnemyPart, EnemyPartData> _bodyPart;
        public KeyValuePair<EnemyPart, EnemyPartData> _headPart;
        
        public bool LineOfSight => EnemyParts.LineOfSight;
        public EnemyPartsClass EnemyParts { get; } = new EnemyPartsClass(enemy.EnemyPlayerComponent);

        private const float MAX_RANGE_VISION_UNKNOWN = 300f;

        public static float AIVisionRangeLimit(Enemy enemy)
        {
            float max = CheckMaxVisionRangeAI(enemy);
            if (!enemy.EnemyKnown && max > MAX_RANGE_VISION_UNKNOWN)
            {
                return MAX_RANGE_VISION_UNKNOWN;
            }
            return max;
        }

        private static float CheckMaxVisionRangeAI(Enemy enemy)
        {
            if (!enemy.IsAI)
            {
                return float.MaxValue;
            }
            var aiLimit = GlobalSettingsClass.Instance.General.AILimit;
            if (!aiLimit.LimitAIvsAIGlobal)
            {
                return float.MaxValue;
            }
            if (!aiLimit.LimitAIvsAIVision)
            {
                return float.MaxValue;
            }
            var enemyBot = enemy.EnemyPlayerComponent.BotComponent;
            if (enemyBot == null)
            {
                // if an enemy bot is not a sain bot, but has this bot as an enemy, dont limit at all.
                if (enemy.EnemyPlayerComponent.BotOwner?.Memory.GoalEnemy?.ProfileId == enemy.Bot.ProfileId)
                {
                    return float.MaxValue;
                }
                return GetMaxVisionRange(enemy.Bot.CurrentAILimit);
            }
            else
            {
                if (enemyBot.GoalEnemy?.EnemyProfileId == enemy.Bot.ProfileId)
                {
                    return float.MaxValue;
                }
                return GetMaxVisionRange(enemyBot.CurrentAILimit);
            }
        }

        private static float GetMaxVisionRange(AILimitSetting aiLimit)
        {
            return aiLimit switch {
                AILimitSetting.Far => GlobalSettingsClass.Instance.General.AILimit.MaxVisionRanges[AILimitSetting.Far],
                AILimitSetting.VeryFar => GlobalSettingsClass.Instance.General.AILimit.MaxVisionRanges[AILimitSetting.VeryFar],
                AILimitSetting.Narnia => GlobalSettingsClass.Instance.General.AILimit.MaxVisionRanges[AILimitSetting.Narnia],
                _ => float.MaxValue,
            };
        }

        public override void Init()
        {
            _bodyPart = Enemy.EnemyInfo._bodyPart;
            _headPart = Enemy.EnemyInfo._headPart;
            Enemy.Events.OnEnemyKnownChanged.OnToggle += OnEnemyKnownChanged;
            base.Init();
        }

        public override void ManualUpdate()
        {
            Angles.CalcAngles(Enemy);
            EnemyParts.Update();
            Enemy.Events.OnEnemyLineOfSightChanged.CheckToggle(LineOfSight);
            GainSightCoef = EnemyGainSightClass.GetGainSightModifier(Enemy);
            UpdateVisibleState();
            base.ManualUpdate();
        }

        public override void Dispose()
        {
            Enemy.Events.OnEnemyKnownChanged.OnToggle -= OnEnemyKnownChanged;
            base.Dispose();
        }

        public void OnEnemyKnownChanged(bool known, Enemy enemy)
        {
            if (!known)
            {
                UpdateVisibleState(true);
            }
        }

        public void UpdateVisibleState(bool forceOff = false)
        {
            bool wasVisible = IsVisible;
            if (forceOff)
            {
                IsVisible = false;
            }
            else if (!EnemyParts.CanBeSeen)
            {
                if (EnemyInfo.IsVisible) try { EnemyInfo.SetVisible(false); } catch { /* eft code */ }
                IsVisible = false;
            }
            else
            {
                IsVisible = EnemyInfo.IsVisible;
            }

            bool iamCurrentEnemy = Enemy.IsCurrentEnemy;
            if (iamCurrentEnemy && !IsVisible && wasVisible)
            {
               // try { BotOwner.CalcGoal(); } catch { /* eft code */  }
            }
            else if (!iamCurrentEnemy && IsVisible && Bot.GoalEnemy?.IsVisible != true)
            {
                //try { BotOwner.CalcGoal(); } catch { /* eft code */  }
            }

            if (IsVisible)
            {
                if (!wasVisible)
                {
                    VisibleStartTime = Time.time;
                    if (Seen && TimeSinceSeen >= _repeatContactMinSeenTime)
                    {
                        ShallReportRepeatContact = true;
                    }
                }
                if (!Seen)
                {
                    FirstContactOccured = true;
                    TimeFirstSeen = Time.time;
                    Seen = true;
                    Enemy.Events.EnemyFirstSeen();
                }

                TimeLastSeen = Time.time;
                Enemy.UpdateCurrentEnemyPos(EnemyTransform.Position);
            }

            if (!IsVisible)
            {
                if (wasVisible)
                    Enemy.UpdateLastSeenPosition(EnemyTransform.Position);

                if (Seen &&
                    TimeSinceSeen > _lostContactMinSeenTime &&
                    _nextReportLostVisualTime < Time.time)
                {
                    _nextReportLostVisualTime = Time.time + 20f;
                    ShallReportLostVisual = true;
                }
                VisibleStartTime = -1f;
            }

            Enemy.Events.OnVisionChange.CheckToggle(IsVisible);
            if (IsVisible != wasVisible)
            {
                LastChangeVisionTime = Time.time;
            }
        }

        private readonly EnemyVisionDistanceClass _visionDistance = new EnemyVisionDistanceClass(enemy);
        private float _nextReportLostVisualTime;
    }
}