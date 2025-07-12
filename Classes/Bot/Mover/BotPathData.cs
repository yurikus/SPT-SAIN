using EFT;
using EFT.Interactive;
using SAIN.Classes.Transform;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class BotPathData : IBotMoveData
    {
        public bool Moving => _coroutine != null;
        public bool Running => _coroutine != null && WantToSprint;
        public int CurrentIndex { get; private set; }
        public CornerMoveData CurrentCornerMoveData { get; private set; }

        public EBotMoveStatus CurrentMoveStatus { get; private set; }
        public BotPathCorner Destination { get; private set; }
        public float DestinationReachDistance { get; private set; }

        public List<BotPathCorner> PathCorners { get; } = [];
        public List<Vector3> PathPoints { get; } = [];

        public EBotSprintStatus CurrentSprintStatus { get; set; }
        public ESprintUrgency SprintUrgency { get; set; }
        public bool WantToSprint { get; set; }
        public bool ShallStopSprintWhenSeeEnemy { get; set; }
        public bool ShallSprintNow { get; private set; }

        public float PauseTime { get; private set; }
        public bool Active => _coroutine != null;

        public bool PathRecalcRequested { get; set; }
        public Action<OperationResult> OnPathComplete { get; private set; }

        public float CancelTime { get; private set; }

        public float TimeStarted { get; }

        public float PathLength { get; private set; }
        public bool Crawling { get; set; }

        public Vector3 StartPosition { get; }

        public bool Paused => CurrentMoveStatus == EBotMoveStatus.Paused;
        public bool OnLastCorner => CurrentIndex == PathCorners.Count - 1;
        public float CurrentCornerDistanceSqr => CurrentCornerMoveData.SqrMagnitude;
        public bool Canceling => CurrentMoveStatus == EBotMoveStatus.Canceling;

        public NavMeshPathStatus PathStatus { get; set; }

        public BotPathData(BotComponent bot, Vector3 botNavPosition, Vector3 destination, bool shallSprint, ESprintUrgency urgency, Vector3[] corners, Action<OperationResult> onComplete)
        {
            Bot = bot;
            StartPosition = botNavPosition;
            CurrentMoveStatus = EBotMoveStatus.ReadyToMove;
            TimeStarted = Time.time;
            WantToSprint = shallSprint;
            SprintUrgency = urgency;
            OnPathComplete = onComplete;
            CurrentCornerMoveData = new() {
                Dot = 1,
                SqrMagnitude = float.MaxValue,
            };

            // skip first corner
            for (int i = 1; i < corners.Length; i++)
            {
                PathPoints.Add(corners[i]);
                Vector3 start = corners[i - 1];
                EBotCornerType type = Util.FindCornerType(corners.Length, i);
                PathCorners.Add(new(start, corners[i], type, i));
            }

            int count = PathPoints.Count;
            // Set next corner directions from precalculate array data
            for (int i = 0; i < count - 1; i++)
            {
                BotPathCorner corner = PathCorners[i];
                corner.DirectionToNext = PathCorners[i + 1].DirectionFromPrevious;
                PathCorners[i] = corner;
            }
            if ((destination - GetLastCorner().Position).sqrMagnitude > 0.1f)
            {
                BotPathCorner destinationCorner = new(GetLastCorner().Position, destination, EBotCornerType.Destination, PathCorners.Count);
                PathPoints.Add(destination);
                PathCorners.Add(destinationCorner);
                Destination = destinationCorner;
                _destinationIsLastCorner = false;
            }
            else
            {
                Destination = GetLastCorner();
                _destinationIsLastCorner = true;
            }
            PathLength = PathCorners.CalcPathLength();
            _coroutine = Bot.StartCoroutine(GoToPointCoroutine());
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
                const float MIN_DIST_CHANGE_DESTINATION = 0.25f;
                const float MIN_DIST_UPDATE_DESTINATION = 1f;
                // If the place being requested to move to is very close to where we are already moving to, we dont need to change anything.
                if ((Destination.Position - possibleDestination).sqrMagnitude < MIN_DIST_CHANGE_DESTINATION)
                {
                    return true;
                }
                // If the destination is close enough to where the last corner is on the path, update the final destination, but dont recalc the path.
                float distanceFromLastCornerSqr = (GetLastCorner().Position - possibleDestination).sqrMagnitude;
                if (distanceFromLastCornerSqr < MIN_DIST_UPDATE_DESTINATION)
                {
                    //Logger.LogDebug($"Move Destination Updated: [{Time.time}]");

                    UpdateDestination(possibleDestination);
                    return true;
                }
            }
            return false;
        }

        public BotPathCorner GetCurrentCorner()
        {
            return PathCorners[CurrentIndex];
        }

        public BotPathCorner GetLastCorner()
        {
            return PathCorners[PathCorners.Count - 1];
        }

        public void Pause(float duration)
        {
            if (duration > 0f)
            {
                CurrentMoveStatus = EBotMoveStatus.Paused;
                PauseTime = Time.time + duration;
            }
        }

        public void UnPause()
        {
            PauseTime = -1;
        }

        public void Cancel(float delay)
        {
            if (CurrentMoveStatus != EBotMoveStatus.Canceling)
            {
                CurrentMoveStatus = EBotMoveStatus.Canceling;
                CancelTime = Time.time + delay;
            }
        }

        public void Start()
        {
            if (_coroutine == null)
            {
                Util.PrepareBot(Bot, WantToSprint);
                _coroutine = Bot.StartCoroutine(GoToPointCoroutine());
            }
        }

        public void Stop(bool success, string reason = null)
        {
            if (_coroutine != null)
            {
                Bot.StopCoroutine(_coroutine);
                _coroutine = null;
                InvokeCompletion(success, reason);
            }
        }

        private void UpdateDestination(Vector3 destination)
        {
            if (!_destinationIsLastCorner)
            {
                PathPoints.RemoveAt(PathPoints.Count - 1);
                PathCorners.RemoveAt(PathCorners.Count - 1);
                BotPathCorner destinationCorner = new(GetLastCorner().Position, destination, EBotCornerType.Destination, PathCorners.Count);
                PathPoints.Add(destination);
                PathCorners.Add(destinationCorner);
                Destination = destinationCorner;
                PathLength = PathCorners.CalcPathLength();
                return;
            }
            else if ((destination - GetLastCorner().Position).sqrMagnitude > 0.1f)
            {
                BotPathCorner destinationCorner = new(GetLastCorner().Position, destination, EBotCornerType.Destination, PathCorners.Count);
                PathPoints.Add(destination);
                PathCorners.Add(destinationCorner);
                Destination = destinationCorner;
                _destinationIsLastCorner = false;
                PathLength = PathCorners.CalcPathLength();
                return;
            }
        }

        public void InvokeCompletion(bool result, string message = null)
        {
            if (OnPathComplete != null)
            {
                OnPathComplete.Invoke(new OperationResult(result, message));
                OnPathComplete = null;
            }
        }

        public void Dispose()
        {
            Stop(false, "Dispose");
        }

        private IEnumerator GoToPointCoroutine()
        {
            CurrentMoveStatus = EBotMoveStatus.Moving;
            //Logger.LogDebug($"[{BotOwner.name}]: Move Started at time [{Time.time}]");
            positionMoving = true;
            bool canTryVault = Bot.Info.FileSettings.Move.VAULT_TOGGLE && GlobalSettingsClass.Instance.Move.VAULT_TOGGLE;

            //WaitForSeconds wait = new(1f / 60f);
            WaitForSeconds wait = null;
            for (int i = 0; i < PathCorners.Count; i++)
            {
                //Logger.LogDebug($"[{BotOwner.name}]: Corner [{i}] Start at time [{Time.time}]");
                StartCorner(i);
                while (CheckContinueMove())
                {
                    yield return InteractWithDoors();
                    if (AwaitUnpause())
                    {
                        yield return wait;
                        continue;
                    }

                    if (!Canceling)
                        CurrentMoveStatus = EBotMoveStatus.Moving;

                    MoveToCurrentCorner(out bool recalcPath, canTryVault);
                    if (recalcPath)
                    {
                        PathRecalcRequested = true;
                        yield break;
                    }
                    if (CurrentCornerDistanceSqr <= ReachDistance())
                        break;
                    yield return wait;
                }
                CompleteCorner(i);
                if (Canceling && CancelTime < Time.time)
                {
                    break;
                }
                //Logger.LogDebug($"[{BotOwner.name}]: Corner [{i}] Complete at time [{Time.time}]");
            }
            Stop(true, "complete");
            //Logger.LogDebug($"[{BotOwner.name}]: Complete Move Time: [{Time.time}]");
        }

        private float ReachDistance()
        {
            if (OnLastCorner) return DestinationReachDistance;
            return WantToSprint ? SAINPlugin.LoadedPreset.GlobalSettings.Move.BotSprintCornerReachDist : SAINPlugin.LoadedPreset.GlobalSettings.Move.BotWalkCornerReachDist;
        }

        private void StartCorner(int i)
        {
            CurrentIndex = i;
            BotPathCorner startedCorner = PathCorners[i];
            startedCorner.TimeStarted = Time.time;
            startedCorner.Status = EBotCornerStatus.Active;
            PathCorners[i] = startedCorner;
        }

        private void CompleteCorner(int i)
        {
            BotPathCorner completeCorner = PathCorners[i];
            completeCorner.TimeComplete = Time.time;
            completeCorner.Status = EBotCornerStatus.Used;
            PathCorners[i] = completeCorner;
        }

        private bool CheckContinueMove()
        {
            if (Canceling && CancelTime < Time.time)
            {
                return false;
            }
            return Bot != null && Bot.SAINLayersActive && Moving;
        }

        private bool AwaitUnpause()
        {
            if (Paused && CheckPaused())
            {
                if (WantToSprint)
                {
                    Bot.Steering.SteerByPriority(null, true, true);
                    SetSprint(false);
                }
                return true;
            }
            return false;
        }

        private IEnumerator InteractWithDoors()
        {
            //Logger.LogDebug($"[{index}] LookForDoor");
            Vector3 cornerPosition = GetCurrentCorner().Position;
            DoorData doorData = Util.SelectDoor(Bot.Position, Bot.DoorOpener, cornerPosition, out bool opening, out bool kicking);
            if (doorData != null)
            {
                _index++;
                int index = _index;
                Logger.LogDebug($"[{index}] DoorFound [{doorData.Link.Id}]");

                FindMovePositionsAndLookPosition(doorData, opening, out Vector3 doorHandleLook, out Vector3 movePosition);

                EDoorState desiredDoorState = opening ? EDoorState.Open : EDoorState.Shut;
                float startTime = Time.time;
                //while (Time.time - startTime < 3f)
                while (CheckContinueMove())
                {
                    //Logger.LogDebug($"[{index}] Tick");
                    Bot.Mover.Prone.SetProne(false);
                    SetSprint(false);
                    if (doorData.Door.DoorState == desiredDoorState)
                    {
                        Logger.LogDebug($"[{index}] door finish");
                        yield break;
                    }

                    DebugGizmos.DrawLine(Bot.Position + Vector3.up, movePosition + Vector3.up, Color.red, 0.1f, 0.02f, true);
                    DebugGizmos.DrawSphere(movePosition, 0.15f, Color.red, 0.02f, "door move pos");
                    Bot.Mover.MovePlayerCharacterToPoint(movePosition);

                    DebugGizmos.DrawSphere(doorHandleLook, 0.15f, Color.white, 1f / 60f, "door handle");
                    if (Bot.Steering.IsLookingAtPoint(doorHandleLook, out float dot))
                    {
                        DebugGizmos.DrawLine(Bot.Transform.WeaponData.WeaponRoot, doorHandleLook, Color.green, 0.05f, 1f / 60f, true);
                        Logger.LogDebug($"[{index}] looking at point [{dot}]");
                        if (!Bot.DoorOpener.InteractWithDoor(doorData, Bot.Position, kicking))
                        {
                            Logger.LogError($"[{index}] failed to interact");
                            Bot.Steering.Unlock();
                            yield break;
                        }

                        if (kicking)
                        {
                            yield return new WaitForSeconds(2f);
                            yield break;
                        }

                        Bot.Steering.Unlock();
                        yield return new WaitForSeconds(0.5f);

                        Logger.LogDebug($"[{index}] interaction done {desiredDoorState}");
                        if (desiredDoorState == EDoorState.Open && DoorOpener.IsDoorPullOpen(doorData.Door, Bot.Position))
                        {
                            Logger.LogDebug($"[{index}] pull open door");

                            while (CheckContinueMove() && doorData.Door.DoorState != desiredDoorState)
                            {
                                Util.BackupFromOpeningDoor(Bot, doorData);
                                yield return null;
                            }
                            yield break;
                        }
                        yield break;
                    }

                    Logger.LogDebug($"[{index}] look2point [{dot}]");
                    DebugGizmos.DrawLine(Bot.Transform.WeaponData.WeaponRoot, doorHandleLook, Color.red, 0.05f, 1f / 60f, true);
                    
                    Bot.Steering.Lock();
                    Bot.Steering.LookToPoint(doorHandleLook);
                    yield return null;
                }
            }
            Bot.Steering.Unlock();
        }

        private void FindMovePositionsAndLookPosition(DoorData doorData, bool opening, out Vector3 doorHandleLook, out Vector3 movePosition)
        {
            var locations = doorData.MoveLocations;
            Vector3 doorHandleFloor = opening ? locations.DoorHandleOpenFloorPoint : locations.DoorHandleCloseFloorPoint;
            doorHandleLook = opening ? locations.DoorHandleOpenLookPoint : locations.DoorHandleCloseLookPoint;
            if (NavMesh.SamplePosition(doorHandleFloor - (((doorHandleFloor - Bot.Position).normalized) * 0.25f), out var hit, 1f, -1))
            {
                movePosition = hit.position;
            }
            else if (NavMesh.SamplePosition(doorHandleFloor, out hit, 1f, -1))
            {
                movePosition = hit.position;
            }
            else
            {
                movePosition = doorHandleFloor;
            }
        }

        private void SetSprint(bool value)
        {
            if (value)
            {
                Bot.Player.MovementContext.SetTilt(0);
            }
            Bot.Player.EnableSprint(value);
        }

        private CornerMoveData CalculateMoveData(Vector3 botPosition, BotPathCorner activeCorner)
        {
            Vector3 dir = (activeCorner.Position - botPosition);
            Vector3 normal = dir.normalized;
            float sqrMag = dir.sqrMagnitude;
            float dot = Vector3.Dot(normal, activeCorner.DirectionFromPrevious.DirectionNormalized);
            return new() {
                CornerDirectionFromBot = dir,
                CornerDirectionFromBotNormal = normal,
                Dot = dot,
                SqrMagnitude = sqrMag,
                PercentageComplete = activeCorner.GetPercentageOfCornerComplete(sqrMag),
            };
        }

        private void MoveToCurrentCorner(out bool recalcPath, bool canTryVault)
        {
            BotPathCorner activeCorner = this.GetCurrentCorner();
            Enemy enemy = Bot.GoalEnemy;
            Vector3 botPosition = Bot.Transform.Position;

            Vector3 botNavPosition = botPosition;
            var navData = Bot.Transform.NavData;
            if (navData.PlayerNavMeshStatus == EPlayerNavMeshDistance.OnNavMesh)
                botNavPosition = navData.NavMeshPosition;

            CornerMoveData lastMoveData = CurrentCornerMoveData;
            //Logger.LogDebug($"lastMoveData: " +
            //    $"{pathData.CurrentCornerMoveData.Dot}:" +
            //    $"{pathData.CurrentCornerMoveData.SqrMagnitude}");
            CurrentCornerMoveData = CalculateMoveData(botNavPosition, activeCorner);
            //Logger.LogDebug($"CurrentMoveData: " +
            //$"{pathData.CurrentCornerMoveData.Dot}:" +
            //$"{pathData.CurrentCornerMoveData.SqrMagnitude}");

            Util.StopVanillaMover(Bot.BotOwner.Mover);

            Bot.Mover.Prone.SetProne(!WantToSprint && Bot.Mover.Crawling);

            if (SAINPlugin.DebugMode)
                Util.DrawMoverDebug(botPosition, activeCorner.Position);

            if (WantToSprint)
            {
                Util.HandleSprinting(this, Bot.Player.MovementContext, Bot.Transform, enemy, Bot.Player.Physical.Stamina.NormalValue);
            }
            else
            {
                CurrentSprintStatus = EBotSprintStatus.None;
                ShallSprintNow = false;
            }
            SetSprint(ShallSprintNow);
            if (ShallSprintNow)
            {
                Bot.Mover.SetTargetMoveSpeed(1f);
                Bot.Mover.SetTargetPose(1f);
            }

            if (Time.time - activeCorner.TimeStarted > 0.5f)
            {
                var currentMoveData = CurrentCornerMoveData;
                if (currentMoveData.Dot < 0.5f)
                {
                    Logger.LogDebug($"recalc from Dot MoveData: " +
                        $"{currentMoveData.CornerDirectionFromBot}:" +
                        $"{currentMoveData.CornerDirectionFromBotNormal}:" +
                        $"{currentMoveData.Dot}:" +
                        $"{currentMoveData.SqrMagnitude}");
                    recalcPath = true;
                    return;
                }
                positionMoving = lastMoveData.SqrMagnitude - CurrentCornerMoveData.SqrMagnitude > 0.001f;
                if (positionMoving)
                {
                    _timeNotMoving = -1f;
                }
                else if (_timeNotMoving < 0)
                {
                    _timeNotMoving = Time.time;
                }
                float timeSinceNoMove = Time.time - _timeNotMoving;
                if (timeSinceNoMove > GlobalSettingsClass.Instance.Move.BotSprintRecalcTime)
                {
                    Logger.LogDebug($"recalc from no move: " +
                        $"{currentMoveData.CornerDirectionFromBot}:" +
                        $"{currentMoveData.CornerDirectionFromBotNormal}:" +
                        $"{currentMoveData.Dot}:" +
                        $"{currentMoveData.SqrMagnitude}");
                    recalcPath = true;
                    return;
                }
                //else if (timeSinceNoMove > _moveSettings.BotSprintTryJumpTime)
                //{
                //    SAINBot.Mover.TryJump();
                //}
                else if (canTryVault && timeSinceNoMove > GlobalSettingsClass.Instance.Move.BotSprintTryVaultTime)
                {
                    Bot.Mover.TryVault();
                }
            }

            Bot.Mover.MovePlayerCharacterToPoint(activeCorner.Position);
            SetPlayerSteering(activeCorner.Position, Bot, enemy);
            recalcPath = false;
        }

        private bool CheckPaused()
        {
            if (CurrentMoveStatus == EBotMoveStatus.Paused)
            {
                if (PauseTime > Time.time)
                {
                    return true;
                }
                CurrentMoveStatus = EBotMoveStatus.Moving;
            }
            return false;
        }

        private void SetPlayerSteering(Vector3 corner, BotComponent bot, Enemy enemy)
        {
            if (WantToSprint)
            {
                if (ShallSteerbyPriority())
                {
                    bot.Steering.Unlock();
                    if (bot.Steering.SteerByPriority(enemy, false, true))
                    {
                        return;
                    }
                }
                bot.Steering.Lock();
                bot.Steering.LookToFloorPoint(corner);
            }
        }

        private bool ShallSteerbyPriority()
        {
            return !ShallSprintNow && CurrentSprintStatus switch {
                EBotSprintStatus.Turning or
                EBotSprintStatus.Running or
                EBotSprintStatus.ArrivingAtDestination or
                EBotSprintStatus.ShortCorner => false,
                _ => true,
            };
        }

        internal void SetWantToSprint(bool value)
        {
            WantToSprint = value;
        }

        private Coroutine _coroutine;
        private float _timeNotMoving;
        private bool _destinationIsLastCorner = false;
        private bool positionMoving;
        protected readonly BotComponent Bot;
        private static int _index = 0;

        private static class Util
        {
            public static void PrepareBot(BotComponent bot, bool sprinting)
            {
                StopVanillaMover(bot.BotOwner.Mover);
                bot.BotOwner.WeaponManager.Stationary.StartMove();
                if (sprinting)
                {
                    bot.Aim.LoseAimTarget();
                    bot.AimDownSightsController.SetADS(false);
                    bot.Mover.Prone.SetProne(false);
                }
            }

            public static EBotCornerType FindCornerType(int count, int i)
            {
                if (i == count - 1) // LastCorner?
                {
                    return EBotCornerType.PathEnd;
                }
                else if (i == count - 2) // Second to last corner?
                {
                    return EBotCornerType.PathEndApproach;
                }
                else
                {
                    return EBotCornerType.PathPoint;
                }
            }

            public static void StopVanillaMover(BotMover botOwnerMover)
            {
                // Make sure the vanilla path finder is NOT active
                if (botOwnerMover.IsMoving || botOwnerMover.HasPathAndNoComplete)
                {
                    botOwnerMover.Stop(); // Backwards sprint / moonwalking fix
                    Logger.LogDebug($"vanilla mover stopped");
                }
            }

            public static void DrawMoverDebug(Vector3 BotPos, Vector3 destination)
            {
                Vector3 debugOffset = Vector3.up * 0.6f;
                DebugGizmos.DrawSphere(destination, 0.2f, Color.white, 0.02f);
                DebugGizmos.DrawLine(destination, destination + debugOffset, Color.white, 0.075f, 0.02f);
                DebugGizmos.DrawLine(destination + debugOffset, BotPos + debugOffset, Color.white, 0.075f, 0.02f);
            }

            private static float FindStartSprintStamina(ESprintUrgency urgency)
            {
                return urgency switch {
                    ESprintUrgency.None or ESprintUrgency.Low => 0.9f,
                    ESprintUrgency.Middle => 0.6f,
                    ESprintUrgency.High => 0.4f,
                    _ => 0.5f,
                };
            }

            private static float FindEndSprintStamina(ESprintUrgency urgency)
            {
                return urgency switch {
                    ESprintUrgency.None or ESprintUrgency.Low => 0.3f,
                    ESprintUrgency.Middle => 0.2f,
                    ESprintUrgency.High => 0.01f,
                    _ => 0.25f,
                };
            }

            public static void HandleSprinting(BotPathData MoveData, MovementContext movementContext, PlayerTransformClass botTransform, Enemy enemy, float staminaNormal)
            {
                if (!SprintCheck1(MoveData, enemy))
                {
                    return;
                }
                // I cant sprint :(
                if (!movementContext.CanSprint)
                {
                    MoveData.CurrentSprintStatus = EBotSprintStatus.CantSprint;
                    MoveData.ShallSprintNow = false;
                    return;
                }

                // We are out of stamina, stop sprinting.
                if (ShallPauseSprintStamina(staminaNormal, MoveData.SprintUrgency))
                {
                    MoveData.CurrentSprintStatus = EBotSprintStatus.NoStamina;
                    MoveData.ShallSprintNow = false;
                    return;
                }

                bool sprintingNow = movementContext.IsSprintEnabled;
                if (CheckArrivingAtDestination(MoveData, sprintingNow))
                {
                    MoveData.CurrentSprintStatus = EBotSprintStatus.ArrivingAtDestination;
                    MoveData.ShallSprintNow = false;
                    return;
                }

                // If we are not looking in the direction of the corner we are moving toward, dont sprint.
                if (FindHorizontalAngleFromLookDir(botTransform.WeaponData.WeaponRoot, MoveData.GetCurrentCorner().Position + enemy.Bot.Steering.WeaponRootOffset, enemy.Bot.PlayerComponent.SmoothController.CurrentControlLookDirection) > 45f)
                {
                    MoveData.CurrentSprintStatus = EBotSprintStatus.Turning;
                    MoveData.ShallSprintNow = false;
                    return;
                }

                // If we arne't already sprinting, and our corner were moving to is far enough away, and I have enough stamina, and the angle isn't too sharp... enable sprint
                if (MoveData.CurrentCornerDistanceSqr > 0.5f && ShallStartSprintStamina(staminaNormal, MoveData.SprintUrgency))
                {
                    MoveData.CurrentSprintStatus = EBotSprintStatus.Running;
                    MoveData.ShallSprintNow = true;
                }
            }

            private static bool SprintCheck1(BotPathData MoveData, Enemy enemy)
            {
                if (MoveData.Canceling)
                {
                    MoveData.CurrentSprintStatus = EBotSprintStatus.Canceling;
                    MoveData.ShallSprintNow = false;
                    return false;
                }
                //if (MoveData.CurrentCorner.ShortCorner)
                //{
                //    MoveData.CurrentSprintStatus = EBotSprintStatus.ShortCorner;
                //    MoveData.ShallSprintNow = false;
                //    return false;
                //}
                if (MoveData.ShallStopSprintWhenSeeEnemy && enemy?.IsVisible == true)
                {
                    MoveData.CurrentSprintStatus = EBotSprintStatus.LookAtEnemyNoSprint;
                    MoveData.ShallSprintNow = false;
                    return false;
                }
                return true;
            }

            private static bool CheckArrivingAtDestination(BotPathData MoveData, bool sprintingNow)
            {
                // We are arriving to our destination, stop sprinting when you get close.
                switch (MoveData.GetCurrentCorner().Type)
                {
                    case EBotCornerType.PathEnd:
                    case EBotCornerType.Destination:
                        break;

                    default:
                        return false;
                }
                float StopSprintDistSqr = GlobalSettingsClass.Instance.Move.BotSprintDistanceToStopSprintDestination.Sqr();
                float maxSprintDist = sprintingNow ? StopSprintDistSqr : StopSprintDistSqr * 1.1f;
                return MoveData.CurrentCornerDistanceSqr <= maxSprintDist;
            }

            private static bool ShallPauseSprintStamina(float stamina, ESprintUrgency urgency) => stamina <= FindEndSprintStamina(urgency);

            private static bool ShallStartSprintStamina(float stamina, ESprintUrgency urgency) => stamina >= FindStartSprintStamina(urgency);

            private static float FindHorizontalAngleFromLookDir(Vector3 start, Vector3 end, Vector3 lookDirection)
            {
                Vector3 direction = end - start;
                lookDirection.y = 0;
                direction.y = 0;
                return Vector3.Angle(lookDirection.normalized, direction.normalized);
            }

            public static void BackupFromOpeningDoor(BotComponent bot, DoorData doorData)
            {
                Vector3 movePosition = doorData.MoveLocations.DoorBackupPoint;
                bot.Mover.MovePlayerCharacterToPoint(movePosition);
                if (!bot.Shoot.ShootAnyVisibleEnemies(bot.GoalEnemy))
                {
                    bot.Steering.SteerByPriority(bot.GoalEnemy);
                }
                DebugGizmos.DrawLine(bot.Position + Vector3.up, movePosition + Vector3.up, Color.red, 0.1f, 0.02f, true);
                DebugGizmos.DrawSphere(movePosition, 0.15f, Color.red, 0.02f);
                Logger.LogDebug("opening back up");
            }

            public static DoorData SelectDoor(Vector3 botPosition, DoorOpener doorOpener, Vector3 cornerPosition, out bool opening, out bool kicking)
            {
                doorOpener.DoorFinder.UpdateDoors(botPosition, cornerPosition);
                List<DoorData> doors = doorOpener.FindDoorsToInteractWith(botPosition);
                Vector3 cornerDir = cornerPosition - botPosition;
                foreach (DoorData data in doors)
                {
                    Collider doorCollider = data?.Door?.Collider;
                    if (doorCollider == null) continue;
                    Ray ray = new() {
                        origin = botPosition + Vector3.up,
                        direction = cornerDir,
                    };
                    if (doorCollider.Raycast(ray, out RaycastHit hit, 1f))
                    {
                        Logger.LogDebug($"hit door");
                        DebugGizmos.DrawLine(ray.origin, hit.point, Color.red, 0.25f, 30f, true);
                        if (data.Door.DoorState == EDoorState.Open)
                        {
                            opening = false;
                            kicking = false;
                            return data;
                        }
                        if (data.Door.DoorState == EDoorState.Shut)
                        {
                            opening = true;
                            kicking = false;
                            return data;
                        }
                        continue;
                    }
                }
                opening = false;
                kicking = false;
                return null;
            }

            public static float ReachDist(bool sprinting)
            {
                return sprinting ? SAINPlugin.LoadedPreset.GlobalSettings.Move.BotSprintCornerReachDist : SAINPlugin.LoadedPreset.GlobalSettings.Move.BotWalkCornerReachDist;
            }
        }
    }
}