using Comfort.Common;
using EFT;
using SAIN.Components;
using SAIN.Helpers.Events;
using SAIN.Models.Enums;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINActivationClass(BotComponent botComponent) : BotComponentClassBase(botComponent)
    {
        private const float ACTIVATE_STANDBY_HUMAN = 150;
        private const float ACTIVATE_STANDBY_AI = 50;
        private const float ACTIVATE_STANDBY_CHECK_FREQ = 3f;

        public ESAINLayer ActiveLayer { get; private set; }

        public bool BotActive => BotActiveToggle.Value;
        public ToggleEvent BotActiveToggle { get; } = new ToggleEvent();
        public bool BotInStandBy => BotStandByToggle.Value;
        public ToggleEvent BotStandByToggle { get; } = new ToggleEvent();
        public bool GameEnding => GameEndingToggle.Value;
        public ToggleEvent GameEndingToggle { get; } = new ToggleEvent();
        public bool SAINLayersActive => SAINLayersActiveToggle.Value;
        public ToggleEvent SAINLayersActiveToggle { get; } = new ToggleEvent();
        public bool BotInCombat => BotInCombatToggle.Value;
        public ToggleEvent BotInCombatToggle { get; } = new ToggleEvent();

        public void SetActive(bool botActive)
        {
            BotActiveToggle.CheckToggle(botActive);

            if (!botActive)
            {
                BotStandByToggle.CheckToggle(true);
                ActiveLayer = ESAINLayer.None;
                SAINLayersActiveToggle.CheckToggle(false);
            }
        }

        public void SetInCombat(bool inCombat)
        {
            BotInCombatToggle.CheckToggle(!inCombat);
        }

        public void SetActiveLayer(ESAINLayer layer)
        {
            ActiveLayer = layer;
        }

        public override void ManualUpdate()
        {
            CheckGameEnding();
            CheckBotActive();
            CheckStandBy();
            SAINLayersActiveToggle.CheckToggle(ActiveLayer != ESAINLayer.None);
        }

        private void CheckBotActive()
        {
            if (GameEnding && BotActive)
                SetActive(false);

            if (!GameEnding &&
                !BotActive &&
                BotOwner != null && 
                BotOwner.BotState == EBotState.Active && 
                BotOwner.StandBy.StandByType == BotStandByType.active)
            {
                Logger.LogWarning($"Bot not active but should be!");
                SetActive(true);
            }
        }

        private void CheckStandBy()
        {
            bool standby = BotActive && BotOwner?.StandBy?.StandByType != BotStandByType.active;
            if (standby)
            {
                if (Bot.HasEnemy)
                {
                    //Logger.LogWarning($"Had to activate bot manually because they were in stand by.");
                    BotOwner.StandBy.Activate();
                    standby = false;
                }
                else if (CheckAllEnemies())
                {
                    Logger.LogDebug($"[{BotOwner.name}] disabled standby due to enemies being near.");
                    BotOwner.StandBy.Activate();
                    standby = false;
                }
            }

            BotStandByToggle.CheckToggle(standby);
        }

        private bool CheckAllEnemies()
        {
            if (_nextCheckEnemiesTime > Time.time)
            {
                return false;
            }
            _nextCheckEnemiesTime = Time.time + ACTIVATE_STANDBY_CHECK_FREQ;

            var enemies = Bot.EnemyController.Enemies.Values;
            foreach (var enemy in enemies)
                if (enemy != null &&
                    (enemy.InLineOfSight ||
                    (enemy.IsAI && enemy.RealDistance < ACTIVATE_STANDBY_AI) ||
                    (!enemy.IsAI && enemy.RealDistance < ACTIVATE_STANDBY_HUMAN)))
                    return true;

            return false;
        }

        private void CheckGameEnding()
        {
            var botGame = Singleton<IBotGame>.Instance;
            bool gameEnding = botGame == null || botGame.Status == GameStatus.Stopping;
            GameEndingToggle.CheckToggle(gameEnding);
        }

        public override void Init()
        {
            SetActive(true);
            base.Init();
        }

        public override void Dispose()
        {
            SetActive(false);
            base.Dispose();
        }

        private float _nextCheckEnemiesTime;
    }
}