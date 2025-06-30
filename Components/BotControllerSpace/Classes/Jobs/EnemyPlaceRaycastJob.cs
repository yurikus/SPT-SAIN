using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace SAIN.Components
{
    public class EnemyPlaceRaycastJob : SAINControllerBase
    {
        public struct CalcEnemyPlaceJob : IJobFor
        {
            [ReadOnly] public NativeArray<Vector3> PlacePositions;
            [ReadOnly] public NativeArray<Vector3> BotPositions;
            [ReadOnly] public NativeArray<Vector3> EnemyPositions;
            [WriteOnly] public NativeArray<float> PlaceDistancesToBot;
            [WriteOnly] public NativeArray<float> PlaceDistancesToEnemy;

            public void Execute(int index)
            {
                Vector3 EnemyPlace = PlacePositions[index];
                Vector3 BotPosition = BotPositions[index];
                Vector3 EnemyPosition = EnemyPositions[index];
                PlaceDistancesToBot[index] = (BotPosition - EnemyPlace).magnitude;
                PlaceDistancesToEnemy[index] = (EnemyPosition - EnemyPlace).magnitude;
            }

            public void Dispose()
            {
                if (PlacePositions.IsCreated) PlacePositions.Dispose();
                if (BotPositions.IsCreated) BotPositions.Dispose();
                if (EnemyPositions.IsCreated) EnemyPositions.Dispose();
                if (PlaceDistancesToBot.IsCreated) PlaceDistancesToBot.Dispose();
                if (PlaceDistancesToEnemy.IsCreated) PlaceDistancesToEnemy.Dispose();
            }
        }

        public EnemyPlaceRaycastJob(SAINBotController botcontroller) : base(botcontroller)
        {
            botcontroller.StartCoroutine(EnemyPlaceJobLoop());
        }

        private JobHandle EnemyPlaceJobHandle;
        private CalcEnemyPlaceJob EnemyPlaceJob;

        private IEnumerator EnemyPlaceJobLoop()
        {
            yield return null;

            while (true)
            {
                if (BotController == null)
                {
                    yield return null;
                    continue;
                }

                var bots = BotController.BotSpawnController?.BotDictionary;
                if (bots == null || bots.Count == 0)
                {
                    yield return null;
                    continue;
                }

                if (BotController.BotGame?.Status == EFT.GameStatus.Stopping)
                {
                    yield return null;
                    continue;
                }

                List<EnemyPlace> PlacesToCheck = [];
                foreach (BotComponent bot in bots.Values)
                {
                    if (bot?.BotActive == true)
                    {
                        foreach (Enemy enemy in bot.EnemyController.Enemies.Values)
                        {
                            if (enemy?.EnemyKnown == true)
                            {
                                foreach (EnemyPlace Place in enemy.KnownPlaces.AllEnemyPlaces)
                                {
                                    if (Place != null)
                                        PlacesToCheck.Add(Place);
                                }
                            }
                        }
                    }
                }
                int Count = PlacesToCheck.Count;
                if (Count == 0)
                {
                    yield return null;
                    continue;
                }
                NativeArray<Vector3> PlacePositions = new(Count, Allocator.TempJob);
                NativeArray<Vector3> BotPositions = new(Count, Allocator.TempJob);
                NativeArray<Vector3> EnemyPositions = new(Count, Allocator.TempJob);
                for (int i = 0; i < Count; i++)
                {
                    EnemyPlace Place = PlacesToCheck[i];
                    PlacePositions[i] = Place.Position;
                    BotPositions[i] = Place.PlaceData.Owner.Transform.HeadPosition;
                    EnemyPositions[i] = Place.PlaceData.Enemy.EnemyTransform.Position;
                }
                EnemyPlaceJob = new CalcEnemyPlaceJob {
                    PlacePositions = PlacePositions,
                    BotPositions = BotPositions,
                    EnemyPositions = EnemyPositions,
                    PlaceDistancesToBot = new NativeArray<float>(Count, Allocator.TempJob),
                    PlaceDistancesToEnemy = new NativeArray<float>(Count, Allocator.TempJob)
                };

                // schedule job and wait for next frame to read data
                EnemyPlaceJobHandle = EnemyPlaceJob.Schedule(Count, new JobHandle());
                yield return null;
                EnemyPlaceJobHandle.Complete();

                _commands = new NativeArray<RaycastCommand>(Count, Allocator.TempJob);
                _hits = new NativeArray<RaycastHit>(Count, Allocator.TempJob);

                for (int i = 0; i < Count; i++)
                {
                    EnemyPlace Place = PlacesToCheck[i];
                    Vector3 HeadPosition = Place.PlaceData.Owner.Transform.HeadPosition;
                    Vector3 PlacePosition = Place.Position;
                    float Distance = Place.DistanceToBot;
                    _commands[i] = new RaycastCommand(HeadPosition, PlacePosition - HeadPosition, new QueryParameters {
                        layerMask = _LOSMask
                    }, Distance);
                }

                RaycastJobHandle = RaycastCommand.ScheduleBatch(_commands, _hits, 8);
                yield return null;
                RaycastJobHandle.Complete();

                //var stringBuilder = new StringBuilder();
                //stringBuilder.AppendLine($"Checked {Count} Enemy Places");
                for (int i = 0; i < Count; i++)
                {
                    EnemyPlace Place = PlacesToCheck[i];
                    RaycastHit Hit = _hits[i];
                    //stringBuilder.AppendLine($"Hit: {Hit.collider?.name} : {Hit.distance} :: PlaceDistancesToBot: {EnemyPlaceJob.PlaceDistancesToBot[i]} : PlaceDistancesToEnemy: {EnemyPlaceJob.PlaceDistancesToEnemy[i]}");
                    Place?.SetDistances(EnemyPlaceJob.PlaceDistancesToBot[i], EnemyPlaceJob.PlaceDistancesToEnemy[i]);
                    Place?.SetVisibilityOfPlace(Hit.collider == null);
                }

                //Logger.LogDebug(stringBuilder.ToString());
                EnemyPlaceJob.Dispose();
                _commands.Dispose();
                _hits.Dispose();
            }
        }

        public void Dispose()
        {
            if (!RaycastJobHandle.IsCompleted) RaycastJobHandle.Complete();
            if (!EnemyPlaceJobHandle.IsCompleted) EnemyPlaceJobHandle.Complete();
            EnemyPlaceJob.Dispose();
            if (_commands.IsCreated) _commands.Dispose();
            if (_hits.IsCreated) _hits.Dispose();
        }

        private NativeArray<RaycastHit> _hits;
        private NativeArray<RaycastCommand> _commands;
        private JobHandle RaycastJobHandle;

        private readonly LayerMask _LOSMask = LayerMaskClass.HighPolyWithTerrainMask;
    }
}