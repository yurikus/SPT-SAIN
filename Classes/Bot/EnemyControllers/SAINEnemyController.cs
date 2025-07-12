using EFT;
using SAIN.Components;
using SAIN.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class SAINEnemyController : BotComponentClassBase
    {
        public Dictionary<string, Enemy> Enemies => _listController.Enemies;
        public HashSet<Enemy> EnemiesArray => _listController.EnemiesArray;
        public EnemyControllerEvents Events { get; }
        public EnemyListsClass EnemyLists { get; }

        public Enemy GoalEnemy => _enemyChooser.GoalEnemy;
        public Enemy LastGoalEnemy => _enemyChooser.LastGoalEnemy;
        public bool AtPeace => Events.OnPeaceChanged.Value && Events.OnPeaceChanged.TimeSinceTrue > 1f;
        public bool ActiveHumanEnemy => Events.ActiveHumanEnemyEvent.Value;
        public bool HumanEnemyInLineofSight => Events.HumanInLineOfSightEvent.Value;

        public SAINEnemyController(BotComponent sain) : base(sain)
        {
            TickRequirement = ESAINTickState.OnlyBotActive;

            _listController = new EnemyListController(this);

            Events = new EnemyControllerEvents(this);
            AddSubClass(Events);
            EnemyLists = new EnemyListsClass(this);
            AddSubClass(EnemyLists);
            _enemyUpdater = new EnemyUpdaterClass(sain);
            AddSubClass(_enemyUpdater);
            _enemyChooser = new EnemyChooserClass(this);
            AddSubClass(_enemyChooser);
        }

        public override void Init()
        {
            _listController.Init();
            base.Init();
        }

        public override void ManualUpdate()
        {
            _listController.ManualUpdate();
            updateDebug();
            base.ManualUpdate();
        }

        public void LateUpdate()
        {
            _enemyUpdater.LateUpdate();
        }

        public override void Dispose()
        {
            // must be first, so all enemies are removed properly and their events are triggered!
            _listController.Dispose();
            base.Dispose();
        }

        private void updateDebug()
        {
            var enemy = GoalEnemy;
            if (enemy != null)
            {
                if (SAINPlugin.DebugMode && SAINPlugin.DrawDebugGizmos)
                {
                    if (enemy.KnownPlaces.LastHeardPosition != null)
                    {
                        if (debugLastHeardPosition == null)
                        {
                            debugLastHeardPosition = DebugGizmos.DrawLine(enemy.KnownPlaces.LastHeardPosition.Value, Bot.Position, Color.yellow, 0.01f, Time.deltaTime, true);
                        }
                        DebugGizmos.UpdateLinePosition(enemy.KnownPlaces.LastHeardPosition.Value, Bot.Position, debugLastHeardPosition);
                    }
                    if (enemy.KnownPlaces.LastSeenPosition != null)
                    {
                        if (debugLastSeenPosition == null)
                        {
                            debugLastSeenPosition = DebugGizmos.DrawLine(enemy.KnownPlaces.LastSeenPosition.Value, Bot.Position, Color.red, 0.01f, Time.deltaTime, true);
                        }
                        DebugGizmos.UpdateLinePosition(enemy.KnownPlaces.LastSeenPosition.Value, Bot.Position, debugLastSeenPosition);
                    }
                }
                else if (debugLastHeardPosition != null || debugLastSeenPosition != null)
                {
                    GameObject.Destroy(debugLastHeardPosition);
                    GameObject.Destroy(debugLastSeenPosition);
                }
            }
            else if (debugLastHeardPosition != null || debugLastSeenPosition != null)
            {
                GameObject.Destroy(debugLastHeardPosition);
                GameObject.Destroy(debugLastSeenPosition);
            }
        }

        public void ClearEnemy() => _enemyChooser.ClearEnemy();

        public Enemy GetEnemy(string profileID, bool mustBeActive) => _listController.GetEnemy(profileID, mustBeActive);

        public Enemy CheckAddEnemy(IPlayer IPlayer) => _listController.CheckAddEnemy(IPlayer);

        public void RemoveEnemy(string profileID) => _listController.RemoveEnemy(profileID);

        public bool IsPlayerAnEnemy(string profileID) => _listController.IsPlayerAnEnemy(profileID);

        public bool IsPlayerFriendly(IPlayer iPlayer) => _listController.IsPlayerFriendly(iPlayer);

        public bool IsBotInBotsGroup(BotOwner botOwner) => _listController.IsBotInBotsGroup(botOwner);

        private readonly EnemyListController _listController;
        private readonly EnemyChooserClass _enemyChooser;
        private readonly EnemyUpdaterClass _enemyUpdater;

        private GameObject debugLastSeenPosition;
        private GameObject debugLastHeardPosition;
    }
}