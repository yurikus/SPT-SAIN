using SAIN.Components;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using System.Collections;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Components.BotController;
using EFT;
using Comfort.Common;
using UnityEngine.Experimental.AI;

namespace SAIN.Types.Jobs
{
    public struct NavMeshPathQuerryJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<NavMeshQueryDataEnemy> Input;
        [WriteOnly] public NativeArray<NavMeshQuery> Output;

        public NavMeshPathQuerryJob(List<Enemy> enemies, List<NavMeshQueryDataEnemy> preAllocatedList)
        {
            preAllocatedList.Clear();
            foreach (Enemy enemy in enemies)
            {
                if (enemy != null)
                {
                    preAllocatedList.Add(new(enemy));
                }
            }
            int count = preAllocatedList.Count;
            Input = new NativeArray<NavMeshQueryDataEnemy>(count, Allocator.TempJob);
            Output = new NativeArray<NavMeshQuery>(count, Allocator.TempJob);

            for (int i = 0; i < count; i++)
            {
                Input[i] = preAllocatedList[i];
            }
            preAllocatedList.Clear();
        }

        public void Execute(int index)
        {
            NavMeshQueryDataEnemy Data = Input[index];
            Data.Execute(1024);
            Output[index] = Data.Query;
        }

        public void Dispose()
        {
            Input.Dispose();
            Output.Dispose();
        }
    }

    public struct NavMeshQueryData(NavMeshLocation Start, NavMeshLocation End)
    {
        public NavMeshQuery Query = new(NavMeshWorld.GetDefaultWorld(), Allocator.TempJob, 60000);
        public NavMeshLocation StartPosition = Start;
        public NavMeshLocation EndPosition = End;
        public void Execute(int iterations)
        {
            Query.BeginFindPath(StartPosition, EndPosition);
            Query.UpdateFindPath(iterations, out _);
            Query.UpdateFindPath(iterations, out _);
            Query.UpdateFindPath(iterations, out _);
            //Query.EndFindPath(out int pathSize);
            //PathSize = pathSize;
        }
        //public int PathSize;
    }

    public struct NavMeshQueryDataEnemy
    {
        public NavMeshQuery Query = new(NavMeshWorld.GetDefaultWorld(), Allocator.TempJob, 60000);
        public NavMeshLocation StartPosition;
        public NavMeshLocation EndPosition;
        public Enemy Enemy;

        public NavMeshQueryDataEnemy(Enemy enemy)
        {
            StartPosition = Query.MapLocation(enemy.Bot.Position, new Vector3(1, 2, 1), 0);
            EndPosition = Query.MapLocation(enemy.EnemyPosition, new Vector3(1, 2, 1), 0);
            Enemy = enemy;
        }
        public void Execute(int iterations)
        {
            Query.BeginFindPath(StartPosition, EndPosition);
            Query.UpdateFindPath(iterations, out _);
            Query.EndFindPath(out int pathSize);
            PathSize = pathSize;
        }
        public int PathSize;
    }

    public interface ISainJob
    {
        public void Start();

        public void Stop();
    }

    public abstract class SainMultiJobTemplate(string InName, MonoBehaviour InOwner, bool InLooping = true, float InLoopInterval = 1.0f / 60.0f) : SainJobTemplate(InName, InOwner, InLooping, InLoopInterval)
    {
        protected readonly List<JobHandle> JobHandles = [];

        protected virtual void ClearJobHandles()
        {
            foreach (JobHandle Handle in JobHandles)
                if (Handle.IsCompleted)
                    Handle.Complete();
            JobHandles.Clear();
        }
    }

    public abstract class SainMultiRaycastJobTemplate(string InName, MonoBehaviour InOwner, bool InLooping = true, float InLoopInterval = 1.0f / 60.0f) : SainJobTemplate(InName, InOwner, InLooping, InLoopInterval)
    {
        protected readonly List<IRaycastJob> Jobs = [];

        protected void StopAndClearJobs()
        {
            foreach (IRaycastJob Job in Jobs)
                Job.Dispose();
            Jobs.Clear();
        }
    }

    public abstract class SainJobTemplate(string InName, MonoBehaviour InOwner, bool InLooping = true, float InLoopInterval = 1.0f / 30.0f) : ISainJob
    {
        protected readonly string Name = InName;
        protected readonly bool Looping = InLooping;
        protected readonly float LoopInterval = InLoopInterval;
        protected readonly MonoBehaviour Owner = InOwner;

        public event Action OnExecutionFinished;

        public event Action OnExecutionStarted;

        public bool Active => Coroutine != null;

        protected Coroutine Coroutine;

        protected virtual IEnumerator Loop()
        {
            Logger.LogDebug($"Starting Job: [{Name}]");
            WaitForSeconds Wait = new(LoopInterval);
            while (LoopCondition())
            {
                if (!CanProceed())
                {
                    //Logger.LogDebug($"Cannot Proceed [{Name}]");
                    yield return null;
                    continue;
                }
                OnExecutionStarted?.Invoke();
                yield return PrimaryFunction();
                OnExecutionFinished?.Invoke();
                yield return Wait;
            }
            Logger.LogDebug($"Job Ended [{Name}]");
        }

        protected virtual IEnumerator PrimaryFunction()
        {
            yield return null;
        }

        protected virtual bool LoopCondition()
        {
            return true;
        }

        protected virtual bool CanProceed()
        {
            return true;
        }

        public void Start()
        {
            if (!Active)
            {
                if (Owner == null)
                {
                    Logger.LogError("Owner Null. Cannot Start.");
                }
                else
                {
                    Coroutine = Owner.StartCoroutine(Loop());
                }
            }
        }

        public void Stop()
        {
            if (Active)
            {
                if (Owner == null)
                {
                    Logger.LogError("Owner Null. Cannot Stop Coroutine.");
                }
                else
                {
                    Owner.StopCoroutine(Coroutine);
                }
            }
        }

        protected static GameWorld GameWorld => Singleton<GameWorld>.Instance;
        protected static IBotGame BotGame => Singleton<IBotGame>.Instance;
        protected static GameWorldComponent SAINGameWorld => GameWorldComponent.Instance;
        protected static SAINBotController SAINBotController => SAINBotController.Instance;
        protected static PlayerDictionary AlivePlayers => GameWorldComponent.Instance?.PlayerTracker?.AlivePlayersDictionary;
        protected static List<IPlayer> DeadPlayers => GameWorldComponent.Instance?.PlayerTracker?.DeadPlayers;
        protected static BotDictionary AliveBots => BotSpawnController.Instance?.BotDictionary;

        protected static bool GameActive {
            get
            {
                return GameStatus switch {
                    GameStatus.Running or GameStatus.Runned or GameStatus.Starting or GameStatus.Started => true,
                    _ => false,
                };
            }
        }

        protected static GameStatus GameStatus {
            get
            {
                if (BotGame == null)
                {
                    return GameStatus.Stopped;
                }
                return BotGame.Status;
            }
        }
    }
}