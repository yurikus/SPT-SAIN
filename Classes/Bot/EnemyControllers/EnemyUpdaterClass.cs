using EFT;
using SAIN.Components;
using System.Collections.Generic;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyUpdaterClass : BotBase
    {
        public EnemyUpdaterClass(BotComponent bot) : base(bot)
        {
            //TickInterval = 1f / 15f;
        }

        public override void ManualUpdate()
        {
            base.ManualUpdate();
            UpdateEnemies(Bot);
        }

        public void UpdateEnemies(BotComponent bot)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Enemy Updater");
            if (bot == null)
                return;
            var EnemyController = bot.EnemyController;
            if (EnemyController == null) 
                return;
            HashSet<Enemy> enemies = EnemyController.EnemiesArray;
            if (enemies == null)
                return;
            List<IPlayer> Allies = bot.BotOwner.BotsGroup.Allies;
            foreach (Enemy Enemy in enemies)
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
                    EnemyController.RemoveEnemy(id);
                Logger.LogWarning($"Removed {_invalidIdsToRemove.Count} Invalid Enemies");
                _invalidIdsToRemove.Clear();
            }

            if (_allyIdsToRemove.Count > 0)
            {
                foreach (var id in _allyIdsToRemove)
                    EnemyController.RemoveEnemy(id);
                if (SAINPlugin.DebugMode)
                    Logger.LogWarning($"Removed {_allyIdsToRemove.Count} allies");
                _allyIdsToRemove.Clear();
            }

            UnityEngine.Profiling.Profiler.EndSample();
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

        private readonly List<string> _allyIdsToRemove = [];
        private readonly List<string> _invalidIdsToRemove = [];
    }
}