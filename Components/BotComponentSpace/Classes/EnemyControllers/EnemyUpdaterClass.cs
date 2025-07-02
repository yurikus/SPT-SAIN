using EFT;
using System.Collections.Generic;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyUpdaterClass : BotBase
    {
        public EnemyUpdaterClass(BotComponent bot) : base(bot)
        {
            //TickInterval = 1f / 15f;
        }

        public override void Init()
        {
            Enemies = Bot.EnemyController.Enemies;
            base.Init();
        }

        public override void ManualUpdate()
        {
            base.ManualUpdate();
            if (Bot == null || Bot.EnemyController == null)
            {
                return;
            }

            if (SAINPlugin.ProfilingMode)
            {
                UnityEngine.Profiling.Profiler.BeginSample("Enemy Updater");
            }

            List<IPlayer> Allies = Bot.BotOwner.BotsGroup.Allies;
            foreach (var item in Enemies)
            {
                string profileId = item.Key;
                Enemy enemy = item.Value;
                if (enemy == null || !enemy.CheckValid())
                {
                    _invalidIdsToRemove.Add(profileId);
                    continue;
                }
                if (Allies.Contains(enemy.EnemyPlayer))
                {
                    if (SAINPlugin.DebugMode)
                        Logger.LogWarning($"{enemy.EnemyPlayer.name} is an ally of {Bot.Player.name} and will be removed from its enemies collection");

                    _allyIdsToRemove.Add(profileId);
                    continue;
                }
                enemy.ManualUpdate();
            }

            if (_invalidIdsToRemove.Count > 0)
            {
                foreach (var id in _invalidIdsToRemove)
                {
                    Bot.EnemyController.RemoveEnemy(id);
                }
                Logger.LogWarning($"Removed {_invalidIdsToRemove.Count} Invalid Enemies");
                _invalidIdsToRemove.Clear();
            }
            
            if (_allyIdsToRemove.Count > 0)
            {
                foreach (var id in _allyIdsToRemove)
                {
                    Bot.EnemyController.RemoveEnemy(id);
                }

                if (SAINPlugin.DebugMode)
                    Logger.LogWarning($"Removed {_allyIdsToRemove.Count} allies");

                _allyIdsToRemove.Clear();
            }

            if (SAINPlugin.ProfilingMode)
            {
                UnityEngine.Profiling.Profiler.EndSample();
            }
        }

        public void LateUpdate()
        {
            if (Bot == null || Bot.EnemyController == null || !Bot.BotActive)
            {
                return;
            }

            foreach (var item in Enemies)
            {
                Enemy enemy = item.Value;
                if (enemy == null || !enemy.CheckValid())
                {
                    _invalidIdsToRemove.Add(item.Key);
                }
            }

            if (_invalidIdsToRemove.Count > 0)
            {
                foreach (var id in _invalidIdsToRemove)
                {
                    Bot.EnemyController.RemoveEnemy(id);
                }
                Logger.LogWarning($"Removed {_invalidIdsToRemove.Count} Invalid Enemies");
                _invalidIdsToRemove.Clear();
            }
        }

        private Dictionary<string, Enemy> Enemies;
        private readonly List<string> _allyIdsToRemove = [];
        private readonly List<string> _invalidIdsToRemove = [];
    }
}