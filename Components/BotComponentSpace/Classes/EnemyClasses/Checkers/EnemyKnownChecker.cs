using SAIN.SAINComponent.Classes.EnemyClasses;

namespace SAIN.Components.BotComponentSpace.Classes.EnemyClasses
{
    public class EnemyKnownChecker(Enemy enemy) : EnemyBase(enemy), IBotClass
    {
        public override void Init()
        {
            Bot.BotActivation.BotActiveToggle.OnToggle += BotStateChanged;
            base.Init();
        }

        public override void ManualUpdate()
        {
            bool enemyKnown = ShallKnowEnemy();
            SetEnemyKnown(enemyKnown);
            base.ManualUpdate();
        }

        public override void Dispose()
        {
            Bot.BotActivation.BotActiveToggle.OnToggle -= BotStateChanged;
            base.Dispose();
        }

        private void BotStateChanged(bool botActive)
        {
            if (!botActive)
            {
                SetEnemyKnown(false);
            }
        }

        public void SetEnemyKnown(bool enemyKnown)
        {
            Enemy.Events.OnEnemyKnownChanged.CheckToggle(enemyKnown);
        }

        private bool ShallKnowEnemy()
        {
            if (!Enemy.CheckValid())
            {
                //if (Enemy.EnemyKnown)
                //    Logger.LogDebug("enemy not valid");
                return false;
            }

            if (!EnemyPlayerComponent.IsActive)
            {
                //if (Enemy.EnemyKnown)
                //    Logger.LogDebug("enemy not active");
                return false;
            }

            if (Enemy.LastKnownPosition == null)
            {
                //if (Enemy.EnemyKnown)
                //    Logger.LogDebug("enemy null lastknown");
                return false;
            }

            float timeSinceUpdate = Enemy.KnownPlaces.TimeSinceLastKnownUpdated;

            //if (Enemy.EnemyKnown && Enemy.EnemyPlayer.IsYourPlayer)
            //    Logger.LogDebug($"timesince update [{timeSinceUpdate}]");

            if (timeSinceUpdate > LAST_KNOWN_TIME_UPDATE_UPPER_LIMIT)
            {
                //if (Enemy.EnemyKnown)
                //    Logger.LogDebug("enemy forgotten becuz update too long");

                return false;
            }

            if (timeSinceUpdate <= Bot.Info.ForgetEnemyTime)
                return true;

            if (BotIsSearchingForMe())
                return true;

            //if (Enemy.EnemyKnown)
            //    Logger.LogDebug($"enemy forgotten. timesinceupdate: {timeSinceUpdate} forgetenemytime: {Bot.Info.ForgetEnemyTime}");

            return false;
        }

        private const float LAST_KNOWN_TIME_UPDATE_UPPER_LIMIT = 400f;

        public bool BotIsSearchingForMe()
        {
            if (!IsBotSearching())
            {
                return false;
            }
            if (Enemy.Events.OnSearch.Value)
            {
                return !Enemy.KnownPlaces.SearchedAllKnownLocations;
            }
            return false;
        }

        private bool IsBotSearching()
        {
            if (Bot.Decision.CurrentCombatDecision == ECombatDecision.Search)
            {
                return true;
            }
            var squadDecision = Bot.Decision.CurrentSquadDecision;
            if (squadDecision == ESquadDecision.Search ||
                squadDecision == ESquadDecision.GroupSearch)
            {
                return true;
            }
            return false;
        }
    }
}