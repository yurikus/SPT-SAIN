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
        protected readonly List<PathVisionJob> VisionJobs = [];
        protected readonly List<PathVisionJob> ShootJobs = [];
        private float _nextVisionPathUpdateTime;
        private const float VisionPathUpdateInterval = 1f / 20f;
        private QueryParameters queryParams;

        public EnemyPathVisibilityRaycastJob(MonoBehaviour botcontroller) : base("Path Visibility Job", botcontroller, true, 1f / 60f)
        {
            LayerMask HighPolyWithTerrain = LayerMaskClass.HighPolyWithTerrainMask;
            LayerMask DoorLayer = LayerMaskClass.DoorLayer;
            LayerMask Mask = HighPolyWithTerrain & ~(1 << DoorLayer);
            queryParams = new(Mask, false, QueryTriggerInteraction.Ignore);
        }

        protected override IEnumerator PrimaryFunction()
        {
            HashSet<BotComponent> bots = SAINBotController.BotSpawnController.SAINBots;
            CalcEnemyPaths(bots);
            if (_nextVisionPathUpdateTime < Time.time)
            {
                _nextVisionPathUpdateTime = Time.time + VisionPathUpdateInterval;
                CreateJobs(bots);
                if (VisionJobs.Count > 0)
                {
                    yield return null;
                    foreach (var job in VisionJobs) job.Schedule(32);
                    yield return null;
                    ScheduleShootCommands();
                    if (ShootJobs.Count > 0)
                    {
                        yield return null;
                        foreach (var job in ShootJobs) job.Schedule(32);
                        yield return null;
                        ReadResults();
                    }
                }
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
                                if (cornerCount > 2 && enemy.Path.AllPathPoints.Count > 0)
                                {
                                    VisionJobs.Add(new(enemy.Path.AllPathPoints, eyePosition, enemy, queryParams));
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
                job.Complete();
                Enemy enemy = job.Enemy;
                if (enemy.EnemyKnown)
                {
                    var visiblePoints = enemy.Path.VisiblePathPoints;
                    var allPoints = enemy.Path.AllPathPoints;
                    var Hits = job.Hits;
                    //int lastVisibleIndex = 0;
                    visiblePoints.Clear();
                    for (int j = 0; j < allPoints.Count; j++)
                    {
                        if (Hits[j].collider == null)
                        {
                            visiblePoints.Add(allPoints[j]);
                            //lastVisibleIndex = i;
                        }
                    }
                    int visiblePointsCount = visiblePoints.Count;
                    if (visiblePointsCount > 0)
                    {
                        ShootJobs.Add(new(visiblePoints, enemy.Bot.Transform.WeaponRoot, enemy, queryParams));
                        continue;
                    }
                    enemy.ClearVisiblePathPoint();
                }
            }
            foreach (var job in VisionJobs) job.Dispose();
            VisionJobs.Clear();
        }

        private void ReadResults()
        {
            for (int i = 0; i < ShootJobs.Count; i++)
            {
                PathVisionJob job = ShootJobs[i];
                job.Complete();

                NativeArray<RaycastHit> hits = job.Hits;
                Enemy enemy = job.Enemy;
                if (enemy.EnemyKnown)
                {
                    List<Vector3> visiblePoints = enemy.Path.VisiblePathPoints;
                    bool PointFound = false;
                    for (int j = visiblePoints.Count - 1; j >= 0; j--)
                    {
                        if (hits[j].collider == null)
                        {
                            enemy.SetLastVisiblePathPoint(visiblePoints[j], j);
                            PointFound = true;
                            break;
                        }
                    }
                    if (!PointFound) enemy.ClearVisiblePathPoint();
                }
            }
            foreach (var job in ShootJobs) job.Dispose();
            ShootJobs.Clear();
        }

        private static void CalcEnemyPaths(HashSet<BotComponent> bots)
        {
            foreach (BotComponent bot in bots)
            {
                if (bot != null && bot.BotActive)
                {
                    foreach (Enemy enemy in bot.EnemyController.EnemiesArray)
                    {
                        if (enemy.EnemyKnown)
                        {
                            enemy.Path.CheckCalcPath();
                        }
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