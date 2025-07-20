using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.Types.Jobs;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Components
{
    public class EnemyPathVisibilityRaycastJob(MonoBehaviour botcontroller) : SainJobTemplate("Path Visibility Job", botcontroller, true, 1f / 60f), IDisposable
    {
        protected readonly List<RaycastJob> Jobs = [];

        private float _nextVisionPathUpdateTime;
        private const float VisionPathUpdateInterval = 0.1f;

        protected override IEnumerator PrimaryFunction()
        {
            HashSet<BotComponent> bots = SAINBotController.BotSpawnController.SAINBots;
            CalcEnemyPaths(bots);
            if (_nextVisionPathUpdateTime < Time.time)
            {
                yield return null;
                _nextVisionPathUpdateTime = Time.time + VisionPathUpdateInterval;
                CreateJobs(bots);
                int Total = Jobs.Count;
                if (Total > 0)
                {
                    yield return null;
                    ReadResults(Total);
                    Dispose();
                }
            }
            yield return null;
        }
        
        private void CreateJobs(HashSet<BotComponent> bots)
        {
            LayerMask HighPolyWithTerrain = LayerMaskClass.HighPolyWithTerrainMask;
            LayerMask DoorLayer = LayerMaskClass.DoorLayer;
            LayerMask Mask = HighPolyWithTerrain & ~(1 << DoorLayer);
            foreach (BotComponent bot in bots)
            {
                if (bot != null && bot.BotActive)
                {
                    foreach (Enemy enemy in bot.EnemyController.EnemiesArray)
                    {
                        if (enemy == null) continue;
                        if (enemy.EnemyKnown)
                        {
                            Vector3[] corners = enemy.Path.PathCorners;
                            if (corners != null)
                            {
                                int cornerCount = corners.Length;
                                if (cornerCount > 2 && enemy.Path.PathToEnemyStatus != NavMeshPathStatus.PathInvalid && enemy.Path.VisionPathPoints.Count > 0)
                                {
                                    enemy.Path.VisionPathPoints_Cache.AddRange(enemy.Path.VisionPathPoints);
                                    RaycastJob job = new(enemy.Path.VisionPathPoints_Cache, bot.Transform.EyePosition, Mask, bot.Player, enemy.EnemyPlayer);
                                    job.Schedule(64);
                                    Jobs.Add(job);
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

        private void ScheduleJobs(int Total)
        {
            //for (int i = 0; i < Total; i++)
            //    RaycastJobs[i].RaycastJob.Schedule();
            for (int i = 0; i < Total; i++)
                Jobs[i].Schedule();
        }
        
        private void ReadResults(int Total)
        {
            for (int i = 0; i < Total; i++)
            {
                RaycastJob Job = Jobs[i];
                Job.Complete();
                NativeArray<RaycastHit> Hits = Job.Hits;
                List<Vector3> Points = Job.Points;

                if (SAINEnableClass.GetSAIN(Job.Owner?.AIData?.BotOwner, out BotComponent Bot))
                {
                    Enemy Enemy = Bot.EnemyController.GetEnemy(Job.Target?.ProfileId, false);
                    if (Enemy != null)
                    {
                        bool PointFound = false;
                        for (int j = Hits.Length - 1; j >= 0; j--)
                        {
                            if (Hits[j].collider == null)
                            {
                                Enemy.SetLastVisiblePathPoint(Points[j], j);
                                PointFound = true;
                                break;
                            }
                        }
                        if (!PointFound)
                        {
                            Enemy.ClearVisiblePathPoint();
                        }
                    }
                }
                Points.Clear();
            }
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
            var bots = SAINBotController?.BotSpawnController?.BotDictionary;
            return bots != null && bots.Count > 0;
        }

        protected override bool LoopCondition()
        {
            return SAINGameWorld != null;
        }

        public void Dispose()
        {
            foreach (RaycastJob Job in Jobs)
            {
                Job.Dispose();
            }
            Jobs.Clear();
        }
    }
}