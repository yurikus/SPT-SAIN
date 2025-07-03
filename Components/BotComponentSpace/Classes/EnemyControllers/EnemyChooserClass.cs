using EFT;
using SAIN.Helpers;
using SAIN.Models.Enums;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyChooserClass : BotSubClass<SAINEnemyController>, IBotClass
    {
        public Enemy GoalEnemy {
            get
            {
                return _activeEnemy;
            }
            private set
            {
                if (value == _activeEnemy)
                {
                    return;
                }

                LastGoalEnemy = _activeEnemy;
                _activeEnemy = value;
                BaseClass.Events.EnemyChanged(value, LastGoalEnemy);
            }
        }

        public Enemy LastGoalEnemy { get; private set; }

        public EnemyChooserClass(SAINEnemyController controller) : base(controller)
        {
        }

        public override void Init()
        {
            BaseClass.Events.OnEnemyRemoved += enemyRemoved;
            BaseClass.Events.OnEnemyKnownChanged += enemyKnownChanged;
            base.Init();
        }

        public override void ManualUpdate()
        {
            assignActiveEnemy();
            checkDiscrepency();
            base.ManualUpdate();
        }

        public override void Dispose()
        {
            BaseClass.Events.OnEnemyRemoved -= enemyRemoved;
            BaseClass.Events.OnEnemyKnownChanged -= enemyKnownChanged;
            base.Dispose();
        }

        private void enemyKnownChanged(bool known, Enemy enemy)
        {
            if (!known &&
                _activeEnemy != null &&
                _activeEnemy.EnemyProfileId == enemy.EnemyProfileId)
            {
                setActiveEnemy(null);
            }
        }

        private void enemyRemoved(string profileId, Enemy enemy)
        {
            if (GoalEnemy != null &&
                GoalEnemy.EnemyProfileId == profileId)
            {
                GoalEnemy = null;
                LastGoalEnemy = null;
                return;
            }
            if (LastGoalEnemy != null &&
                LastGoalEnemy.EnemyProfileId == profileId)
            {
                LastGoalEnemy = null;
            }
        }

        public void ClearEnemy()
        {
            setActiveEnemy(null);
        }

        private void assignActiveEnemy()
        {
            Enemy activeEnemy = findActiveEnemy();
            if (activeEnemy != null &&
                (!activeEnemy.CheckValid() || !activeEnemy.EnemyPerson.Active))
            {
                Logger.LogWarning($"Tried to assign inactive or invalid player.");
                activeEnemy = null;
            }
            if (activeEnemy == null)
            {
                foreach (var enemy in Bot.EnemyController.Enemies.Values)
                {
                    if (enemy?.EnemyKnown == true)
                    {
                        Logger.LogWarning("enemy known but no enemy");
                    }
                }
            }
            setActiveEnemy(activeEnemy);
            if (activeEnemy != null && BotOwner.Memory.IsPeace)
            {
                Logger.LogWarning("has enemy but peace!?");
            }
        }

        private Enemy findActiveEnemy()
        {
            Enemy dogFightTarget = Bot.Decision.DogFightDecision.DogFightTarget;
            if (dogFightTarget?.CheckValid() == true && dogFightTarget.EnemyPerson.Active)
            {
                return dogFightTarget;
            }

            var targetEnemy = Bot.CurrentTarget.CurrentTargetEnemy;
            if (targetEnemy != null &&
                (GoalEnemy == null || targetEnemy.IsDifferent(GoalEnemy)))
            {
                return targetEnemy;
            }

            checkGoalEnemy(out Enemy goalEnemy);

            if (goalEnemy != null)
            {
                if (!goalEnemy.IsVisible)
                {
                    Enemy visibleEnemy = BaseClass.EnemyLists.First(EEnemyListType.Visible);
                    if (visibleEnemy?.CheckValid() == true &&
                        visibleEnemy.EnemyPerson.Active)
                    {
                        return visibleEnemy;
                    }
                }
                return goalEnemy;
            }

            var enemy = BaseClass.EnemyLists.KnownEnemies.First();
            return enemy;
        }

        private void checkGoalEnemy(out Enemy enemy)
        {
            enemy = null;

            EnemyInfo goalEnemy = BotOwner.Memory.GoalEnemy;
            Enemy activeEnemy = GoalEnemy;

            // make sure the bot's goal enemy isn't dead
            if (goalEnemy?.Person != null &&
                goalEnemy.Person.HealthController.IsAlive == false)
            {
                try { BotOwner.Memory.GoalEnemy = null; }
                catch
                { // Sometimes bsg code throws an error here :D
                }
                goalEnemy = null;
            }

            // Bot has no goal enemy, set active enemy to null if they aren't already, and if they aren't currently visible or shot at me
            if (goalEnemy == null)
            {
                if (activeEnemy == null)
                {
                    return;
                }
                if (activeEnemy.CheckValid() &&
                    activeEnemy.EnemyPerson.Active &&
                    (activeEnemy.Status.ShotAtMeRecently || activeEnemy.IsVisible))
                {
                    enemy = activeEnemy;
                }
                return;
            }

            // if the bot's active enemy already matches goal enemy, do nothing
            if (activeEnemy != null &&
                activeEnemy.EnemyInfo.ProfileId == goalEnemy.ProfileId)
            {
                enemy = activeEnemy;
                return;
            }

            // our enemy is changing.
            activeEnemy = BaseClass.CheckAddEnemy(goalEnemy?.Person);

            if (activeEnemy == null)
            {
                Logger.LogError($"{goalEnemy?.Person?.ProfileId} not SAIN enemy!");
                return;
            }

            if (activeEnemy.CheckValid() && activeEnemy.EnemyPerson.Active)
            {
                enemy = activeEnemy;
            }
            else
            {
                enemy = null;
            }
        }

        private void setActiveEnemy(Enemy enemy)
        {
            if (enemy == null || (enemy.CheckValid() && enemy.EnemyPerson.Active))
            {
                GoalEnemy = enemy;
                setGoalEnemy(enemy?.EnemyInfo);
            }
        }

        private void setLastEnemy(Enemy activeEnemy)
        {
            bool nullActiveEnemy = activeEnemy?.EnemyPerson?.Active == true;
            bool nullLastEnemy = LastGoalEnemy?.EnemyPerson?.Active == true;

            if (!nullLastEnemy && nullActiveEnemy)
            {
                return;
            }
            if (nullLastEnemy && !nullActiveEnemy)
            {
                LastGoalEnemy = activeEnemy;
                return;
            }
            if (!AreEnemiesSame(activeEnemy, LastGoalEnemy))
            {
                LastGoalEnemy = activeEnemy;
                return;
            }
        }

        private void setGoalEnemy(EnemyInfo enemyInfo)
        {
            if (BotOwner.Memory.GoalEnemy != enemyInfo)
            {
                try
                {
                    BotOwner.Memory.GoalEnemy = enemyInfo;
                    BotOwner.CalcGoal();
                }
                catch
                {
                    // Sometimes bsg code throws an error here :D
                }
            }
        }

        public bool AreEnemiesSame(Enemy a, Enemy b)
        {
            return AreEnemiesSame(a?.EnemyIPlayer, b?.EnemyIPlayer);
        }

        public bool AreEnemiesSame(IPlayer a, IPlayer b)
        {
            return a != null
                && b != null
                && a.ProfileId == b.ProfileId;
        }

        private void checkDiscrepency()
        {
            EnemyInfo goalEnemy = BotOwner.Memory.GoalEnemy;
            if (goalEnemy != null && GoalEnemy == null)
            {
                //if (_nextLogTime < Time.time)
                //{
                //_nextLogTime = Time.time + 1f;

                //Logger.LogError("Bot's Goal Enemy is not null, but SAIN enemy is null.");
                if (goalEnemy.Person == null)
                {
                    //Logger.LogError("Bot's Goal Enemy Person is null");
                    return;
                }
                if (goalEnemy.ProfileId == Bot.ProfileId)
                {
                    //Logger.LogError("goalEnemy.ProfileId == SAINBot.ProfileId");
                    return;
                }
                if (goalEnemy.ProfileId == Bot.Player.ProfileId)
                {
                    //Logger.LogError("goalEnemy.ProfileId == SAINBot.Player.ProfileId");
                    return;
                }
                if (goalEnemy.ProfileId == Bot.BotOwner.ProfileId)
                {
                    //Logger.LogError("goalEnemy.ProfileId == SAINBot.Player.ProfileId");
                    return;
                }
                Enemy sainEnemy = BaseClass.GetEnemy(goalEnemy.ProfileId, true);
                if (sainEnemy != null)
                {
                    setActiveEnemy(sainEnemy);
                    //Logger.LogError("Got SAINEnemy from goalEnemy.ProfileId");
                    return;
                }
                sainEnemy = BaseClass.CheckAddEnemy(goalEnemy.Person);
                if (sainEnemy != null)
                {
                    setActiveEnemy(sainEnemy);
                    //Logger.LogError("Got SAINEnemy from goalEnemy.Person");
                    return;
                }
                //}
            }
        }

        private Enemy _activeEnemy;
    }
}