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
    public readonly struct RandomDir
    {
        public RandomDir(float RandomMin, float RandomMax)
        {
            Magnitude = UnityEngine.Random.Range(RandomMin, RandomMax);
            DirectionNormal = UnityEngine.Random.onUnitSphere;
            Direction = DirectionNormal * Magnitude;
        }

        public RandomDir(float magnitude)
        {
            Magnitude = magnitude;
            DirectionNormal = UnityEngine.Random.onUnitSphere;
            Direction = DirectionNormal * Magnitude;
        }

        public RandomDir(float magnitude, Vector3 directionNormal)
        {
            Magnitude = magnitude;
            DirectionNormal = directionNormal;
            Direction = DirectionNormal * Magnitude;
        }

        public readonly Vector3 Direction;
        public readonly Vector3 DirectionNormal;
        public readonly float Magnitude;
    }

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
                        for (int j = Hits.Length - 1; j >= 0; j--)
                        {
                            RaycastHit Hit = Hits[j];
                            if (Hit.collider == null || j == 0)
                            {
                                RaycastCommand Command = Commands[j];
                                Vector3 LastVisiblePoint = Command.from + Command.direction * Command.distance;
                                if (NavMesh.SamplePosition(LastVisiblePoint, out NavMeshHit hit, 2, -1))
                                {
                                    Enemy.SetLastVisiblePathPoint(hit.position);
                                    PointFound = true;
                                    break;
                                }
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
            foreach (var bot in AliveBots.Values)
            {
                if (bot?.BotActive == true)
                {
                    foreach (Enemy enemy in bot.EnemyController.Enemies.Values)
                    {
                        if (enemy != null && enemy.Path.PathToEnemyStatus != NavMeshPathStatus.PathInvalid && enemy.Path.VisionCheckPoints.Count > 0)
                        {
                            RaycastJobs.Add(NavMeshPathRaycastJob.Create([.. enemy.Path.VisionCheckPoints], 5, LayerMaskClass.HighPolyWithTerrainMask, bot.Player, enemy.EnemyPlayer));
                        }
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