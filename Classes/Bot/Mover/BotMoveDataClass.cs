using SAIN.Helpers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class BotMoveDataClass : IBotMoveData
    {
        public bool Active => CurrentMoveStatus != EBotMoveStatus.None;
        public EBotMoveStatus CurrentMoveStatus { get; set; }
        public BotCornerDetails Destination { get; set; } = new();
        public BotCornerDetails LastCorner { get; set; } = new();
        public BotCornerDetails CurrentCorner { get; set; } = new();
        public int CurrentIndex { get; set; }

        public List<BotCornerDetails> PathCornerDetails { get; } = [];
        public List<Vector3> PathCorners { get; } = [];
        public int CornerCount => PathCorners.Count;
        public bool OnLastCorner => CurrentIndex == CornerCount - 1;
        public float CurrentCornerDistanceSqr { get; set; }

        public EBotSprintStatus CurrentSprintStatus { get; set; }
        public ESprintUrgency SprintUrgency { get; set; }
        public bool WantToSprint { get; set; }
        public bool ShallSprintNow { get; set; }
        public bool ShallStopSprintWhenSeeEnemy { get; set; }

        public float PauseTime { get; set; }
        public bool Paused => CurrentMoveStatus == EBotMoveStatus.Paused;

        public bool CheckPaused()
        {
            if (CurrentMoveStatus == EBotMoveStatus.Paused)
            {
                if (PauseTime > Time.time)
                {
                    return true;
                }
                CurrentMoveStatus = WantToSprint ? EBotMoveStatus.Running : EBotMoveStatus.Walking;
            }
            return false;
        }

        public float CancelTime { get; set; }
        public bool Canceling => CurrentMoveStatus == EBotMoveStatus.Canceling;

        public float TimeStarted { get; set; }

        public float PathLength { get; set; }

        /// <summary>
        /// Dispose old data. Cache new data and analyze path corners to populate PathCornerDetails list.
        /// </summary>
        /// <param name="corners"></param>
        /// <param name="shortCornerConfigDistanceSqr"></param>
        public void ActivateNewPath(Vector3 destination, bool shallSprint, ESprintUrgency urgency, Vector3[] corners, float shortCornerConfigDistance)
        {
            Dispose();

            CurrentMoveStatus = shallSprint ? EBotMoveStatus.Running : EBotMoveStatus.Walking;
            TimeStarted = Time.time;
            WantToSprint = shallSprint;
            SprintUrgency = urgency;

            AnalyzePath(corners, shortCornerConfigDistance);
            SetNewDestination(destination);
        }

        /// <summary>
        /// Checks if the destination is the same as where we already going, or if we can update our path to a modified destination
        /// </summary>
        /// <param name="possibleDestination"></param>
        /// <param name="shallSprint"></param>
        /// <returns></returns>
        public bool TryUpdatePath(Vector3 possibleDestination)
        {
            if (Active)
            {
                const float MIN_DIST_CHANGE_DESTINATION = 0.025f;
                const float MIN_DIST_UPDATE_DESTINATION = 0.5f;
                const float MIN_DIST_CALC_PATH_NEW_DESTINATION = 1f;
                // If the place being requested to move to is very close to where we are already moving to, we dont need to change anything.
                if ((Destination.Position - possibleDestination).sqrMagnitude < MIN_DIST_CHANGE_DESTINATION)
                {
                    return true;
                }
                // If the destination is close enough to where the last corner is on the path, update the final destination, but dont recalc the path.
                float distanceFromLastCornerSqr = (LastCorner.Position - possibleDestination).sqrMagnitude;
                if (distanceFromLastCornerSqr < MIN_DIST_UPDATE_DESTINATION)
                {
                    //Logger.LogDebug($"Move Destination Updated: [{Time.time}]");
                    SetNewDestination(possibleDestination);
                    return true;
                }
                // If the destination is close enough to the last corner, try calculating a short path from the second to last corner and see if it will work.
                if (distanceFromLastCornerSqr < MIN_DIST_CALC_PATH_NEW_DESTINATION && TryCalcPathToNewDestination(possibleDestination, out Vector3[] newCorners) && newCorners.Length > 0)
                {
                    //Logger.LogDebug($"Move Destination Path Modified: [{Time.time}]");
                    AddCornersToPath(newCorners);
                    SetNewDestination(possibleDestination);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Created corner details for input corner array, and adds them to the existing path and updates old point types
        /// </summary>
        /// <param name="newCorners"></param>
        private void AddCornersToPath(Vector3[] newCorners)
        {
            int oldLastIndex = PathCorners.Count - 1;
            bool wasOnLastCorner = CurrentCorner.Index == oldLastIndex;
            PathCorners.RemoveAt(oldLastIndex);
            PathCornerDetails.RemoveAt(oldLastIndex);

            int newCornerCount = newCorners.Length;
            for (int i = 0; i < newCornerCount; i++)
            {
                PathCorners.Add(newCorners[i]);
                Vector3? nextCorner = i < newCornerCount - 1 ? newCorners[i + 1] : null;
                PathCornerDetails.AddCornerToPath(newCorners[i], nextCorner, EBotCornerType.PathTurn, EBotCornerType.PathEndApproach, EBotCornerType.PathEnd);
            }

            PathLength = PathCornerDetails.CalcPathLength();

            int newLastIndex = PathCorners.Count - 1;
            LastCorner = PathCornerDetails[newLastIndex];
            if (wasOnLastCorner && newLastIndex >= oldLastIndex)
            {
                CurrentCorner = PathCornerDetails[oldLastIndex];
            }
        }

        private void SetNewDestination(Vector3 destination)
        {
            int count = PathCorners.Count;
            Destination = BotCornerDetails.Create(destination, EBotCornerType.Destination, count);

            BotCornerDetails lastCorner = PathCornerDetails[count - 1];
            lastCorner.SetDirection(destination - lastCorner.Position);
            LastCorner = lastCorner;
            PathCornerDetails[count - 1] = lastCorner;

            //Logger.LogDebug($"Destination Set: [{Time.time}]");
        }

        /// <summary>
        /// Tries to calculate a short path from the second to last corner to the new destination
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="newCorners"></param>
        /// <returns></returns>
        private bool TryCalcPathToNewDestination(Vector3 destination, out Vector3[] newCorners)
        {
            Vector3 secondToLastCorner = PathCorners[CornerCount - 2];
            _destinationPath.ClearCorners();
            if (NavMesh.CalculatePath(secondToLastCorner, destination, -1, _destinationPath) && _destinationPath.status == NavMeshPathStatus.PathComplete)
            {
                newCorners = _destinationPath.corners;
                return true;
            }
            newCorners = null;
            return false;
        }

        private void AnalyzePath(Vector3[] corners, float shortCornerConfigDistance)
        {
            PathCornerDetails.Clear();
            PathCorners.Clear();
            for (int i = 0; i < corners.Length; i++)
            {
                PathCorners.Add(corners[i]);
                PathCornerDetails.Add(BotCornerDetails.Create(ref corners, shortCornerConfigDistance, CornerCount, i));
            }
            PathLength = PathCornerDetails.CalcPathLength();
            LastCorner = PathCornerDetails[PathCornerDetails.Count - 1];
        }

        /// <summary>
        /// Resets all cached data
        /// </summary>
        public void Dispose()
        {
            CurrentMoveStatus = EBotMoveStatus.None;
            CurrentSprintStatus = EBotSprintStatus.None;
            SprintUrgency = ESprintUrgency.None;

            Destination = default;
            LastCorner = default;
            CurrentCorner = default;

            WantToSprint = false;
            ShallSprintNow = false;
            ShallStopSprintWhenSeeEnemy = false;

            PathCornerDetails.Clear();
            PathCorners.Clear();
            _destinationPath.ClearCorners();

            CurrentIndex = 0;
            CurrentCornerDistanceSqr = 0;
            PauseTime = -1;
            CancelTime = -1;
            PathLength = 0;
            TimeStarted = 0;
        }

        private NavMeshPath _destinationPath { get; } = new();
    }
}