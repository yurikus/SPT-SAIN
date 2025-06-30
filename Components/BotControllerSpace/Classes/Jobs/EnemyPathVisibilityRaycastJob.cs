using SAIN.Components.PlayerComponentSpace;
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

        public readonly Vector3 Direction;
        public readonly Vector3 DirectionNormal;
        public readonly float Magnitude;
    }

    public class RandomVisiblePointGeneratorJob : SainJobTemplate, IDisposable
    {
        public RandomVisiblePointGeneratorJob(SAINBotController botcontroller) : base("Random Visible Point Generator", botcontroller, true)
        {
            //Start();
        }

        protected readonly List<RaycastJob> RaycastJobs = [];

        protected override IEnumerator PrimaryFunction()
        {
            //RandomDir[] ShortRandomDirections = GenerateRandomDirections(100, 0.5f, 5.0f);
            //RandomDir[] MidRangeRandomDirections = GenerateRandomDirections(100, 5f, 12.0f);
            RandomDir[] LongRandomDirections = GenerateRandomDirections(500, 0.5f, 100.0f);
            foreach (var player in AlivePlayers.Values)
            {
                if (player?.IsActive == true)
                {
                    //RaycastJobs.Add(new RaycastJob(ShortRandomDirections, player.Transform.HeadPosition, LayerMaskClass.HighPolyWithTerrainMask, player.Player, null));
                    //RaycastJobs.Add(new RaycastJob(MidRangeRandomDirections, player.Transform.HeadPosition, LayerMaskClass.HighPolyWithTerrainMask, player.Player, null));
                    RaycastJobs.Add(new RaycastJob(LongRandomDirections, player.Transform.HeadPosition, LayerMaskClass.HighPolyWithTerrainMask, player.Player, null));
                }
            }
            int Total = RaycastJobs.Count;
            if (Total > 0)
            {
                ScheduleJobs(Total);
                yield return AwaitCompletion(Total);

                for (int i = 0; i < Total; i++)
                {
                    RaycastJob Job = RaycastJobs[i];
                    Job.Complete();
                    NativeArray<RaycastHit> Hits = Job.Hits;
                    NativeArray<RaycastCommand> Commands = Job.Commands;

                    if (GameWorldComponent.TryGetPlayerComponent(Job.Owner, out PlayerComponent Player))
                    {
                        for (int j = Hits.Length - 1; j >= 0; j--)
                        {
                            RaycastHit Hit = Hits[j];
                            if (Hit.collider == null)
                            {
                                RaycastCommand Command = Commands[j];
                                Vector3 Point = Command.from + Command.direction * Command.distance;
                                Color RandomColor = DebugGizmos.RandomColor;
                                if (Player.Player.IsYourPlayer)
                                {
                                    DebugGizmos.Sphere(Point, 0.025f, RandomColor, true, 0.05f);
                                    //DebugGizmos.Line(Command.from, Point, RandomColor, 0.01f, true, 0.05f);
                                }
                                if (Command.distance > 3)
                                {
                                    if (NavMesh.SamplePosition(Point, out NavMeshHit NavHit, 1.5f, -1))
                                    {
                                        if (Player.Player.IsYourPlayer)
                                        {
                                            DebugGizmos.Sphere(NavHit.position, 0.1f, RandomColor, true, 0.05f);
                                            DebugGizmos.Line(NavHit.position, NavHit.position + Vector3.up * 1.5f, RandomColor, 0.025f, true, 0.05f);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                Dispose();
                // Logger.LogDebug($"Completed {Total}");
            }
        }

        private void ScheduleJobs(int Total)
        {
            for (int i = 0; i < Total; i++)
                RaycastJobs[i].Schedule();
        }

        private void ReadResults(int Total)
        {
            for (int i = 0; i < Total; i++)
            {
                RaycastJob Job = RaycastJobs[i];
                Job.Complete();
                NativeArray<RaycastHit> Hits = Job.Hits;
                NativeArray<RaycastCommand> Commands = Job.Commands;

                if (GameWorldComponent.TryGetPlayerComponent(Job.Owner, out PlayerComponent Player))
                {
                    for (int j = Hits.Length - 1; j >= 0; j--)
                    {
                        RaycastHit Hit = Hits[j];
                        if (Hit.collider == null)
                        {
                            RaycastCommand Command = Commands[j];
                            Vector3 Point = Command.from + Command.direction * Command.distance;
                            if (Player.Player.IsYourPlayer)
                            {
                                Color RandomColor = DebugGizmos.RandomColor;
                                DebugGizmos.Sphere(Point, 0.05f, RandomColor, true, 0.1f);
                                DebugGizmos.Line(Command.from, Point, RandomColor, 0.025f, true, 0.1f);
                            }
                        }
                    }
                }
            }
            //for (int i = 0; i < Total; i++)
            //{
            //    RaycastJob Job = RaycastJobs[i];
            //    Job.Complete();
            //    NativeArray<RaycastHit> Hits = Job.Hits;
            //    NativeArray<RaycastCommand> Commands = Job.Commands;
            //
            //    if (SAINEnableClass.GetSAIN(Job.Owner?.AIData?.BotOwner, out BotComponent Bot))
            //    {
            //        Enemy Enemy = Bot.EnemyController.GetEnemy(Job.Target?.ProfileId, false);
            //        if (Enemy != null)
            //        {
            //            bool PointFound = false;
            //            for (int j = Hits.Length - 1; j >= 0; j--)
            //            {
            //                RaycastHit Hit = Hits[j];
            //                if (Hit.collider == null)
            //                {
            //                    RaycastCommand Command = Commands[j];
            //                    Vector3 Point = Command.from + Command.direction * Command.distance;
            //                    DebugGizmos.Sphere(Point, 0.05f, 0.1f);
            //                    //if (NavMesh.SamplePosition(LastVisiblePoint, out NavMeshHit hit, 2, -1))
            //                    //{
            //                    //    PointFound = true;
            //                    //    break;
            //                    //}
            //                }
            //            }
            //            if (!PointFound)
            //            {
            //                Enemy.ClearVisiblePathPoint();
            //            }
            //        }
            //    }
            //}
        }

        protected static RandomDir[] GenerateRandomDirections(int Count, float LengthMin, float LengthMax)
        {
            RandomDir[] Result = new RandomDir[Count];
            for (int i = 0; i < Count; i++)
            {
                Result[i] = new RandomDir(LengthMin, LengthMax);
            }
            return Result;
        }

        private void CreateJobs()
        {
            RandomDir[] RandomDirections = GenerateRandomDirections(100, 0.5f, 8.0f);
            foreach (var player in AlivePlayers.Values)
            {
                if (player?.IsActive == true)
                {
                    RaycastJobs.Add(new RaycastJob(RandomDirections, player.Transform.HeadPosition, LayerMaskClass.HighPolyWithTerrainMask, player.Player, null));
                }
            }
            //foreach (var bot in AliveBots.Values)
            //{
            //    if (bot?.BotActive == true)
            //    {
            //        foreach (Enemy enemy in bot.EnemyController.Enemies.Values)
            //        {
            //            if (enemy?.EnemyKnown == true)
            //            {
            //                RaycastJobs.Add(new RaycastJob(RandomDirections, enemy.EnemyHeadPosition, LayerMaskClass.HighPolyWithTerrainMask, bot.Player, enemy.EnemyPlayer));
            //            }
            //        }
            //    }
            //}
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
                    if (!RaycastJobs[i].IsCompleted)
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
            foreach (RaycastJob Job in RaycastJobs)
            {
                Job.Dispose();
            }
            RaycastJobs.Clear();
        }
    }

    public class EnemyPathVisibilityRaycastJob : SainJobTemplate, IDisposable
    {
        public EnemyPathVisibilityRaycastJob(SAINBotController botcontroller) : base("Path Visibility Job", botcontroller, true)
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
                            if (Hit.collider == null)
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
                        NavMeshPath Path = enemy?.Path?.PathToEnemy;
                        if (Path != null && Path.status != NavMeshPathStatus.PathInvalid)
                        {
                            RaycastJobs.Add(NavMeshPathRaycastJob.Create(Path.corners, 5, LayerMaskClass.HighPolyWithTerrainMask, bot.Player, enemy.EnemyPlayer));
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