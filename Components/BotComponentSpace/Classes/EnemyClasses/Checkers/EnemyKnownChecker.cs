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
                return false;

            if (!EnemyPlayerComponent.IsActive)
                return false;

            if (Enemy.LastKnownPosition == null)
                return false;

            float timeSinceUpdate = Enemy.KnownPlaces.TimeSinceLastKnownUpdated;

            if (timeSinceUpdate > LAST_KNOWN_TIME_UPDATE_UPPER_LIMIT)
                return false;

            if (timeSinceUpdate <= Bot.Info.ForgetEnemyTime)
                return true;

            if (BotIsSearchingForMe())
                return true;

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
