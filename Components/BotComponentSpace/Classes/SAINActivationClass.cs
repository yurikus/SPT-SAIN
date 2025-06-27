using Comfort.Common;
using EFT;
using SAIN.Helpers.Events;
using SAIN.Models.Enums;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINActivationClass : BotBase, IBotClass
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

        public void SetActiveLayer(ESAINLayer layer)
        {
            ActiveLayer = layer;
        }

        public void Update()
        {
            checkActive();
            //checkSpeedReset();
        }

        public void LateUpdate()
        {
            checkActive();
        }

        private void checkActive()
        {
            checkGameEnding();
            checkBotActive();
            checkStandBy();
            checkLayersActive();
        }

        private void checkLayersActive()
        {
            SAINLayersActiveToggle.CheckToggle(ActiveLayer != ESAINLayer.None);
        }

        private void checkBotActive()
        {
            if (GameEnding && BotActive)
                SetActive(false);

            if (!GameEnding &&
                !BotActive &&
                Bot.Person.ActivationClass.BotActive)
            {
                Logger.LogWarning($"Bot not active but should be!");
                SetActive(true);
            }
        }

        private void checkStandBy()
        {
            bool standby = _botInStandby;
            if (standby && BotActive)
            {
                if (Bot.HasEnemy)
                {
                    //Logger.LogWarning($"Had to activate bot manually because they were in stand by.");
                    BotOwner.StandBy.Activate();
                    standby = false;
                }
                else if (checkAllEnemies())
                {
                    Logger.LogDebug($"[{BotOwner.name}] disabled standby due to enemies being near.");
                    BotOwner.StandBy.Activate();
                    standby = false;
                }
            }

            BotStandByToggle.CheckToggle(standby);
        }

        private bool checkAllEnemies()
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

        private void checkGameEnding()
        {
            var botGame = Singleton<IBotGame>.Instance;
            bool gameEnding = botGame == null || botGame.Status == GameStatus.Stopping;
            GameEndingToggle.CheckToggle(gameEnding);
        }

        private void checkSpeedReset()
        {
            if (SAINLayersActive)
            {
                if (_speedReset)
                    _speedReset = false;

                return;
            }

            if (!_speedReset)
            {
                _speedReset = true;
                BotOwner.SetTargetMoveSpeed(1f);
                BotOwner.Mover.SetPose(1f);
                Bot.Mover.SetTargetMoveSpeed(1f);
                Bot.Mover.SetTargetPose(1f);
            }
        }

        public SAINActivationClass(BotComponent botComponent) : base(botComponent)
        {
        }

        public void Init()
        {
            SetActive(true);
            Bot.Person.ActivationClass.OnBotActiveChanged += SetActive;
        }

        public void Dispose()
        {
        }

        private bool _botInStandby => BotOwner.StandBy.StandByType != BotStandByType.active;
        private bool _speedReset;
        private float _nextCheckEnemiesTime;
    }
}