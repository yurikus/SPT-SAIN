using EFT;
using SAIN.Components;
using SAIN.Helpers;
using System.Collections.Generic;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class SAINEnemyController : BotComponentClassBase
    {
        public override void Init()
        {
            Events.OnEnemyAdded += EnemyAdded;
            Events.OnEnemyRemoved += EnemyRemoved;
            _listController.Init();
            base.Init();
        }

        public Dictionary<string, Enemy> Enemies => _listController.Enemies;
        public HashSet<Enemy> EnemiesArray => _listController.EnemiesArray;

        public EnemyList KnownEnemies { get; private set; } = new("Known Enemies");
        public EnemyList ActiveThreats { get; private set; } = new("Active Threats");
        public EnemyList EnemiesInLineOfSight { get; private set; } = new("Enemies In LoS");
        public EnemyList VisibleEnemies { get; private set; } = new("Visible Enemies");

        public EnemyControllerEvents Events { get; }

        public bool AtPeace => Events.OnPeaceChanged.Value && Events.OnPeaceChanged.TimeSinceTrue > 1f;
        public bool ActiveHumanEnemy => Events.ActiveHumanEnemyEvent.Value;
        public bool HumanEnemyInLineofSight => Events.HumanInLineOfSightEvent.Value;

        public SAINEnemyController(BotComponent sain) : base(sain)
        {
            TickRequirement = ESAINTickState.OnlyBotActive;
            _listController = new EnemyListController(this);
            Events = new EnemyControllerEvents(this);
        }

        public override void ManualUpdate()
        {
            UpdateEnemies(Bot);
            _listController.ManualUpdate();
            checkDiscrepency();
            base.ManualUpdate();
        }

        public void LateUpdate()
        {
            //if (Bot == null || Bot.EnemyController == null || !Bot.BotActive)
            //{
            //    return;
            //}
            //
            //foreach (var item in EnemiesDictionary)
            //{
            //    Enemy enemy = item.Value;
            //    if (enemy == null || !enemy.CheckValid())
            //    {
            //        _invalidIdsToRemove.Add(item.Key);
            //    }
            //}
            //
            //if (_invalidIdsToRemove.Count > 0)
            //{
            //    foreach (var id in _invalidIdsToRemove)
            //    {
            //        Bot.EnemyController.RemoveEnemy(id);
            //    }
            //    Logger.LogWarning($"Removed {_invalidIdsToRemove.Count} Invalid Enemies");
            //    _invalidIdsToRemove.Clear();
            //}
        }

        public override void Dispose()
        {
            _listController.Dispose();
            Events.OnEnemyAdded -= EnemyAdded;
            Events.OnEnemyRemoved -= EnemyRemoved;
            KnownEnemies.Clear();
            ActiveThreats.Clear();
            EnemiesInLineOfSight.Clear();
            VisibleEnemies.Clear();
            base.Dispose();
        }

        public Enemy GetEnemy(string profileID, bool mustBeActive) => _listController.GetEnemy(profileID, mustBeActive);

        public Enemy CheckAddEnemy(IPlayer IPlayer) => _listController.CheckAddEnemy(IPlayer);

        public void RemoveEnemy(string profileID) => _listController.RemoveEnemy(profileID);

        public bool IsPlayerAnEnemy(string profileID) => _listController.IsPlayerAnEnemy(profileID);

        public bool IsPlayerFriendly(IPlayer iPlayer) => _listController.IsPlayerFriendly(iPlayer);

        public bool IsBotInBotsGroup(BotOwner botOwner) => _listController.IsBotInBotsGroup(botOwner);

        private void EnemyAdded(Enemy enemy)
        {
            EnemyEvents events = enemy.Events;
            KnownEnemies.Subscribe(ref events.OnEnemyKnownChanged.OnToggle);
            ActiveThreats.Subscribe(ref events.OnActiveThreatChanged.OnToggle);
            EnemiesInLineOfSight.Subscribe(ref events.OnVisionChange.OnToggle);
            VisibleEnemies.Subscribe(ref events.OnEnemyLineOfSightChanged.OnToggle);
        }

        private void EnemyRemoved(string profileID, Enemy enemy)
        {
            EnemyEvents events = enemy.Events;
            KnownEnemies.Unsubscribe(ref events.OnEnemyKnownChanged.OnToggle, enemy);
            ActiveThreats.Unsubscribe(ref events.OnActiveThreatChanged.OnToggle, enemy);
            EnemiesInLineOfSight.Unsubscribe(ref events.OnVisionChange.OnToggle, enemy);
            VisibleEnemies.Unsubscribe(ref events.OnEnemyLineOfSightChanged.OnToggle, enemy);
            KnownEnemies.RemoveEnemy(enemy);
            ActiveThreats.RemoveEnemy(enemy);
            EnemiesInLineOfSight.RemoveEnemy(enemy);
            VisibleEnemies.RemoveEnemy(enemy);
            if (GoalEnemy != null && GoalEnemy == enemy)
            {
                GoalEnemy = null;
            }
            if (LastGoalEnemy != null && LastGoalEnemy == enemy)
            {
                LastGoalEnemy = null;
            }
        }

        public void UpdateEnemies(BotComponent bot)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Enemy Updater");
            List<IPlayer> Allies = bot.BotOwner?.BotsGroup?.Allies;
            if (Allies == null)
            {
                Logger.LogError($"[{bot.name}] No Allies List!");
                return;
            }
            foreach (Enemy Enemy in EnemiesArray)
            {
                if (!Enemy.CheckValid())
                {
                    _invalidIdsToRemove.Add(Enemy.EnemyProfileId);
                    continue;
                }
                if (Allies.Contains(Enemy.EnemyPlayer))
                {
                    if (SAINPlugin.DebugMode)
                        Logger.LogWarning($"{Enemy.EnemyPlayer.name} is an ally of {Bot.Player.name} and will be removed from its enemies collection");

                    _allyIdsToRemove.Add(Enemy.EnemyProfileId);
                    continue;
                }
                Enemy.ManualUpdate();
            }

            if (_invalidIdsToRemove.Count > 0)
            {
                foreach (var id in _invalidIdsToRemove)
                    RemoveEnemy(id);
                Logger.LogWarning($"Removed {_invalidIdsToRemove.Count} Invalid Enemies");
                _invalidIdsToRemove.Clear();
            }

            if (_allyIdsToRemove.Count > 0)
            {
                foreach (var id in _allyIdsToRemove)
                    RemoveEnemy(id);
                if (SAINPlugin.DebugMode)
                    Logger.LogWarning($"Removed {_allyIdsToRemove.Count} allies");
                _allyIdsToRemove.Clear();
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        public Enemy GoalEnemy {
            get
            {
                return _goalEnemy;
            }
            private set
            {
                if (value == _goalEnemy)
                {
                    return;
                }
                if (value != null)
                {
                    if (value.LastKnownPosition == null)
                    {
                        Logger.LogError("cant set enemy with null last known!");
                        return;
                    }
                    Bot.Cover.CoverFinder.CalcTargetPoint(value, value.LastKnownPosition.Value);
                }

                LastGoalEnemy = _goalEnemy;
                _goalEnemy = value;
                Events.EnemyChanged(value, LastGoalEnemy);
            }
        }

        public Enemy LastGoalEnemy { get; private set; }

        public Enemy ChooseEnemy()
        {
            if (KnownEnemies.Count == 0)
            {
                ClearEnemy();
            }
            else
            {
                GoalEnemy = SelectEnemy();
                setGoalEnemy(GoalEnemy?.EnemyInfo);
            }
            return GoalEnemy;
        }

        public void ClearEnemy()
        {
            GoalEnemy = null;
            setGoalEnemy(null);
        }

        private readonly List<Enemy> _preAllocList = [];

        private Enemy SelectEnemy()
        {
            const float CHANGE_ENEMY_DIST_RATIO_SHOOTER = 0.66f;
            const float CHANGE_ENEMY_DIST_RATIO_NON_SHOOTER = 0.33f;
            const float CHANGE_ENEMY_KNOWN_KNOWN_DIST_RATIO = 0.5f;
            const float CHANGE_ENEMY_KNOWN_SHOT_AT_ME_DIST_RATIO = 0.5f;
            const float MAX_DISTANCE_NON_ENGAGED_PRIORITIZE_DIST = 50f;

            if (Bot.Decision.DogFightDecision.CheckShallDogFight(KnownEnemies, out Enemy dogFightEnemy))
            {
                return dogFightEnemy;
            }

            if (VisibleEnemies.Count > 0)
            {
                return SelectVisibleEnemy(CHANGE_ENEMY_DIST_RATIO_SHOOTER, CHANGE_ENEMY_DIST_RATIO_NON_SHOOTER);
            }

            if (Bot.Medical.TimeSinceShot < 5f)
            {
                Enemy enemy = Bot.Medical.HitByEnemy.EnemyWhoLastShotMe;
                if (enemy != null && KnownEnemies.Contains(enemy))
                {
                    return enemy;
                }
            }

            if (BotOwner.Memory.IsUnderFire)
            {
                Enemy enemy = Bot.Memory.LastUnderFireEnemy;
                if (enemy != null && KnownEnemies.Contains(enemy))
                {
                    return enemy;
                }
            }

            if (_goalEnemy != null && _goalEnemy.Events.OnSearch.Value)
            {
                return _goalEnemy;
            }

            KnownEnemies.SortBy(EnemyList.EBotListSortType.ByLastKnownDistance);
            Enemy closestKnownEnemy = KnownEnemies[0];
            if (_goalEnemy == null) return closestKnownEnemy;
            if (_goalEnemy == closestKnownEnemy) return closestKnownEnemy;
            for (int i = 0; i < KnownEnemies.Count; i++)
            {
                Enemy knownEnemy = KnownEnemies[i];
                if (knownEnemy.KnownPlaces.BotDistanceFromLastKnown <= MAX_DISTANCE_NON_ENGAGED_PRIORITIZE_DIST) return knownEnemy;
                if (knownEnemy.KnownPlaces.EnemyDistanceFromLastKnown < CHANGE_ENEMY_KNOWN_KNOWN_DIST_RATIO * _goalEnemy.RealDistance) return closestKnownEnemy;
            }

            List<Enemy> enemiesWhoEngagedMe = _preAllocList;
            KnownEnemies.FilterByPredicateNonAlloc(enemiesWhoEngagedMe, x => x.Status.ShotAtMe || x.Status.ShotMe);
            if (enemiesWhoEngagedMe.Count > 0)
            {
                Enemy lastEngagedEnemy = enemiesWhoEngagedMe[0];
                if (_goalEnemy == null) return lastEngagedEnemy;
                if (_goalEnemy == lastEngagedEnemy) return lastEngagedEnemy;
                for (int i = 0; i < KnownEnemies.Count; i++)
                {
                    Enemy engagedEnemy = enemiesWhoEngagedMe[i];
                    if (engagedEnemy.KnownPlaces.EnemyDistanceFromLastKnown < CHANGE_ENEMY_KNOWN_SHOT_AT_ME_DIST_RATIO * _goalEnemy.RealDistance) return engagedEnemy;
                }
            }
            return closestKnownEnemy;
        }

        private Enemy SelectVisibleEnemy(float CHANGE_ENEMY_DIST_RATIO_SHOOTER, float CHANGE_ENEMY_DIST_RATIO_NON_SHOOTER)
        {
            if (VisibleEnemies.Count > 1) VisibleEnemies.SortBy(EnemyList.EBotListSortType.ByRealDistance);
            Enemy closestVisibleEnemy = VisibleEnemies[0];

            if (_goalEnemy == null) return closestVisibleEnemy;

            List<Enemy> visibleShooters = _preAllocList;
            VisibleEnemies.FilterByPredicateNonAlloc(visibleShooters, x => x.IsShooter());
            if (visibleShooters.Count > 0)
            {
                Enemy visibleShooter = visibleShooters[0];
                if (closestVisibleEnemy != visibleShooter &&
                    closestVisibleEnemy.RealDistance < CHANGE_ENEMY_DIST_RATIO_NON_SHOOTER * visibleShooter.RealDistance)
                {
                    return closestVisibleEnemy;
                }

                if (visibleShooter == _goalEnemy) return _goalEnemy;
                else if (visibleShooter.RealDistance < CHANGE_ENEMY_DIST_RATIO_SHOOTER * _goalEnemy.RealDistance)
                    return visibleShooter;
            }

            for (int i = 0; i < VisibleEnemies.Count; i++)
            {
                Enemy visibleEnemy = VisibleEnemies[i];
                if (visibleEnemy == _goalEnemy)
                {
                    return _goalEnemy;
                }
                else if (visibleEnemy.RealDistance < CHANGE_ENEMY_DIST_RATIO_SHOOTER * _goalEnemy.RealDistance)
                {
                    return visibleEnemy;
                }
                if (visibleEnemy == _goalEnemy) return _goalEnemy;
                else if (visibleEnemy.RealDistance < CHANGE_ENEMY_DIST_RATIO_SHOOTER * _goalEnemy.RealDistance)
                    return visibleEnemy;
            }
            return closestVisibleEnemy;
        }

        private void checkGoalEnemy(out Enemy enemy)
        {
            enemy = null;

            EnemyInfo eftEnemy = BotOwner.Memory.GoalEnemy;
            Enemy sainEnemy = GoalEnemy;

            if (eftEnemy?.Person != null && !eftEnemy.Person.HealthController.IsAlive)
            {
                try { BotOwner.Memory.GoalEnemy = null; }
                catch
                { // Sometimes bsg code throws an error here :D
                }
                eftEnemy = null;
            }

            // Bot has no goal enemy, set active enemy to null if they aren't already, and if they aren't currently visible or shot at me
            if (eftEnemy == null)
            {
                if (sainEnemy == null)
                {
                    return;
                }
                if (sainEnemy.CheckValid() &&
                    Enemy.IsEnemyActive(sainEnemy) &&
                    (sainEnemy.Status.ShotAtMeRecently || sainEnemy.IsVisible))
                {
                    enemy = sainEnemy;
                }
                return;
            }

            // if the bot's active enemy already matches goal enemy, do nothing
            if (sainEnemy != null &&
                sainEnemy.EnemyInfo.ProfileId == eftEnemy.ProfileId)
            {
                enemy = sainEnemy;
                return;
            }

            // our enemy is changing.
            sainEnemy = CheckAddEnemy(eftEnemy?.Person);

            if (sainEnemy == null)
            {
                Logger.LogError($"{eftEnemy?.Person?.ProfileId} not SAIN enemy!");
                return;
            }

            if (sainEnemy.CheckValid() && Enemy.IsEnemyActive(sainEnemy))
            {
                enemy = sainEnemy;
            }
            else
            {
                enemy = null;
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
                Bot.EnemyController.CheckAddEnemy(goalEnemy.Person);
                //}
            }
        }

        private Enemy _goalEnemy;

        private readonly List<string> _allyIdsToRemove = [];
        private readonly List<string> _invalidIdsToRemove = [];

        private readonly EnemyListController _listController;
    }
}