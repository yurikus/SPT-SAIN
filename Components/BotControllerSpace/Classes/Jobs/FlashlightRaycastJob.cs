using SAIN.Components.PlayerComponentSpace;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.Types.Jobs;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace SAIN.Components
{
    public class FlashlightRaycastJob : SainJobTemplate, IDisposable
    {
        public FlashlightRaycastJob(MonoBehaviour gameWorld) : base("Flashlight Detection Job", gameWorld, true, 0.1f)
        {
            Start();
        }

        protected readonly List<RaycastJob> RaycastJobs = [];

        protected override IEnumerator PrimaryFunction()
        {
            CreateFlashlightJobs();
            int Total = RaycastJobs.Count;
            if (Total > 0)
            {
                ScheduleJobs(Total);
                yield return AwaitCompletion(Total);
                ReadFlashlightJobData(Total);
                Dispose();
            }

            CreateLightDetectionJobs();
            Total = RaycastJobs.Count;
            if (Total > 0)
            {
                ScheduleJobs(Total);
                yield return AwaitCompletion(Total);
                ReadLightDetectionJobData(Total);
                Dispose();
            }
        }

        private void ReadLightDetectionJobData(int Total)
        {
            for (int i = 0; i < Total; i++)
            {
                RaycastJob Job = RaycastJobs[i];
                Job.Complete();
                NativeArray<RaycastHit> Hits = Job.Hits;
                if (GameWorldComponent.TryGetPlayerComponent(Job.Owner, out PlayerComponent Player))
                {
                    bool VisiblePoint = false;
                    foreach (RaycastHit Hit in Hits)
                    {
                        if (Hit.collider == null)
                        {
                            VisiblePoint = true;
                            break;
                        }
                    }
                    if (VisiblePoint && Job.Target != null)
                    {
                        Player.Flashlight.LightDetection.TryToInvestigate(Job.Target);
                    }
                }
            }
        }

        private void CreateLightDetectionJobs()
        {
            foreach (BotComponent Bot in AliveBots.Values)
            {
                if (Bot != null && Bot.BotActive)
                {
                    foreach (Enemy Enemy in Bot.EnemyController.Enemies.Values)
                    {
                        if (Enemy != null && Enemy.EnemyPerson.Active)
                        {
                            FlashLightClass EnemyLight = Enemy.EnemyPlayerComponent.Flashlight;
                            if (EnemyLight.DeviceActive &&
                                Bot.PlayerComponent.Flashlight.LightDetection.CheckIsBeamVisible(EnemyLight) &&
                                Enemy.RealDistance <= 125f)
                            {
                                RaycastJobs.Add(new RaycastJob(EnemyLight.LightDetection.LightPoints2, Enemy.EnemyTransform.HeadPosition, LayerMaskClass.HighPolyWithTerrainMaskAI, Bot.Player, Enemy.Player));
                            }
                        }
                    }
                }
            }
        }

        private void ReadFlashlightJobData(int Total)
        {
            for (int i = 0; i < Total; i++)
            {
                RaycastJob Job = RaycastJobs[i];
                Job.Complete();
                NativeArray<RaycastHit> Hits = Job.Hits;
                if (GameWorldComponent.TryGetPlayerComponent(Job.Owner, out PlayerComponent Player))
                {
                    List<Vector3> LightPoints = Player.Flashlight.LightDetection.LightPoints2;
                    LightPoints.Clear();
                    for (int j = Hits.Length - 1; j >= 0; j--)
                    {
                        RaycastHit Hit = Hits[j];
                        if (Hit.collider != null)
                        {
                            LightPoints.Add(Hit.point + (Hit.normal * 0.1f));
                        }
                    }
                }
            }
        }

        private void CreateFlashlightJobs()
        {
            const float LaserTraceDistance = 75;
            const float FlashlightTraceDistance = 40;

            foreach (var player in AlivePlayers.Values)
            {
                if (player?.IsActive == true && player.Flashlight.DeviceActive)
                {
                    List<RandomDir> Directions = [];
                    Vector3 WeaponPointDir = player.Transform.WeaponPointDirection;
                    if (player.Flashlight.Laser || player.Flashlight.IRLaser)
                    {
                        Directions.Add(new(LaserTraceDistance, WeaponPointDir));
                    }
                    if (player.Flashlight.WhiteLight || player.Flashlight.IRLight)
                    {
                        createFlashlightBeam(Directions, WeaponPointDir, 30, FlashlightTraceDistance, 12.5f);
                    }
                    if (Directions.Count > 0)
                        RaycastJobs.Add(new RaycastJob(Directions, player.Transform.HeadPosition, LayerMaskClass.HighPolyWithTerrainMaskAI, player.Player, null));
                }
            }
        }

        private void ScheduleJobs(int Total)
        {
            for (int i = 0; i < Total; i++)
                RaycastJobs[i].Schedule();
        }

        private static void createFlashlightBeam(List<Vector3> beamDirections, Vector3 weaponPointDir, int count, float coneAngle = 10.0f)
        {
            beamDirections.Clear();
            for (int i = 0; i < count; i++)
            {
                // Generate random angles within the cone range for yaw and pitch
                float angle = coneAngle * 0.5f;
                float x = UnityEngine.Random.Range(-angle, angle);
                float y = UnityEngine.Random.Range(-angle, angle);
                float z = UnityEngine.Random.Range(-angle, angle);

                // AddColor a Quaternion rotation based on the random yaw and pitch angles
                Quaternion randomRotation = Quaternion.Euler(x, y, z);

                // Rotate the player's look direction by the Quaternion rotation
                Vector3 randomBeamDirection = randomRotation * weaponPointDir;

                beamDirections.Add(randomBeamDirection);
            }
        }

        private static void createFlashlightBeam(List<RandomDir> beamDirections, Vector3 weaponPointDir, int count, float distance, float coneAngle = 10.0f)
        {
            beamDirections.Clear();
            for (int i = 0; i < count; i++)
            {
                // Generate random angles within the cone range for yaw and pitch
                float angle = coneAngle * 0.5f;
                float x = UnityEngine.Random.Range(-angle, angle);
                float y = UnityEngine.Random.Range(-angle, angle);
                float z = UnityEngine.Random.Range(-angle, angle);

                // AddColor a Quaternion rotation based on the random yaw and pitch angles
                Quaternion randomRotation = Quaternion.Euler(x, y, z);

                // Rotate the player's look direction by the Quaternion rotation
                Vector3 randomBeamDirection = randomRotation * weaponPointDir;

                beamDirections.Add(new(distance, randomBeamDirection));
            }
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
}