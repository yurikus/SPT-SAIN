using EFT;
using SAIN.Components;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections.Generic;
using UnityEngine;

// Found in Botowner.Looksensor
using EnemyVisionCheck = GClass564;
using LookAllData = GClass589;

namespace SAIN.SAINComponent.Classes
{
    public class SAINBotLookClass : BotBase
    {
        private const float VISION_FREQ_INACTIVE_BOT_COEF = 5f;
        private const float VISION_FREQ_ACTIVE_BOT_COEF = 2f;
        private const float VISION_FREQ_CURRENT_ENEMY = 0.04f;
        private const float VISION_FREQ_UNKNOWN_ENEMY = 0.1f;
        private const float VISION_FREQ_KNOWN_ENEMY = 0.05f;

        public SAINBotLookClass(BotComponent component) : base(component)
        {
            LookData = new LookAllData();
        }

        public override void Init()
        {
            _enemies = Bot.EnemyController.Enemies;
            base.Init();
        }

        private Dictionary<string, Enemy> _enemies;
        public readonly LookAllData LookData;

        // This (And the methods it calls) mirrors a large part of BSG's LookSensor
        // Look at that for potential changes between versions
        public int UpdateLook(float deltaTime)
        {
            if (BotOwner.LeaveData == null || BotOwner.LeaveData.LeaveComplete)
            {
                return 0;
            }

            int numUpdated = UpdateLookForEnemies(LookData, deltaTime);
            UpdateLookData(LookData);
            return numUpdated;
        }

        public void UpdateLookData(LookAllData lookData)
        {
            for (int i = 0; i < lookData.ReportsData.Count; i++)
            {
                EnemyVisionCheck enemyVision = lookData.ReportsData[i];
                BotOwner.BotsGroup.ReportAboutEnemy(enemyVision.Enemy, enemyVision.VisibleOnlyBySence);
            }

            if (lookData.ReportsData.Count > 0)
            {
                BotOwner.Memory.SetLastTimeSeeEnemy();
            }

            if (lookData.ShallRecalcGoal)
            {
                BotOwner.CalcGoal();
            }

            lookData.Reset();
        }

        private int UpdateLookForEnemies(LookAllData lookAll, float deltaTime)
        {
            int updated = 0;
            _cachedList.Clear();
            _cachedList.AddRange(Bot.EnemyController.Enemies.Values);
            foreach (Enemy enemy in _cachedList)
                if (ShallCheckEnemy(enemy) && CheckEnemy(enemy, lookAll, deltaTime))
                    updated++;

            _cachedList.Clear();
            return updated;
        }

        private readonly List<Enemy> _cachedList = new();

        private bool ShallCheckEnemy(Enemy enemy)
        {
            if (enemy == null) return false;
            if (!enemy.CheckValid() || !Enemy.IsEnemyActive(enemy)) return false;
            if (!enemy.Vision.EnemyParts.CanBeSeen || !enemy.Vision.Angles.CanBeSeen)
            {
                SetNotVis(enemy);
                return false;
            }
            return true;
        }

        private void SetNotVis(Enemy enemy)
        {
            foreach (var part in enemy.EnemyInfo.AllActiveParts.Values)
            {
                if (part.IsVisible || part.VisibleType == EEnemyPartVisibleType.Sence)
                {
                    part.UpdateVisibility(BotOwner, false, false, false, Time.deltaTime);
                }
            }

            if (enemy.EnemyInfo.IsVisible)
            {
                enemy.EnemyInfo.SetVisible(false);
            }
        }

        private bool CheckEnemy(Enemy enemy, LookAllData lookAll, float deltaTime)
        {
            // ArchangelWTF: In AITaskManager.UpdateGroup timeSince is passed here
            enemy.EnemyInfo.CheckLookEnemy(lookAll, deltaTime);
            return true;
        }

        private float GetDelay(Enemy enemy)
        {
            float updateFreqCoef = enemy.UpdateFrequencyCoefNormal + 1f;
            float baseDelay = CalcBaseDelay(enemy) * updateFreqCoef;
            if (!enemy.IsAI)
            {
                return baseDelay;
            }
            var active = Bot.BotActivation;
            if (!active.BotActive || active.BotInStandBy)
            {
                return baseDelay * VISION_FREQ_INACTIVE_BOT_COEF;
            }
            return baseDelay * VISION_FREQ_ACTIVE_BOT_COEF;
        }

        private float CalcBaseDelay(Enemy enemy)
        {
            if (enemy.IsCurrentEnemy)
            {
                return VISION_FREQ_CURRENT_ENEMY;
            }

            if (enemy.EnemyKnown)
            {
                return VISION_FREQ_KNOWN_ENEMY;
            }

            return VISION_FREQ_UNKNOWN_ENEMY;
        }
    }
}