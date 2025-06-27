using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class BotSurgery : BotMedicalBase, IBotClass
    {
        public BotSurgery(SAINBotMedicalClass medical) : base(medical)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        public bool SurgeryStarted
        {
            get
            {
                return _surgeryStarted;
            }
            set
            {
                if (_surgeryStarted != value && value)
                {
                    SurgeryStartTime = Time.time;
                }
                _surgeryStarted = value;
            }
        }

        public float SurgeryStartTime { get; private set; }

        private bool _surgeryStarted;

        public bool AreaClearForSurgery
        {
            get
            {
                if (_nextCheckClearTime < Time.time)
                {
                    _nextCheckClearTime = Time.time + _checkClearFreq;
                    _areaClear = shallTrySurgery();
                }
                return _areaClear;
            }
        }

        private bool _areaClear;
        private float _nextCheckClearTime;
        private float _checkClearFreq = 0.25f;

        private bool shallTrySurgery()
        {
            const float useSurgDist = 100f * 100f;
            bool useSurgery = false;

            if (_canStartSurgery)
            {
                var enemy = Bot.Enemy;
                if (Bot.EnemyController.AtPeace)
                {
                    if (Bot.CurrentTargetPosition == null)
                    {
                        useSurgery = true;
                    }
                    else if ((Bot.CurrentTargetPosition.Value - Bot.Position).sqrMagnitude > useSurgDist)
                    {
                        useSurgery = true;
                    }
                }
                else
                {
                    useSurgery = checkAllClear(SurgeryStarted);
                }
            }

            return useSurgery;
        }

        public bool _canStartSurgery => BotOwner?.Medecine?.SurgicalKit?.ShallStartUse() == true && BotOwner?.Medecine?.FirstAid?.IsBleeding == false;

        private bool checkAllClear(bool surgeryStarted)
        {
            if (_nextCheckEnemiesTime < Time.time)
            {
                float timeAdd = surgeryStarted ? 0.5f : 0.1f;
                _nextCheckEnemiesTime = Time.time + timeAdd;

                float minPathDist = surgeryStarted ? 50f : 100f;
                float minTimeSinceLastKnown = surgeryStarted ? 30f : 60f;

                _allClear = checkEnemies(minPathDist, minTimeSinceLastKnown);
            }
            return _allClear;
        }

        private bool checkEnemies(float minPathDist, float minTimeSinceLastKnown)
        {
            bool allClear = true;
            var enemies = Bot.EnemyController.Enemies;
            foreach (var enemy in enemies.Values)
            {
                if (!checkThisEnemy(enemy, minPathDist, minTimeSinceLastKnown))
                {
                    allClear = false;
                    break;
                }
            }
            return allClear;
        }

        private bool checkThisEnemy(Enemy enemy, float minPathDist, float minTimeSinceLastKnown)
        {
            if (enemy?.EnemyPlayer?.HealthController.IsAlive == true
                && (enemy.Seen || enemy.Heard)
                && enemy.TimeSinceLastKnownUpdated < 360f)
            {
                if (enemy.IsVisible)
                {
                    return false;
                }
                if (enemy.TimeSinceLastKnownUpdated < minTimeSinceLastKnown)
                {
                    return false;
                }
                if (enemy.Path.PathDistance < minPathDist)
                {
                    return false;
                }
            }
            return true;
        }

        private bool _allClear;
        private float _nextCheckEnemiesTime;
    }
}