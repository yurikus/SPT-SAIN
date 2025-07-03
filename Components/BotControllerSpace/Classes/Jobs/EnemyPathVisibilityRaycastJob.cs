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

        protected override IEnumerator PrimaryFunction()
        {
            CreateJobs();
            int Total = RaycastJobs.Count;
            if (Total > 0)
            {
                ScheduleJobs(Total);
                yield return AwaitCompletion(Total);
                ReadResults(Total);
                Dispose();
                // Logger.LogDebug($"Completed {Total}");
            }
        }

        private void ScheduleJobs(int Total)
        {
            for (int i = 0; i < Total; i++)
                RaycastJobs[i].RaycastJob.Schedule();
        }

        private void ReadResults(int Total)
        {
            for (int i = 0; i < Total; i++)
            {
                NavMeshPathRaycastJob Job = RaycastJobs[i];
                Job.RaycastJob.Complete();
                NativeArray<RaycastHit> Hits = Job.RaycastJob.Hits;
                NativeArray<RaycastCommand> Commands = Job.RaycastJob.Commands;

                //Vector3 HeadPosition = Job.Owner.MainParts[BodyPartType.head].Position;
                //for (int j = 0; j < Hits.Length; j++)
                //{
                //    RaycastHit Hit = Hits[j];
                //    RaycastCommand Command = Commands[j];
                //    AddOrUpdateDebugObject(Hit, Command);
                //}

                if (SAINEnableClass.GetSAIN(Job.Owner?.AIData?.BotOwner, out BotComponent Bot))
                {
                    Enemy Enemy = Bot.EnemyController.GetEnemy(Job.Target?.ProfileId, false);
                    if (Enemy != null)
                    {
                        bool PointFound = false;
                        //if (Enemy.EnemyPlayer.IsYourPlayer)
                        //    for (int j = Hits.Length - 1; j >= 0; j--)
                        //    {
                        //        RaycastCommand Command = Commands[j];
                        //        Vector3 Point = Command.from + Command.direction * Command.distance;
                        //        DebugGizmos.Sphere(Point, 0.05f, 0.1f);
                        //        RaycastHit Hit = Hits[j];
                        //        if (Hit.collider == null)
                        //        {
                        //            DebugGizmos.Line(Command.from, Point, 0.05f, 0.1f);
                        //        }
                        //    }
                        for (int j = Hits.Length - 1; j >= 0; j--)
                        {
                            RaycastHit Hit = Hits[j];
                            if (Hit.collider == null || j == 0)
                            {
                                RaycastCommand Command = Commands[j];
                                Vector3 LastVisiblePoint = Command.from + Command.direction * Command.distance;
                                Enemy.SetLastVisiblePathPoint(LastVisiblePoint, Job.GetCornerFromHitIndex(j));
                                PointFound = true;
                                break;
                                //if (Physics.SphereCast(LastVisiblePoint, 0.05f, Vector3.down, out RaycastHit raycastHit, 2.0f, LayerMaskClass.HighPolyWithTerrainMask) &&
                                //    NavMesh.SamplePosition(raycastHit.point, out NavMeshHit navMeshHit, 0.5f, -1))
                                //{
                                //    Enemy.SetLastVisiblePathPoint(navMeshHit.position, Job.GetCornerFromHitIndex(j));
                                //    PointFound = true;
                                //    break;
                                //}
                            }
                        }
                        if (!PointFound)
                        {
                            Enemy.ClearVisiblePathPoint();
                        }
                    }
                }
            }
            //ClearExcessDebugObjects();
        }

        private void AddOrUpdateDebugObject(RaycastHit Hit, RaycastCommand Command)
        {
            //DebugSphere(Command.from + Command.direction, 1, Color.green);
            if (Hit.collider == null)
            {
                DebugLine(Command.from, Command.from + Command.direction * Command.distance, 0.025f, Color.white);
            }
            else
            {
                DebugLine(Command.from, Hit.point, 0.025f, Color.red);
                //DebugLine(Command.from + Command.direction, Hit.point, 0.025f, Color.red);
            }
        }

        private void DebugLine(Vector3 Start, Vector3 End, float Size, Color color)
        {
            if (DebugLines.Count > TotalLines)
            {
                DebugGizmos.UpdateLine(DebugLines[TotalLines], Start, End, Size, color);
            }
            else
            {
                DebugLines.Add(DebugGizmos.Line(Start, End, color, Size));
            }
            TotalLines++;
        }

        private void DebugSphere(Vector3 Position, float Size, Color color)
        {
            if (DebugSpheres.Count > TotalSpheres)
            {
                DebugGizmos.UpdateSphere(DebugSpheres[TotalSpheres], Position, Size, color);
            }
            else
            {
                DebugSpheres.Add(DebugGizmos.Sphere(Position, Size, color));
            }
            TotalSpheres++;
        }

        private void ClearExcessDebugObjects()
        {
            for (int i = DebugSpheres.Count - 1; i > TotalSpheres; i--)
            {
                DebugSpheres[i].SetActive(false);
                //GameObject.Destroy(DebugSpheres[i]);
                //DebugSpheres.RemoveAt(i);
            }
            for (int i = DebugLines.Count - 1; i > TotalLines; i--)
            {
                DebugLines[i].SetActive(false);
                //GameObject.Destroy(DebugLines[i]);
                //DebugLines.RemoveAt(i);
            }
        }

        private readonly List<GameObject> DebugLines = [];
        private readonly List<GameObject> DebugSpheres = [];
        private int TotalSpheres = 0;
        private int TotalLines = 0;

        private void CreateJobs()
        {
            LayerMask HighPolyWithTerrain = LayerMaskClass.HighPolyWithTerrainMask;
            LayerMask DoorLayer = LayerMaskClass.DoorLayer;
            LayerMask Mask = HighPolyWithTerrain & ~(1 << DoorLayer);
            foreach (var bot in AliveBots.Values)
            {
                if (bot != null && bot.BotActive)
                {
                    foreach (Enemy enemy in bot.EnemyController.Enemies.Values)
                    {
                        if (enemy == null) continue;
                        if (enemy.EnemyKnown)
                        {
                            Vector3[] corners = enemy.Path.PathCorners;
                            if (corners != null)
                            {
                                int cornerCount = corners.Length;
                                if (cornerCount > 2 && enemy.Path.PathToEnemyStatus != NavMeshPathStatus.PathInvalid && enemy.Path.VisionCheckPoints.Count > 0)
                                {
                                    RaycastJobs.Add(NavMeshPathRaycastJob.Create([.. enemy.Path.VisionCheckPoints], 5, Mask, bot.Player, enemy.EnemyPlayer));
                                    continue;
                                }
                                else if (cornerCount == 2)
                                {
                                    enemy.SetLastCornerAsVisiblePathPoint(corners[1], 1);
                                    continue;
                                }
                            }
                        }
                        enemy.ClearVisiblePathPoint();
                    }
                }
            }
        }

        private IEnumerator AwaitCompletion(int Total)
        {
            int FramesWaited = 0;
            float DeltaTimeWaited = 0;
            const int MaxFramesToWait = 10;
            bool JobsComplete = false;
            while (!JobsComplete && FramesWaited < MaxFramesToWait)
            {
                for (int i = 0; i < Total; i++)
                {
                    if (!RaycastJobs[i].RaycastJob.IsCompleted)
                        continue;
                    JobsComplete = true;
                }
                yield return null;
                FramesWaited++;
                DeltaTimeWaited += Time.deltaTime;
            }

            // Logger.LogDebug($"Took {FramesWaited} frames or {DeltaTimeWaited} seconds To Complete Navmesh Raycasts Jobs");
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
            foreach (NavMeshPathRaycastJob Job in RaycastJobs)
            {
                Job.RaycastJob.Dispose();
            }
            RaycastJobs.Clear();
        }
    }
}