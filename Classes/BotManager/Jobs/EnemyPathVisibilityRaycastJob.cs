using SAIN.Plugin;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.Types.Jobs;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace SAIN.Components
{
    public class EnemyPathVisibilityRaycastJob : SainJobTemplate, IDisposable
    {
        static EnemyPathVisibilityRaycastJob()
        {
            PresetHandler.OnPresetUpdated += updateSettings;
            updateSettings(PresetHandler.LoadedPreset);
        }

        private static void updateSettings(SAINPresetClass preset)
        {
            _commandsPerJob = Mathf.RoundToInt(preset.GlobalSettings.Steering.PathVisionMinCommandsPerJob);
        }

        protected readonly List<PathVisionJob> VisionJobs = [];
        protected readonly List<PathVisionJob> ShootJobs = [];
        private float _nextVisionPathUpdateTime;
        private const float VisionPathUpdateInterval = 1f / 20f;
        private QueryParameters queryParams;

        private static int _commandsPerJob = 256;

        public EnemyPathVisibilityRaycastJob(MonoBehaviour botcontroller) : base("Path Visibility Job", botcontroller, true, 1f / 60f)
        {
            LayerMask HighPolyWithTerrain = LayerMaskClass.HighPolyWithTerrainMask;
            LayerMask DoorLayer = LayerMaskClass.DoorLayer;
            LayerMask Mask = HighPolyWithTerrain & ~(1 << DoorLayer);
            queryParams = new(Mask, false, QueryTriggerInteraction.Ignore);
        }

        protected override IEnumerator PrimaryFunction()
        {
            HashSet<BotComponent> bots = SAINBotController.BotSpawnController.BotGroup1;
            CalcEnemyPaths(bots);
            CreateJobs(bots);
            if (VisionJobs.Count > 0)
            {
                ScheduleJobs(VisionJobs, _commandsPerJob);
                yield return null;
                ScheduleShootCommands();
                if (ShootJobs.Count > 0)
                {
                    ScheduleJobs(ShootJobs, _commandsPerJob);
                    yield return null;
                    ReadResults();
                }
            }
            
            yield return null;

            bots = SAINBotController.BotSpawnController.BotGroup2;
            CalcEnemyPaths(bots);
            CreateJobs(bots);
            if (VisionJobs.Count > 0)
            {
                ScheduleJobs(VisionJobs, _commandsPerJob);
                yield return null;
                ScheduleShootCommands();
                if (ShootJobs.Count > 0)
                {
                    ScheduleJobs(ShootJobs, _commandsPerJob);
                    yield return null;
                    ReadResults();
                }
            }
        }

        private static void ScheduleJobs(List<PathVisionJob> jobs, int minCommandsPerJob = 256)
        {
            for (int i = 0; i < jobs.Count; i++)
            {
                PathVisionJob job = jobs[i];
                job.Handle = RaycastCommand.ScheduleBatch(job.Commands, job.Hits, minCommandsPerJob);
                jobs[i] = job;
            }
        }

        private void CreateJobs(HashSet<BotComponent> bots)
        {
            foreach (BotComponent bot in bots)
            {
                if (bot != null && bot.BotActive)
                {
                    EnemyList knownEnemies = bot.EnemyController.KnownEnemies;
                    if (knownEnemies.Count > 0)
                    {
                        Vector3 eyePosition = bot.Transform.EyePosition;
                        foreach (Enemy enemy in bot.EnemyController.KnownEnemies)
                        {
                            Vector3[] corners = enemy.Path.PathCorners;
                            if (corners != null)
                            {
                                int cornerCount = corners.Length;
                                int nodeCount = enemy.Path.AllPathNodeCount;
                                if (cornerCount > 2 && nodeCount > 0)
                                {
                                    VisionJobs.Add(new(enemy.Path.AllPathNodes, nodeCount, eyePosition, enemy, queryParams));
                                    continue;
                                }
                                else if (cornerCount == 2)
                                {
                                    enemy.SetLastCornerAsVisiblePathPoint(corners[1], 1);
                                    continue;
                                }
                            }
                            enemy.ClearVisiblePathPoint();
                        }
                    }
                }
            }
        }

        private void ScheduleShootCommands()
        {
            for (int i = 0; i < VisionJobs.Count; i++)
            {
                PathVisionJob job = VisionJobs[i];
                if (!job.Handle.IsCompleted)
                {
                    Logger.LogWarning("completing job!");
                    job.Handle.Complete();
                }
                Enemy enemy = job.Enemy;
                if (enemy.EnemyKnown)
                {
                    var Hits = job.Hits;
                    //int lastVisibleIndex = 0;
                    var nodes = enemy.Path.AllPathNodes;
                    for (int j = 0; j < Hits.Length; j++)
                    {
                        BotVisiblePathNode node = nodes[j];
                        node.State = Hits[j].collider == null ? VisiblePathNodeState.Visible : VisiblePathNodeState.NotVisible;
                        nodes[j] = node;
                    }
                    ShootJobs.Add(new(nodes, enemy.Path.AllPathNodeCount, enemy.Bot.Transform.WeaponRoot, enemy, queryParams));
                }
                job.Hits.Dispose();
                job.Commands.Dispose();
            }
            VisionJobs.Clear();
        }

        private void ReadResults()
        {
            for (int i = 0; i < ShootJobs.Count; i++)
            {
                PathVisionJob job = ShootJobs[i];
                if (!job.Handle.IsCompleted)
                {
                    Logger.LogWarning("completing job!");
                    job.Handle.Complete();
                }

                NativeArray<RaycastHit> hits = job.Hits;
                Enemy enemy = job.Enemy;
                if (enemy.EnemyKnown)
                {
                    var nodes = enemy.Path.AllPathNodes;
                    bool PointFound = false;
                    for (int j = hits.Length - 1; j >= 0; j--)
                    {
                        BotVisiblePathNode node = nodes[j];
                        if (node.State == VisiblePathNodeState.Visible &&
                            hits[j].collider == null)
                        {
                            enemy.SetLastVisiblePathPoint(node);
                            PointFound = true;
                            break;
                        }
                    }
                    if (!PointFound) enemy.ClearVisiblePathPoint();
                }
                job.Hits.Dispose();
                job.Commands.Dispose();
            }
            ShootJobs.Clear();
        }

        private static void CalcEnemyPaths(HashSet<BotComponent> bots)
        {
            PathVisibilityConfig config = new(GlobalSettingsClass.Instance.Steering);
            foreach (BotComponent bot in bots)
            {
                if (bot != null && bot.BotActive)
                {
                    foreach (Enemy enemy in bot.EnemyController.KnownEnemies)
                    {
                        enemy.Path.CheckCalcPath(config);
                    }
                }
            }
        }

        protected override bool CanProceed()
        {
            var bots = SAINBotController?.BotSpawnController?.SAINBots;
            return bots != null && bots.Count > 0;
        }

        protected override bool LoopCondition()
        {
            return SAINGameWorld != null;
        }

        public override void Stop()
        {
            Dispose();
            base.Stop();
        }

        public void Dispose()
        {
            foreach (var Job in VisionJobs) Job.Dispose();
            VisionJobs.Clear();
            foreach (var job in ShootJobs) job.Dispose();
            ShootJobs.Clear();
        }
    }
}