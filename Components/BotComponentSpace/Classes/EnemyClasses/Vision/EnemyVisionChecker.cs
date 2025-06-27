using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Models.Enums;
using SAIN.Models.Structs;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyVisionChecker : EnemyBase, IBotClass
    {
        public float LastCheckLookTime { get; set; }
        public float LastCheckLOSTime { get; set; }
        public bool LineOfSight => EnemyParts.LineOfSight;
        public EnemyPartsClass EnemyParts { get; }

        public EnemyVisionChecker(Enemy enemy) : base(enemy)
        {
            EnemyParts = new EnemyPartsClass(enemy);
            _transform = enemy.Bot.Transform;
            _startVisionTime = Time.time + UnityEngine.Random.Range(0.0f, 0.33f);
        }

        public void Init()
        {
            SubscribeToPreset(UpdatePresetSettings);
        }

        public void Update()
        {
            EnemyParts.Update();
            Enemy.Events.OnEnemyLineOfSightChanged.CheckToggle(LineOfSight);
        }

        public void Dispose()
        {
        }

        public void CheckVision(out bool didCheck)
        {
            // staggers ai vision over a few quarters of a second
            if (!ShallStart())
            {
                didCheck = false;
                return;
            }

            didCheck = true;
            Enemy.Events.OnEnemyLineOfSightChanged.CheckToggle(LineOfSight);
        }

        private bool ShallStart()
        {
            if (_visionStarted)
            {
                return true;
            }
            if (_startVisionTime < Time.time)
            {
                _visionStarted = true;
                return true;
            }
            return false;
        }

        private const float MAX_RANGE_VISION_UNKNOWN = 300f;

        public float AIVisionRangeLimit()
        {
            float max = CheckMaxVisionRangeAI();
            if (!Enemy.EnemyKnown && max > MAX_RANGE_VISION_UNKNOWN)
            {
                return MAX_RANGE_VISION_UNKNOWN;
            }
            return max;
        }

        private float CheckMaxVisionRangeAI()
        {
            if (!Enemy.IsAI)
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
            var enemyBot = Enemy.EnemyPerson.AIInfo.BotComponent;
            if (enemyBot == null)
            {
                // if an enemy bot is not a sain bot, but has this bot as an enemy, dont limit at all.
                if (Enemy.EnemyPerson.AIInfo.BotOwner?.Memory.GoalEnemy?.ProfileId == Bot.ProfileId)
                {
                    return float.MaxValue;
                }
                return GetMaxVisionRange(Bot.CurrentAILimit);
            }
            else
            {
                if (enemyBot.Enemy?.EnemyProfileId == Bot.ProfileId)
                {
                    return float.MaxValue;
                }
                return GetMaxVisionRange(enemyBot.CurrentAILimit);
            }
        }

        private static float GetMaxVisionRange(AILimitSetting aiLimit)
        {
            switch (aiLimit)
            {
                default:
                    return float.MaxValue;

                case AILimitSetting.Far:
                    return _farDistance;

                case AILimitSetting.VeryFar:
                    return _veryFarDistance;

                case AILimitSetting.Narnia:
                    return _narniaDistance;
            }
        }

        public void UpdatePresetSettings(SAINPresetClass preset)
        {
            var aiLimit = preset.GlobalSettings.General.AILimit;
            _farDistance = aiLimit.MaxVisionRanges[AILimitSetting.Far];
            _veryFarDistance = aiLimit.MaxVisionRanges[AILimitSetting.VeryFar];
            _narniaDistance = aiLimit.MaxVisionRanges[AILimitSetting.Narnia];
        }

        private bool _visionStarted;
        private float _startVisionTime;
        private PersonTransformClass _transform;
        private static float _farDistance;
        private static float _veryFarDistance;
        private static float _narniaDistance;
    }
}