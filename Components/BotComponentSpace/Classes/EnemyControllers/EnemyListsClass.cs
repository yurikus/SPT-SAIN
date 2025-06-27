using SAIN.Helpers;
using SAIN.Models.Enums;
using System.Collections.Generic;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyListsClass : BotSubClass<SAINEnemyController>, IBotClass
    {
        public EnemyList KnownEnemies { get; private set; }

        public readonly Dictionary<EEnemyListType, EnemyList> EnemyLists = new();

        public EnemyListsClass(SAINEnemyController controller) : base(controller)
        {
            createLists();
        }

        private void createLists()
        {
            foreach (var type in _types)
                EnemyLists.Add(type, new EnemyList(type.ToString()));

            KnownEnemies = GetEnemyList(EEnemyListType.Known);
        }

        public EnemyList GetEnemyList(EEnemyListType type)
        {
            EnemyLists.TryGetValue(type, out EnemyList list);
            return list;
        }

        public Enemy First(EEnemyListType type)
        {
            return GetEnemyList(type).First();
        }

        public int HumanCount(EEnemyListType type)
        {
            return GetEnemyList(type).Humans;
        }

        public int TotalCount(EEnemyListType type)
        {
            return GetEnemyList(type).Count;
        }

        public int BotCount(EEnemyListType type)
        {
            return GetEnemyList(type).Bots;
        }

        public void Init()
        {
            Bot.EnemyController.Events.OnEnemyAdded += enemyAdded;
            Bot.EnemyController.Events.OnEnemyRemoved += enemyRemoved;
        }

        private void enemyAdded(Enemy enemy)
        {
            subOrUnSub(true, enemy);
        }

        private void enemyRemoved(string profileID, Enemy enemy)
        {
            subOrUnSub(false, enemy);
            foreach (var list in EnemyLists.Values)
                list.RemoveEnemy(enemy);
        }

        public void Update()
        {
        }

        public void Dispose()
        {
            var controller = Bot.EnemyController;
            if (controller != null)
            {
                controller.Events.OnEnemyAdded -= enemyAdded;
                controller.Events.OnEnemyRemoved -= enemyRemoved;
            }
            clearLists();
        }

        private void clearLists()
        {
            foreach (var list in EnemyLists.Values)
            {
                if (list.Count > 0)
                {
                    Logger.LogWarning($"List [{list.Name}] still has [{list.Count}] enemies contained! This shouldn't be the case Solarint, you fuck!");
                    foreach (var item in list)
                    {
                        if (item != null && !_enemiesToRemove.Contains(item))
                            _enemiesToRemove.Add(item);
                    }
                }
            }

            if (_enemiesToRemove.Count > 0)
            {
                Logger.LogWarning($"Had to manually remove [{_enemiesToRemove.Count}] enemies...");
                foreach (var item in _enemiesToRemove)
                {
                    enemyRemoved(item.EnemyProfileId, item);
                }
            }

            foreach (var list in EnemyLists.Values)
                list.Clear();
            EnemyLists.Clear();
        }

        private readonly List<Enemy> _enemiesToRemove = new();

        private void subOrUnSub(bool value, Enemy enemy)
        {
            var events = enemy.Events;

            GetEnemyList(EEnemyListType.Known)
                .SubOrUnSub(value, ref events.OnEnemyKnownChanged.OnToggle, enemy);

            GetEnemyList(EEnemyListType.ActiveThreats)
                .SubOrUnSub(value, ref events.OnActiveThreatChanged.OnToggle, enemy);

            GetEnemyList(EEnemyListType.Visible)
                .SubOrUnSub(value, ref events.OnVisionChange.OnToggle, enemy);

            GetEnemyList(EEnemyListType.InLineOfSight)
                .SubOrUnSub(value, ref events.OnEnemyLineOfSightChanged.OnToggle, enemy);

            /*
            EnemyList list;
            switch (value)
            {
                case true:

                    list = GetEnemyList(EEnemyListType.Known);
                    GetEnemyList(EEnemyListType.Known).SubOrUnSub(value, enemy.Events.OnEnemyKnownChanged.OnToggle);
                    enemy.Events.OnEnemyKnownChanged.OnToggle += list.AddOrRemoveEnemy;

                    list = GetEnemyList(EEnemyListType.ActiveThreats);
                    enemy.Events.OnActiveThreatChanged.OnToggle += list.AddOrRemoveEnemy;

                    list = GetEnemyList(EEnemyListType.Visible);
                    enemy.Events.OnVisionChange.OnToggle += list.AddOrRemoveEnemy;

                    list = GetEnemyList(EEnemyListType.InLineOfSight);
                    enemy.Events.OnEnemyLineOfSightChanged.OnToggle += list.AddOrRemoveEnemy;

                    break;

                case false:

                    list = GetEnemyList(EEnemyListType.Known);
                    enemy.Events.OnEnemyKnownChanged.OnToggle -= list.AddOrRemoveEnemy;

                    list = GetEnemyList(EEnemyListType.ActiveThreats);
                    enemy.Events.OnActiveThreatChanged.OnToggle -= list.AddOrRemoveEnemy;

                    list = GetEnemyList(EEnemyListType.Visible);
                    enemy.Events.OnVisionChange.OnToggle -= list.AddOrRemoveEnemy;

                    list = GetEnemyList(EEnemyListType.InLineOfSight);
                    enemy.Events.OnEnemyLineOfSightChanged.OnToggle -= list.AddOrRemoveEnemy;

                    break;
            }
            */
        }

        private static readonly EEnemyListType[] _types = EnumValues.GetEnum<EEnemyListType>();
    }
}