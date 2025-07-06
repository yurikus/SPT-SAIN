using Comfort.Common;
using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.SAINComponent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Bindings;

namespace SAIN.Components.RotationController
{
    public class BotRotationManagerComponent : MonoBehaviour
    {
        public static BotRotationManagerComponent Create(GameObject gameObject, BotSpawner botSpawner, PlayerSpawnTracker playerSpawner)
        {
            return gameObject.AddComponent<BotRotationManagerComponent>();
        }

        protected void Update()
        {
            HashSet<PlayerComponent> bots = GameWorldComponent.Instance.PlayerTracker.AlivePlayerArray;
            foreach (PlayerComponent player in bots)
            {
                player?.UpdateControlRotation(Time.deltaTime);
            }
        }

        public struct CalcRotationJob : IJobFor
        {
            public static CalcRotationJob Create(List<CalcRotationInput> inputData, float deltaTime)
            {
                int count = inputData.Count;
                CalcRotationJob Result = new() {
                    Input = new NativeArray<CalcRotationInput>(count, Allocator.TempJob),
                    Output = new NativeArray<CalcRotationOutput>(count, Allocator.TempJob),
                    DeltaTime = deltaTime,
                };
                for (int i = 0; i < count; i++)
                {
                    Result.Input[i] = inputData[i];
                }
                return Result;
            }

            [ReadOnly] private float DeltaTime;
            [ReadOnly] public NativeArray<CalcRotationInput> Input;
            [WriteOnly] public NativeArray<CalcRotationOutput> Output;

            public void Execute(int index)
            {
                CalcRotationInput Data = Input[index];
                //RotationTypes.CalcSmoothDampAngleTurn(ref Data, out CalcRotationOutput result, 400, DeltaTime);
                //Output[index] = result;
            }
        }

        public struct CalcRotationInput(Vector3 inLookRotation, Vector3 inTargetLookDirection, Vector3 inLookVelocity, float inSmoothing)
        {
            public Vector3 LookDirection = inLookRotation;
            public Vector3 TargetLookDirection = inTargetLookDirection;
            public float Smoothing = inSmoothing;
            public Vector3 LookVelocity = inLookVelocity;
        }

        public struct CalcRotationOutput
        {
            public Vector3 CalculatedLookDirection;
            public Vector3 LookVelocity;
        }
    }
}