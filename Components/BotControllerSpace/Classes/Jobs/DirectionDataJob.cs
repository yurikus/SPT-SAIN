using SAIN.Components.PlayerComponentSpace;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace SAIN.Components.BotControllerSpace.Classes.Raycasts
{
    public struct CalcDistanceAndNormalJob : IJobFor
    {
        [ReadOnly] public NativeArray<Vector3> directions;

        [WriteOnly] public NativeArray<float> distances;
        [WriteOnly] public NativeArray<Vector3> normals;

        public void Execute(int index)
        {
            Vector3 direction = directions[index];
            distances[index] = direction.magnitude;
            normals[index] = direction.normalized;
        }

        public void Dispose()
        {
            if (directions.IsCreated) directions.Dispose();
            if (distances.IsCreated) distances.Dispose();
            if (normals.IsCreated) normals.Dispose();
        }
    }

    public struct CalcDistanceJob : IJobFor
    {
        [ReadOnly] public NativeArray<Vector3> directions;
        [WriteOnly] public NativeArray<float> distances;

        public void Execute(int index)
        {
            Vector3 direction = directions[index];
            distances[index] = direction.magnitude;
        }

        public void Dispose()
        {
            if (directions.IsCreated) directions.Dispose();
            if (distances.IsCreated) distances.Dispose();
        }
    }

    public struct PlayerDirectionDataJob : IJobFor
    {
        [ReadOnly] public NativeArray<PlayerDirectionData> Input;
        [WriteOnly] public NativeArray<PlayerDirectionData> Output;

        public void Execute(int index)
        {
            PlayerDirectionData Data = Input[index];

            Data.MainData.Update(Data.OwnerPosition);
            Data.MainData.UpdateDotProductAndCalcNormal(Data.OwnerViewPosition, Data.OwnerLookDirection);

            //for (int i = 0; i < Data.BodyParts.Length; i++)
            //{
            //    Data.BodyParts[i].DirectionData.Update(Data.OwnerViewPosition);
            //    Data.BodyParts[i].DirectionData.UpdateDotProduct(Data.OwnerLookDirection);
            //}
            Output[index] = Data;
        }

        public void Dispose()
        {
            if (Input.IsCreated) Input.Dispose();
            if (Output.IsCreated) Output.Dispose();
        }
    }

    public class DirectionDataJob : SAINControllerBase
    {
        private JobHandle _PlayerDirectionDataJobHandle;
        private PlayerDirectionDataJob _PlayerDirectionDataJob;
        private readonly List<PlayerDirectionData> _directionDatas = [];
        private readonly List<OtherPlayerData> _otherPlayerData = [];

        public DirectionDataJob(SAINBotController botController) : base(botController)
        {
            botController.StartCoroutine(DirectionDataJobLoop());
        }

        private IEnumerator DirectionDataJobLoop()
        {
            yield return null;
            while (GameWorldComponent.Instance != null)
            {
                var players = GameWorldComponent.Instance.PlayerTracker?.AlivePlayersDictionary?.Values;
                if (players == null || players.Count <= 1)
                {
                    yield return null;
                    continue;
                }

                foreach (PlayerComponent playerComp in players)
                {
                    if (playerComp != null && playerComp.OtherPlayersData != null)
                    {
                        List<OtherPlayerData> OtherPlayers = playerComp.OtherPlayersData.DataList;
                        for (int j = 0; j < OtherPlayers.Count; j++)
                        {
                            OtherPlayerData otherPlayer = OtherPlayers[j];
                            if (otherPlayer != null)
                            {
                                _directionDatas.Add(otherPlayer.DistanceData.GetUpdatedDirectionData(playerComp, otherPlayer.PlayerComponent));
                                _otherPlayerData.Add(otherPlayer);
                            }
                        }
                    }
                }

                int jobCount = _directionDatas.Count;
                if (jobCount > 0)
                {
                    _PlayerDirectionDataJob = new() {
                        Input = new NativeArray<PlayerDirectionData>(jobCount, Allocator.TempJob),
                        Output = new NativeArray<PlayerDirectionData>(jobCount, Allocator.TempJob)
                    };
                    for (int i = 0; i < jobCount; i++)
                    {
                        _PlayerDirectionDataJob.Input[i] = _directionDatas[i];
                    }

                    // schedule job and wait for next frame to read data
                    _PlayerDirectionDataJobHandle = _PlayerDirectionDataJob.Schedule(jobCount, new JobHandle());

                    yield return null;
                    _PlayerDirectionDataJobHandle.Complete();

                    for (int i = 0; i < jobCount; i++)
                    {
                        _otherPlayerData[i]?.DistanceData.SetPlayerDirectionData(_PlayerDirectionDataJob.Output[i]);
                    }

                    _PlayerDirectionDataJob.Dispose();
                    _otherPlayerData.Clear();
                    _directionDatas.Clear();
                }
                yield return null;
            }
        }

        public void Dispose()
        {
            _PlayerDirectionDataJobHandle.Complete();
            _PlayerDirectionDataJob.Dispose();
        }
    }
}