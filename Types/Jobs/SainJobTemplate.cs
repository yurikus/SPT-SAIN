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

namespace SAIN.Types.Jobs
{
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