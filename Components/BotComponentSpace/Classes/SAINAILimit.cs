using SAIN.Components;
using SAIN.Helpers;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings;
using System;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINAILimit : BotBase, IBotClass
    {
        public event Action<AILimitSetting> OnAILimitChanged;
        public AILimitSetting CurrentAILimit { get; private set; }
        public float ClosestPlayerDistanceSqr { get; private set; }

        public SAINAILimit(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            base.SubscribeToPreset(UpdatePresetSettings);
        }

        public void Update()
        {
            checkAILimit();
        }

        public void Dispose()
        {
        }

        private void checkAILimit()
        {
            AILimitSetting lastLimit = CurrentAILimit;
            if (Bot.EnemyController.ActiveHumanEnemy)
            {
                CurrentAILimit = AILimitSetting.None;
                ClosestPlayerDistanceSqr = -1f;
            }
            else if (_checkDistanceTime < Time.time)
            {
                _checkDistanceTime = Time.time + _frequency * UnityEngine.Random.Range(0.9f, 1.1f);
                var gameWorld = GameWorldComponent.Instance;
                if (gameWorld != null &&
                    gameWorld.PlayerTracker.FindClosestHumanPlayer(out float closestPlayerSqrMag, Bot.Position) != null)
                {
                    CurrentAILimit = checkDistances(closestPlayerSqrMag);
                    ClosestPlayerDistanceSqr = closestPlayerSqrMag;
                }
            }
            if (lastLimit != CurrentAILimit)
            {
                OnAILimitChanged?.Invoke(CurrentAILimit);
            }
        }

        private AILimitSetting checkDistances(float closestPlayerSqrMag)
        {
            if (closestPlayerSqrMag < _farDistance)
            {
                return AILimitSetting.None;
            }
            if (closestPlayerSqrMag < _veryFarDistance)
            {
                return AILimitSetting.Far;
            }
            if (closestPlayerSqrMag < _narniaDistance)
            {
                return AILimitSetting.VeryFar;
            }
            return AILimitSetting.Narnia;
        }

        private float _checkDistanceTime;

        protected void UpdatePresetSettings(SAINPresetClass preset)
        {
            var aiLimit = GlobalSettingsClass.Instance.General.AILimit;
            _frequency = aiLimit.AILimitUpdateFrequency;
            _farDistance = aiLimit.AILimitRanges[AILimitSetting.Far].Sqr();
            _veryFarDistance = aiLimit.AILimitRanges[AILimitSetting.VeryFar].Sqr();
            _narniaDistance = aiLimit.AILimitRanges[AILimitSetting.Narnia].Sqr();
            if (SAINPlugin.DebugMode)
            {
                Logger.LogDebug($"Updated AI Limit Settings: [{_farDistance.Sqrt()}, {_veryFarDistance.Sqrt()}, {_narniaDistance.Sqrt()}]");
            }
        }

        private static float _frequency = 3f;
        private static float _farDistance = 200f * 200f;
        private static float _veryFarDistance = 300f * 300f;
        private static float _narniaDistance = 400f * 400f;
    }
}
