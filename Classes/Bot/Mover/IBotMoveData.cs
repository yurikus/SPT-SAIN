using System;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Mover
{
    public interface IBotMoveData : IDisposable
    {
        public bool Active { get; }
        public EBotMoveStatus CurrentMoveStatus { get; }
        public BotCornerDetails Destination { get; }
        public EBotSprintStatus CurrentSprintStatus { get; }
        public ESprintUrgency SprintUrgency { get; }
        public List<BotCornerDetails> PathCornerDetails { get; }
        public List<Vector3> PathCorners { get; }
        public int CornerCount { get; }
        public int CurrentIndex { get; }
        public bool OnLastCorner { get; }
        public float CurrentCornerDistanceSqr { get; }
        public BotCornerDetails CurrentCorner { get; }
        public BotCornerDetails LastCorner { get; }
        public bool WantToSprint { get; }
        public bool ShallSprintNow { get; }
        public bool ShallStopSprintWhenSeeEnemy { get; }
        public float PauseTime { get; }
        public float CancelTime { get; }
        public float PathLength { get; }
        public float TimeStarted { get; }
        public bool Canceling { get; }
    }
}