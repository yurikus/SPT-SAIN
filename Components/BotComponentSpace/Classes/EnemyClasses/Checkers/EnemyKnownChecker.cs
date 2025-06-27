using SAIN.SAINComponent.Classes.EnemyClasses;

namespace SAIN.Components.BotComponentSpace.Classes.EnemyClasses
{
    public class EnemyKnownChecker : EnemyBase, IBotClass
    {
        public EnemyKnownChecker(Enemy enemy) : base(enemy)
        {

        }

        public void Init()
        {
            Bot.BotActivation.BotActiveToggle.OnToggle += botStateChanged;
        }

        public void Update()
        {
            checkShallKnowEnemy();
        }

        public void Dispose()
        {
            Bot.BotActivation.BotActiveToggle.OnToggle -= botStateChanged;
        }

        private void checkShallKnowEnemy()
        {
            bool enemyKnown = shallKnowEnemy();
            setEnemyKnown(enemyKnown);
        }

        private void botStateChanged(bool botActive)
        {
            if (!botActive)
            {
                setEnemyKnown(false);
            }
        }

        private void setEnemyKnown(bool enemyKnown)
        {
            Enemy.Events.OnEnemyKnownChanged.CheckToggle(enemyKnown);
        }

        private bool shallKnowEnemy()
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
            if (!isBotSearching())
            {
                return false;
            }
            if (Enemy.Events.OnSearch.Value)
            {
                return !Enemy.KnownPlaces.SearchedAllKnownLocations;
            }
            return false;
        }

        private bool isBotSearching()
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
