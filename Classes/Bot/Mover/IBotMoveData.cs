using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Mover
{
    public interface IBotMoveData : IDisposable
    {
        public bool Moving { get; }
        public bool Running { get; }

        public ESprintUrgency SprintUrgency { get; set; }
        public bool WantToSprint { get; set; }
        public bool ShallStopSprintWhenSeeEnemy { get; set; }
        public bool PathRecalcRequested { get; set; }

        public int CurrentIndex { get; }
        public CornerMoveData CurrentCornerMoveData { get; }
        public EBotMoveStatus CurrentMoveStatus { get; }
        public BotPathCorner Destination { get; }
        public List<BotPathCorner> PathCorners { get; }
        public List<Vector3> PathPoints { get; }
        public EBotSprintStatus CurrentSprintStatus { get; }
        public bool ShallSprintNow { get; }
        public float PauseTime { get; }
        public Action<OperationResult> OnPathComplete { get; }
        public float CancelTime { get; }
        public float TimeStarted { get; }
        public float PathLength { get; }
        public Vector3 StartPosition { get; }
        public bool Paused { get; }
        public bool OnLastCorner { get; }
        public float CurrentCornerDistanceSqr { get; }
        public bool Canceling { get; }
        public bool Crawling { get;  set; }
        public float DestinationReachDistance { get; }
        public NavMeshPathStatus PathStatus { get; }


        public void Start();
        public void Stop(bool success, string reason = null);
        public void Pause(float duration);
        public void UnPause();
        public void Cancel(float delay);
        public BotPathCorner GetCurrentCorner();
        public BotPathCorner GetLastCorner();
    }
}