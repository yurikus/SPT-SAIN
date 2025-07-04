using SAIN.Helpers;
using SAIN.SAINComponent;
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
    public class EnemyPathVisibilityRaycastJob : SainJobTemplate, IDisposable
    {
        public EnemyPathVisibilityRaycastJob(MonoBehaviour botcontroller) : base("Path Visibility Job", botcontroller, true, 1f / 10f)
        {
            Start();
        }

        protected readonly List<NavMeshPathRaycastJob> RaycastJobs = [];
        protected readonly List<RaycastJob> Jobs = [];

        protected override IEnumerator PrimaryFunction()
        {
            CreateJobs();
            int Total = Jobs.Count;
            if (Total > 0)
            {
                //ScheduleJobs(Total);
                yield return null;
                ReadResults(Total);
                Dispose();
                // Logger.LogDebug($"Completed {Total}");
            }
            //int Total = RaycastJobs.Count;
            //if (Total > 0)
            //{
            //    ScheduleJobs(Total);
            //    yield return AwaitCompletion(Total);
            //    ReadResults(Total);
            //    Dispose();
            //    // Logger.LogDebug($"Completed {Total}");
            //}
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
                NativeArray<RaycastCommand> Commands = Job.Commands;
                List<Vector3> Points = Job.Points;

                //if (Points == null)
                //{
                //    Logger.LogWarning($"null points");
                //    continue;
                //}
                //if (Points.Count != Hits.Length || Points.Count != Commands.Length || Commands.Length != Hits.Length)
                //{
                //    Logger.LogWarning($"P:[{Points.Count}] H:[{Hits.Length}] C:[{Commands.Length}]");
                //}

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

        private void CreateJobs()
        {
            LayerMask HighPolyWithTerrain = LayerMaskClass.HighPolyWithTerrainMask;
            LayerMask DoorLayer = LayerMaskClass.DoorLayer;
            LayerMask Mask = HighPolyWithTerrain & ~(1 << DoorLayer);
            foreach (BotComponent bot in SAINBotController.BotSpawnController.SAINBots)
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
                                    job.Schedule();
                                    Jobs.Add(job);
                                    //RaycastJobs.Add(NavMeshPathRaycastJob.Create([.. enemy.Path.VisionPathCheckPoints], 5, Mask, bot.Player, enemy.EnemyPlayer));
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