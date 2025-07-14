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
    public class BotPathDataManual : IBotPathData
    {
        public void TickPath(float deltaTime, float CurrentTime)
        {
            if (Status != EBotMoveStatus.Moving)
            {
                Logger.LogDebug($"path not set to move!");
                return;
            }
            if (CancelRequested && _cancelTime < Time.time)
            {
                Logger.LogDebug($"[{Bot?.name}]:[{Id}]: canceled");
                Stop(false, "canceled");
                return;
            }
            if (Bot == null)
            {
                Logger.LogDebug($"[{Id}]: bot null");
                Stop(false, "bot null");
                return;
            }
            Vector3 botPosition = BotPosition();
            if ((Destination - botPosition).sqrMagnitude < DestinationReachDistance * DestinationReachDistance)
            {
                Logger.LogDebug($"[{Bot.name}]:[{Id}]: Bot Arrived");
                Stop(true, "arrived");
                return;
            }

            BotPathCorner currentCorner = GetCurrentCorner();
            if (InteractWithDoor(botPosition, currentCorner))
            {
                return;
            }

            if (CheckPaused())
            {
                return;
            }

            Move(botPosition);

            if (CheckStuck(true, currentCorner))
            {
                return;
            }

            if (CurrentCornerMoveData.Magnitude <= CornerReachDist())
            {
                if (CurrentIndex >= 0)
                {
                    CompleteCorner(CurrentIndex);
                }
                CurrentIndex++;
                if (CurrentIndex == PathCorners.Count)
                {
                    Stop(true, "complete");
                    return;
                }
                StartCorner(CurrentIndex);
            }
        }

        public event Action<OperationResult, IBotPathData> OnPathComplete;

        public void UpdateSprint(ESprintUrgency urgency)
        {
            SprintUrgency = urgency;
        }

        public void RequestStartSprint(ESprintUrgency urgency, string reason)
        {
            WantToSprint = true;
        }

        public void RequestEndSprint(ESprintUrgency urgency, string reason)
        {
            WantToSprint = false;
        }

        public bool Crawling { get; private set; }

        public void RequestStartCrawl()
        {
            throw new NotImplementedException();
        }

        public void RequestEndCrawl()
        {
            throw new NotImplementedException();
        }

        public bool Moving => Status == EBotMoveStatus.Moving;
        public bool Running => Moving && WantToSprint;
        public int CurrentIndex { get; private set; }
        public CornerMoveData CurrentCornerMoveData { get; private set; }

        public EBotMoveStatus Status { get; private set; }
        public float DestinationReachDistance { get; private set; }
        public Vector3 Destination { get; private set; }
        public NavMeshPath NavMeshPath { get; private set; }

        public List<BotPathCorner> PathCorners { get; } = [];
        public List<Vector3> PathPoints { get; } = [];

        public EBotSprintStatus CurrentSprintStatus { get; private set; }
        public ESprintUrgency SprintUrgency { get; private set; }
        public bool WantToSprint { get; private set; }

        public bool PathRecalcRequested { get; set; }

        public float TimeStarted { get; }

        public float PathLength { get; private set; }

        public Vector3 StartPosition { get; private set; }

        public bool PausedRequested { get; private set; }
        public bool CancelRequested { get; private set; }

        public bool OnLastCorner => CurrentIndex == PathCorners.Count - 1;
        public float CurrentCornerDistanceSqr => CurrentCornerMoveData.SqrMagnitude;
        public float CurrentCornerDistance => CurrentCornerMoveData.Magnitude;

        public NavMeshPathStatus PathStatus { get; set; }

        public int Id { get; private set; }

        public BotPathDataManual(BotComponent bot, Vector3 botNavPosition, Vector3 destination, bool shallSprint, ESprintUrgency urgency, Vector3[] corners, NavMeshPath path, Action<OperationResult, IBotPathData> onComplete)
        {
            Id = _moveID;
            _moveID++;

            Bot = bot;
            NavMeshPath = path;
            StartPosition = botNavPosition;
            TimeStarted = Time.time;
            WantToSprint = shallSprint;
            SprintUrgency = urgency;
            if (onComplete != null) OnPathComplete += onComplete;
            CurrentCornerMoveData = new() {
                Dot = 1,
                SqrMagnitude = float.MaxValue,
            };
            CreatePath(corners);
            Destination = destination;
            PathLength = PathCorners.CalcPathLength();
            Status = EBotMoveStatus.ReadyToMove;
        }

        private static int _moveID = 0;

        private void CreatePath(Vector3[] corners)
        {
            // skip first corner
            for (int i = 1; i < corners.Length; i++)
            {
                Vector3 start = corners[i - 1];
                EBotCornerType type = Util.FindCornerType(corners.Length, i);
                PathCorners.Add(new(start, corners[i], type, i));
            }
        }

        /// <summary>
        /// Checks if the destination is the same as where we already going, or if we can update our path to a modified destination
        /// </summary>
        /// <param name="possibleDestination"></param>
        /// <param name="shallSprint"></param>
        /// <returns></returns>
        public bool TryUpdatePath(Vector3 possibleDestination)
        {
            if (Moving)
            {
                const float MIN_DIST_CHANGE_DESTINATION = 0.5f;
                const float MIN_DIST_UPDATE_DESTINATION = 1f;
                // If the place being requested to move to is very close to where we are already moving to, we dont need to change anything.
                if ((Destination - possibleDestination).sqrMagnitude < MIN_DIST_CHANGE_DESTINATION)
                {
                    return true;
                }
            }
            return false;
        }

        public void RecalcPath()
        {
            throw new NotImplementedException();
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
            if (CancelRequested) return;
            if (duration > 0f)
            {
                duration = Mathf.Max(duration, 0.25f);
                PausedRequested = true;
                _pauseStartTime = Time.time + 0.25f;
                _unpauseTime = Time.time + duration;
            }
        }

        public void UnPause()
        {
            if (PausedRequested)
            {
                _unpauseTime = Time.time;
                PausedRequested = false;
            }
        }

        /// <summary>
        /// Cancel the move after a minimum delay
        /// </summary>
        /// <param name="delay"></param>
        public void Cancel(float delay = 0.25f)
        {
            if (!CancelRequested)
            {
                CancelRequested = true;
                delay = Mathf.Max(delay, 0.33f);
                _cancelTime = Time.time + delay;
            }
        }

        public void Start()
        {
            Util.PrepareBot(Bot, WantToSprint);
            Status = EBotMoveStatus.Moving;
        }

        /// <summary>
        /// Cancel the move immedietly.
        /// </summary>
        private void Stop(bool success, string reason = null)
        {
            if (Status != EBotMoveStatus.Complete)
            {
                Status = EBotMoveStatus.Complete;
                OnPathComplete?.Invoke(new OperationResult(success, reason), this);
                Logger.LogDebug($"[{Bot.name}]:[{Id}]: Complete Move Time: [{Time.time}] Reason: [{reason}]");
            }
        }

        public void Dispose()
        {
            Stop(false, "Dispose");
        }

        private void Move(Vector3 botPosition)
        {
            Util.StopVanillaMover(Bot.BotOwner.Mover);
            Bot.Mover.Prone.SetProne(!WantToSprint && Crawling);

            BotPathCorner activeCorner = this.GetCurrentCorner();
            CurrentCornerMoveData = CalculateMoveData(botPosition, activeCorner);
            Util.DrawMoverDebug(botPosition, activeCorner.Position);

            CurrentSprintStatus = GetSprintStatus(botPosition, activeCorner.Position);
            SetSprint(CurrentSprintStatus == EBotSprintStatus.Running, $"{CurrentSprintStatus}");

            Bot.PlayerComponent.CharacterController.SetTargetMoveDirection(activeCorner.Position - botPosition, Destination, Bot.PlayerComponent);
            SetPlayerSteering(activeCorner.Position, Bot, Bot.GoalEnemy);
        }

        private bool InteractWithDoor(Vector3 botPosition, BotPathCorner activeCorner)
        {
            if (_activeDoorInteraction == null)
            {
                DoorData doorData = Util.SelectDoor(botPosition, Bot.DoorOpener, activeCorner.Position, out bool opening, out bool kicking);
                if (doorData != null)
                {
                    CreateDoorInteraction(botPosition, doorData, opening);
                }
            }
            if (_activeDoorInteraction != null)
            {
                var doorData = _activeDoorInteraction.doorData;
                if (Time.time - _activeDoorInteraction.startTime > 5f)
                {
                    Logger.LogDebug($"[{Bot.name}]:[{Id}]:[{doorData.Link.Id}] door finish - too long");
                    _activeDoorInteraction = null;
                    return false;
                }
                if (_activeDoorInteraction.interactionDone && Time.time - _activeDoorInteraction.timeInteractionDone > 3f)
                {
                    Logger.LogDebug($"[{Bot.name}]:[{Id}]:[{doorData.Link.Id}] door finish - interacted 3 sec ago");
                    _activeDoorInteraction = null;
                    return false;
                }
                if (doorData.Door.DoorState == _activeDoorInteraction.desiredDoorState)
                {
                    Logger.LogDebug($"[{Bot.name}]:[{Id}]:[{doorData.Link.Id}] door finish. door state changed");
                    Bot.Steering.Unlock();
                    _activeDoorInteraction = null;
                    return false;
                }

                Bot.Mover.Prone.SetProne(false);
                SetSprint(false, "door");

                if (!_activeDoorInteraction.lookedAtHandle) _activeDoorInteraction.lookedAtHandle = Bot.Steering.IsLookingAtPoint(_activeDoorInteraction.doorHandleLook, out _);
                if (_activeDoorInteraction.lookedAtHandle)
                {
                    DebugGizmos.DrawLine(Bot.Transform.WeaponData.WeaponRoot, _activeDoorInteraction.doorHandleLook, Color.green, 0.05f, 1f / 60f, true);
                    //Logger.LogDebug($"[{Bot.name}]:[{Id}]:[{doorData.Link.Id}] looking at handle. DoorState: [{doorData.Door.DoorState}] Desired State: [{desiredDoorState}]");
                    if (!_activeDoorInteraction.interactionDone)
                    {
                        _activeDoorInteraction.interactionDone = true;
                        _activeDoorInteraction.timeInteractionDone = Time.time;
                        if (!Bot.DoorOpener.InteractWithDoor(doorData, Bot.Position, false))
                        {
                            Logger.LogError($"[{Bot.name}]:[{Id}]:[{doorData.Link.Id}] failed to interact");
                            Bot.Steering.Unlock();
                            _activeDoorInteraction = null;
                            return false;
                        }
                        Logger.LogDebug($"[{Bot.name}]:[{Id}]:[{doorData.Link.Id}] Interacted");
                    }
                    Bot.Steering.Unlock();
                    if (_activeDoorInteraction.pullOpenDoor) BackUpFromDoor(doorData);
                    return true;
                }
                Logger.LogDebug($"[{Bot.name}]:[{Id}]:[{doorData.Link.Id}] Look To Handle. DoorState: [{doorData.Door.DoorState}] Desired State: [{_activeDoorInteraction.desiredDoorState}]");
                MoveToInteract(_activeDoorInteraction.doorHandleLook, _activeDoorInteraction.movePosition);
                return true;
            }
            Bot.Steering.Unlock();
            return false;
        }

        private void CreateDoorInteraction(Vector3 botPosition, DoorData doorData, bool opening)
        {
            _activeDoorInteraction = new() {
                doorData = doorData
            };
            Logger.LogDebug($"[{Bot.name}]:[{Id}]:[{doorData.Link.Id}] DoorFound");
            var locations = doorData.MoveLocations;
            _activeDoorInteraction.doorHandleFloor = opening ? locations.DoorHandleOpenFloorPoint : locations.DoorHandleCloseFloorPoint;
            _activeDoorInteraction.doorHandleLook = opening ? locations.DoorHandleOpenLookPoint : locations.DoorHandleCloseLookPoint;
            Vector3 movePosition = _activeDoorInteraction.doorHandleFloor + (((Bot.Position - _activeDoorInteraction.doorHandleFloor).normalized) * 1.0f);
            if (NavMesh.SamplePosition(movePosition, out var hit, 1f, -1))
            {
                movePosition = hit.position;
            }
            else if (NavMesh.SamplePosition(_activeDoorInteraction.doorHandleFloor, out hit, 1f, -1))
            {
                movePosition = hit.position;
            }
            else
            {
                movePosition = _activeDoorInteraction.doorHandleFloor;
            }
            _activeDoorInteraction.movePosition = movePosition;
            _activeDoorInteraction.desiredDoorState = opening ? EDoorState.Open : EDoorState.Shut;
            _activeDoorInteraction.startTime = Time.time;
            _activeDoorInteraction.lookedAtHandle = false;
            _activeDoorInteraction.interactionDone = false;
            _activeDoorInteraction.timeInteractionDone = 0;
            _activeDoorInteraction.pullOpenDoor = _activeDoorInteraction.desiredDoorState == EDoorState.Open && DoorOpener.IsDoorPullOpen(doorData.Door, botPosition);
        }

        private void BackUpFromDoor(DoorData doorData)
        {
            Vector3 doorPoint = doorData.MoveLocations.DoorBackupPoint;
            Bot.PlayerComponent.CharacterController.SetTargetMoveDirection(doorPoint - Bot.Position, doorPoint, Bot.PlayerComponent);
            DebugGizmos.DrawLine(Bot.Position + Vector3.up, doorPoint + Vector3.up, Color.yellow, 0.1f, 0.02f, true);
            DebugGizmos.DrawSphere(doorPoint, 0.15f, Color.yellow, 0.02f);
            Bot.Steering.SteerByPriority(Bot.GoalEnemy, false, true);
            //Logger.LogDebug($"[{Bot.name}]:[{Id}]:[{doorData.Link.Id}] Backing up from swing open door");
        }

        private void MoveToInteract(Vector3 doorHandleLook, Vector3 movePosition)
        {
            DebugGizmos.DrawLine(Bot.Transform.WeaponData.WeaponRoot, doorHandleLook, Color.magenta, 0.05f, 1f / 60f, true);
            DebugGizmos.DrawLine(Bot.Position + Vector3.up, movePosition + Vector3.up, Color.magenta, 0.1f, 0.02f, true);
            DebugGizmos.DrawSphere(movePosition, 0.15f, Color.magenta, 0.02f, "door move pos");
            Bot.PlayerComponent.CharacterController.SetTargetMoveDirection(movePosition - Bot.Position, movePosition, Bot.PlayerComponent);
            Bot.Steering.Lock();
            Bot.Steering.LookToPoint(doorHandleLook);
        }

        private DoorInteraction _activeDoorInteraction;

        private sealed class DoorInteraction
        {
            public float startTime;
            public DoorData doorData;
            public EDoorState desiredDoorState;
            public Vector3 doorHandleFloor;
            public Vector3 doorHandleLook;
            public Vector3 movePosition;
            public bool lookedAtHandle;
            public bool interactionDone;
            public float timeInteractionDone;
            public bool pullOpenDoor;
        }

        private bool CheckPaused()
        {
            if (PausedRequested)
            {
                if (_unpauseTime < Time.time)
                {
                    Logger.LogDebug($"[{Bot.name}]:[{Id}]: unpaused");
                    UnPause();
                }
                else if (_pauseStartTime < Time.time)
                {
                    Logger.LogDebug($"[{Bot.name}]:[{Id}]: paused");
                    Bot.Steering.Unlock();
                    Bot.Steering.SteerByPriority(null, true, true);
                    SetSprint(false, "paused");
                    return true;
                }
            }
            return false;
        }

        private bool CheckStuck(bool canTryVault, BotPathCorner activeCorner)
        {
            if (Time.time - _lastCheckStuckTime < 0.5f)
            {
                return false;
            }
            _lastCheckStuckTime = Time.time;
            if (Time.time - activeCorner.TimeStarted > 1f)
            {
                var currentMoveData = CurrentCornerMoveData;
                if (currentMoveData.Dot < 0.5f)
                {
                    Logger.LogDebug($"[{Bot.name}]:[{Id}]: recalc from Dot MoveData: " +
                        $"{currentMoveData.CornerDirectionFromBot}:" +
                        $"{currentMoveData.CornerDirectionFromBotNormal}:" +
                        $"{currentMoveData.Dot}:" +
                        $"{currentMoveData.SqrMagnitude}");
                    PathRecalcRequested = true;
                    return true;
                }

                if (_lastCheckedMoveData.SqrMagnitude - currentMoveData.SqrMagnitude > 0.01f)
                {
                    _timeNotMoving = -1f;
                }
                else if (_timeNotMoving < 0)
                {
                    _timeNotMoving = Time.time;
                }
                _lastCheckedMoveData = currentMoveData;
                if (_timeNotMoving > 0f)
                {
                    float timeSinceNoMove = Time.time - _timeNotMoving;
                    if (timeSinceNoMove > GlobalSettingsClass.Instance.Move.BOT_NOMOVE_RECALC_TIME)
                    {
                        Logger.LogDebug($"[{Bot.name}]:[{Id}]: recalc from no move: " +
                            $"{currentMoveData.CornerDirectionFromBot}:" +
                            $"{currentMoveData.CornerDirectionFromBotNormal}:" +
                            $"{currentMoveData.Dot}:" +
                            $"{currentMoveData.SqrMagnitude}");
                        PathRecalcRequested = true;
                        return true;
                    }
                    else if (timeSinceNoMove > GlobalSettingsClass.Instance.Move.BOT_NOMOVE_TRYJUMP_TIME)
                    {
                        Bot.Mover.TryJump();
                    }
                    else if (canTryVault && timeSinceNoMove > GlobalSettingsClass.Instance.Move.BOT_NOMOVE_TRYVAULT_TIME)
                    {
                        Bot.Mover.TryVault();
                    }
                }
            }
            return false;
        }

        private float _lastCheckStuckTime;
        private float _timeNotMoving;
        private CornerMoveData _lastCheckedMoveData;

        private Vector3 BotPosition()
        {
            var navData = Bot.Transform.NavData;
            Vector3 botPosition = navData.IsOnNavMesh ? navData.NavMeshPosition : Bot.Transform.Position;
            return botPosition;
        }

        private float CornerReachDist()
        {
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

        private void SetSprint(bool value, string reason)
        {
            Bot.PlayerComponent.CharacterController.SetWantToSprint(value);
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
                Magnitude = Mathf.Sqrt(sqrMag),
                PercentageComplete = activeCorner.GetPercentageOfCornerComplete(sqrMag),
            };
        }

        private EBotSprintStatus GetSprintStatus(Vector3 botPosition, Vector3 currentCorner)
        {
            if (WantToSprint)
            {
                if (CancelRequested)
                {
                    return EBotSprintStatus.Canceling;
                }
                if (PausedRequested)
                {
                    return EBotSprintStatus.Pausing;
                }
                // I cant sprint :(
                if (!Bot.Player.MovementContext.CanSprint)
                {
                    return EBotSprintStatus.CantSprint;
                }

                bool sprintingNow = Bot.Player.MovementContext.IsSprintEnabled;
                float stamina = Bot.Player.Physical.Stamina.NormalValue;
                // We are out of stamina, stop sprinting.
                if (sprintingNow && Util.ShallPauseSprintStamina(stamina, SprintUrgency))
                {
                    return EBotSprintStatus.NoStamina;
                }
                // If we are not looking in the direction of the corner we are moving toward, dont sprint.
                Vector3 lookDir = Bot.PlayerComponent.CharacterController.CurrentControlLookDirection;
                if (Util.FindHorizontalAngleFromLookDir(botPosition, currentCorner, lookDir) > 45f)
                {
                    return EBotSprintStatus.Turning;
                }
                if (sprintingNow)
                {
                    return EBotSprintStatus.Running;
                }
                if (CurrentCornerDistance < 0.25f)
                {
                    return EBotSprintStatus.Turning;
                }
                // MoveData.CurrentCornerDistanceSqr > 0.5f &&
                // If we aren't already sprinting, and our corner were moving to is far enough away, and I have enough stamina, and the angle isn't too sharp... enable sprint
                if (Util.ShallStartSprintStamina(stamina, SprintUrgency))
                {
                    //Logger.LogDebug($"start sprint {staminaNormal}");
                    return EBotSprintStatus.Running;
                }
            }
            return EBotSprintStatus.None;
        }

        private void SetPlayerSteering(Vector3 corner, BotComponent bot, Enemy enemy)
        {
            if (!WantToSprint)
            {
                bot.Steering.Unlock();
                return;
            }
            if (CurrentSprintStatus == EBotSprintStatus.None ||
                (CurrentSprintStatus != EBotSprintStatus.Turning && CurrentSprintStatus != EBotSprintStatus.Running))
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

        internal void SetDestinationReachDistance(float reachDist)
        {
            DestinationReachDistance = reachDist;
        }

        private float _unpauseTime;
        private float _pauseStartTime;

        private float _cancelTime;
        protected readonly BotComponent Bot;

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
                Vector3 debugOffset = Vector3.up * 0.25f;
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
                    ESprintUrgency.None or ESprintUrgency.Low => 0.25f,
                    ESprintUrgency.Middle => 0.15f,
                    ESprintUrgency.High => 0.01f,
                    _ => 0.25f,
                };
            }

            public static bool ShallPauseSprintStamina(float stamina, ESprintUrgency urgency) => stamina <= FindEndSprintStamina(urgency);

            public static bool ShallStartSprintStamina(float stamina, ESprintUrgency urgency) => stamina >= FindStartSprintStamina(urgency);

            public static float FindHorizontalAngleFromLookDir(Vector3 start, Vector3 end, Vector3 lookDirection)
            {
                Vector3 direction = end - start;
                lookDirection.y = 0;
                direction.y = 0;
                return Vector3.Angle(lookDirection.normalized, direction.normalized);
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

    public class BotPathData : IBotPathData
    {
        public void ManualUpdate(float deltaTime, float CurrentTime)
        {
        }

        private const float END_SPRINT_INTERVAL = 2f;

        public event Action<OperationResult, IBotPathData> OnPathComplete;

        public void UpdateSprint(ESprintUrgency urgency)
        {
            SprintUrgency = urgency;
        }

        public void RequestStartSprint(ESprintUrgency urgency, string reason)
        {
            if (!_sprintRequested)
            {
                _lastStartSprintTime = Time.time;
                _sprintRequested = true;
            }
        }

        public void RequestEndSprint(ESprintUrgency urgency, string reason)
        {
            if (_sprintRequested)
            {
                if (Time.time - _lastStartSprintTime < END_SPRINT_INTERVAL)
                {
                    return;
                }
                _sprintRequested = false;
            }
        }

        private bool _sprintRequested;
        private float _lastStartSprintTime;

        public bool Crawling { get; private set; }

        public void RequestStartCrawl()
        {
            throw new NotImplementedException();
        }

        public void RequestEndCrawl()
        {
            throw new NotImplementedException();
        }

        public bool Moving => _coroutine != null;
        public bool Running => _coroutine != null && WantToSprint;
        public int CurrentIndex { get; private set; }
        public CornerMoveData CurrentCornerMoveData { get; private set; }

        public EBotMoveStatus Status { get; private set; }
        public float DestinationReachDistance { get; private set; }
        public Vector3 Destination { get; private set; }
        public NavMeshPath NavMeshPath { get; private set; }

        public List<BotPathCorner> PathCorners { get; } = [];
        public List<Vector3> PathPoints { get; } = [];

        public EBotSprintStatus CurrentSprintStatus { get; private set; }
        public ESprintUrgency SprintUrgency { get; private set; }
        public bool WantToSprint { get; private set; }

        public bool Active => _coroutine != null;

        public bool PathRecalcRequested { get; set; }

        public float TimeStarted { get; }

        public float PathLength { get; private set; }

        public Vector3 StartPosition { get; private set; }

        public bool PausedRequested { get; private set; }
        public bool CancelRequested { get; private set; }

        public bool OnLastCorner => CurrentIndex == PathCorners.Count - 1;
        public float CurrentCornerDistanceSqr => CurrentCornerMoveData.SqrMagnitude;
        public float CurrentCornerDistance => CurrentCornerMoveData.Magnitude;

        public NavMeshPathStatus PathStatus { get; set; }

        public int Id { get; private set; }

        public BotPathData(BotComponent bot, Vector3 botNavPosition, Vector3 destination, bool shallSprint, ESprintUrgency urgency, Vector3[] corners, NavMeshPath path, Action<OperationResult, IBotPathData> onComplete)
        {
            Id = _moveID;
            _moveID++;

            Bot = bot;
            NavMeshPath = path;
            StartPosition = botNavPosition;
            TimeStarted = Time.time;
            WantToSprint = shallSprint;
            SprintUrgency = urgency;
            if (onComplete != null) OnPathComplete += onComplete;
            CurrentCornerMoveData = new() {
                Dot = 1,
                SqrMagnitude = float.MaxValue,
            };
            CreatePath(corners);
            Destination = destination;
            PathLength = PathCorners.CalcPathLength();
            Status = EBotMoveStatus.ReadyToMove;
        }

        private static int _moveID = 0;

        private void CreatePath(Vector3[] corners)
        {
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
        }

        /// <summary>
        /// Checks if the destination is the same as where we already going, or if we can update our path to a modified destination
        /// </summary>
        /// <param name="possibleDestination"></param>
        /// <param name="shallSprint"></param>
        /// <returns></returns>
        public bool TryUpdatePath(Vector3 possibleDestination, Action<OperationResult, IBotPathData> onComplete)
        {
            if (Active)
            {
                const float MIN_DIST_CHANGE_DESTINATION = 0.33f;
                const float MIN_DIST_UPDATE_DESTINATION = 1f;
                // If the place being requested to move to is very close to where we are already moving to, we dont need to change anything.
                if ((Destination - possibleDestination).sqrMagnitude < MIN_DIST_CHANGE_DESTINATION)
                {
                    if (onComplete != null)
                        OnPathComplete += onComplete;
                    return true;
                }
                // If the destination is close enough to where the last corner is on the path, update the final destination, but dont recalc the path.
                //float distanceFromLastCornerSqr = (GetLastCorner().Position - possibleDestination).sqrMagnitude;
                //if (distanceFromLastCornerSqr < MIN_DIST_UPDATE_DESTINATION)
                //{
                //    //Logger.LogDebug($"Move Destination Updated: [{Time.time}]");
                //
                //    UpdateDestination(possibleDestination);
                //    return true;
                //}
            }
            return false;
        }

        public void RecalcPath()
        {
            throw new NotImplementedException();
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
            if (CancelRequested) return;
            if (duration > 0f)
            {
                duration = Mathf.Max(duration, 0.25f);
                PausedRequested = true;
                _pauseStartTime = Time.time + 0.25f;
                _unpauseTime = Time.time + duration;
            }
        }

        public void UnPause()
        {
            if (PausedRequested)
            {
                _unpauseTime = Time.time;
                PausedRequested = false;
            }
        }

        /// <summary>
        /// Cancel the move after a minimum delay
        /// </summary>
        /// <param name="delay"></param>
        public void Cancel(float delay = 0.25f)
        {
            if (!CancelRequested)
            {
                CancelRequested = true;
                delay = Mathf.Max(delay, 0.33f);
                _cancelTime = Time.time + delay;
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

        /// <summary>
        /// Cancel the move immedietly.
        /// </summary>
        private void Stop(bool success, string reason = null)
        {
            if (_coroutine != null)
            {
                Bot.StopCoroutine(_coroutine);
                _coroutine = null;
                InvokeCompletion(success, reason);
                Logger.LogDebug($"[{Bot.name}]:[{Id}]: Complete Move Time: [{Time.time}] Reason: [{reason}]");
            }
        }

        //private void UpdateDestination(Vector3 destination)
        //{
        //    if ()
        //    {
        //        PathPoints.RemoveAt(PathPoints.Count - 1);
        //        PathCorners.RemoveAt(PathCorners.Count - 1);
        //        BotPathCorner destinationCorner = new(GetLastCorner().Position, destination, EBotCornerType.Destination, PathCorners.Count);
        //        PathPoints.Add(destination);
        //        PathCorners.Add(destinationCorner);
        //        Destination = destinationCorner;
        //        PathLength = PathCorners.CalcPathLength();
        //        return;
        //    }
        //    else if ((destination - GetLastCorner().Position).sqrMagnitude > 0.1f)
        //    {
        //        BotPathCorner destinationCorner = new(GetLastCorner().Position, destination, EBotCornerType.Destination, PathCorners.Count);
        //        PathPoints.Add(destination);
        //        PathCorners.Add(destinationCorner);
        //        Destination = destinationCorner;
        //        _destinationIsLastCorner = false;
        //        PathLength = PathCorners.CalcPathLength();
        //        return;
        //    }
        //}

        public void InvokeCompletion(bool result, string message = null)
        {
            OnPathComplete?.Invoke(new OperationResult(result, message), this);
        }

        public void Dispose()
        {
            Stop(false, "Dispose");
        }

        private IEnumerator GoToPointCoroutine()
        {
            Status = EBotMoveStatus.Moving;
            Logger.LogDebug($"[{Bot.name}]:[{Id}]: Move Started at time [{Time.time}]");

            //WaitForSeconds wait = new(1f / 60f);
            for (int i = 0; i < PathCorners.Count; i++)
            {
                //Logger.LogDebug($"[{Bot.name}]:[{Id}]: Corner [{i}] Start at time [{Time.time}]");
                StartCorner(i);

                while (true)
                {
                    BotPathCorner activeCorner = this.GetCurrentCorner();
                    Vector3 botPosition = BotPosition();
                    CurrentCornerMoveData = CalculateMoveData(botPosition, activeCorner);
                    if (CurrentCornerMoveData.Magnitude <= ReachDistance()) break;

                    DoorData doorData = Util.SelectDoor(botPosition, Bot.DoorOpener, activeCorner.Position, out bool opening, out bool kicking);
                    if (doorData != null)
                    {
                        Logger.LogDebug($"[{Bot.name}]:[{Id}]:[{doorData.Link.Id}] DoorFound");
                        FindMovePositionsAndLookPosition(doorData, opening, out Vector3 doorHandleLook, out Vector3 movePosition);
                        EDoorState desiredDoorState = opening ? EDoorState.Open : EDoorState.Shut;
                        float startTime = Time.time;
                        bool lookedAtHandle = false;
                        bool interactionDone = false;
                        float timeInteractionDone = 0;
                        Vector3 startPosition = botPosition;
                        bool pullOpenDoor = desiredDoorState == EDoorState.Open && DoorOpener.IsDoorPullOpen(doorData.Door, botPosition);

                        while (true)
                        {
                            if (!CheckContinueMove())
                            {
                                Logger.LogDebug($"[{Bot.name}]:[{Id}]:[{doorData.Link.Id}] door finish - move stopped");
                                break;
                            }
                            if (Time.time - startTime > 5f)
                            {
                                Logger.LogDebug($"[{Bot.name}]:[{Id}]:[{doorData.Link.Id}] door finish - too long");
                                break;
                            }
                            if (interactionDone && Time.time - timeInteractionDone > 3f)
                            {
                                Logger.LogDebug($"[{Bot.name}]:[{Id}]:[{doorData.Link.Id}] door finish - interacted 3 sec ago");
                                break;
                            }
                            if (doorData.Door.DoorState == desiredDoorState)
                            {
                                Logger.LogDebug($"[{Bot.name}]:[{Id}]:[{doorData.Link.Id}] door finish. door state changed");
                                Bot.Steering.Unlock();
                                break;
                            }

                            Bot.Mover.Prone.SetProne(false);
                            SetSprint(false, "door");

                            if (!lookedAtHandle) lookedAtHandle = Bot.Steering.IsLookingAtPoint(doorHandleLook, out _);
                            if (lookedAtHandle)
                            {
                                DebugGizmos.DrawLine(Bot.Transform.WeaponData.WeaponRoot, doorHandleLook, Color.green, 0.05f, 1f / 60f, true);
                                //Logger.LogDebug($"[{Bot.name}]:[{Id}]:[{doorData.Link.Id}] looking at handle. DoorState: [{doorData.Door.DoorState}] Desired State: [{desiredDoorState}]");
                                if (!interactionDone)
                                {
                                    interactionDone = true;
                                    timeInteractionDone = Time.time;
                                    if (!Bot.DoorOpener.InteractWithDoor(doorData, Bot.Position, kicking))
                                    {
                                        Logger.LogError($"[{Bot.name}]:[{Id}]:[{doorData.Link.Id}] failed to interact");
                                        Bot.Steering.Unlock();
                                        break;
                                    }
                                    Logger.LogDebug($"[{Bot.name}]:[{Id}]:[{doorData.Link.Id}] Interacted");
                                    if (kicking)
                                    {
                                        yield return new WaitForSeconds(2f);
                                        Bot.Steering.Unlock();
                                        break;
                                    }
                                }
                                Bot.Steering.Unlock();
                                if (pullOpenDoor) BackUpFromDoor(doorData);
                                yield return null;
                                continue;
                            }
                            Logger.LogDebug($"[{Bot.name}]:[{Id}]:[{doorData.Link.Id}] Look To Handle. DoorState: [{doorData.Door.DoorState}] Desired State: [{desiredDoorState}]");
                            MoveToInteract(doorData, doorHandleLook, movePosition);
                            yield return null;
                        }
                        Bot.Steering.Unlock();
                        //startTime = Time.time;
                        //while (Time.time - startTime < 1f && (startPosition - Bot.Position).sqrMagnitude > 0.5f)
                        //{
                        //    Bot.PlayerComponent.CharacterController.SetTargetMoveDirection(startPosition - Bot.Position, startPosition, Bot.PlayerComponent);
                        //    Bot.Steering.SteerByPriority(Bot.GoalEnemy, false, true);
                        //    DebugGizmos.DrawLine(Bot.Position, startPosition, Color.green, 0.05f, 1f / 60f, true);
                        //    yield return null;
                        //}
                        Logger.LogDebug($"[{Bot.name}]:[{Id}]:[{doorData.Link.Id}] door finish");
                    }

                    if (PausedRequested)
                    {
                        if (_unpauseTime < Time.time)
                        {
                            Logger.LogDebug($"[{Bot.name}]:[{Id}]: unpaused");
                            UnPause();
                        }
                        else if (_pauseStartTime < Time.time)
                        {
                            Logger.LogDebug($"[{Bot.name}]:[{Id}]: paused");
                            Bot.Steering.Unlock();
                            Bot.Steering.SteerByPriority(null, true, true);
                            SetSprint(false, "paused");
                            _timeNotMoving = -1f;
                            yield return null;
                            continue;
                        }
                    }

                    Enemy enemy = Bot.GoalEnemy;
                    //if (!Bot.Transform.NavData.IsOnNavMesh)
                    //{
                    //    if (Time.time - Bot.Transform.NavData.TimeLastOnNavMesh > 2f)
                    //    {
                    //        PathRecalcRequested = true;
                    //        Logger.LogDebug($"[{Bot.name}]:[{Id}]: off navmesh, recalc");
                    //        yield return null;
                    //        continue;
                    //    }
                    //    Bot.PlayerComponent.CharacterController.SetTargetMoveDirection(Bot.Transform.NavData.NavMeshPosition - Bot.Position, Bot.Transform.NavData.NavMeshPosition, Bot.PlayerComponent);
                    //    SetSprint(false, "off navmesh");
                    //    Bot.Steering.Unlock();
                    //    Bot.Steering.SteerByPriority(enemy, false, true);
                    //    Logger.LogDebug($"[{Bot.name}]:[{Id}]: off navmesh, trying to fix");
                    //    _timeNotMoving = -1f;
                    //    yield return null;
                    //    continue;
                    //}

                    Util.StopVanillaMover(Bot.BotOwner.Mover);
                    Bot.Mover.Prone.SetProne(!WantToSprint && Crawling);
                    Util.DrawMoverDebug(BotPosition(), activeCorner.Position);
                    HandleSprinting(enemy);

                    //if (CheckStuck(canTryVault, activeCorner))
                    //{
                    //    yield return null;
                    //    continue;
                    //}

                    Bot.PlayerComponent.CharacterController.SetTargetMoveDirection(activeCorner.Position - BotPosition(), Destination, Bot.PlayerComponent);
                    SetPlayerSteering(activeCorner.Position, Bot, enemy);
                    //Logger.LogDebug($"[{Bot.name}]:[{Id}]: Tick");
                    yield return null;
                }

                CompleteCorner(i);
                //Logger.LogDebug($"[{Bot.name}]: Corner [{i}] Complete at time [{Time.time}]");
            }
            Stop(true, "complete");
        }

        private Vector3 BotPosition()
        {
            var navData = Bot.Transform.NavData;
            Vector3 botPosition = navData.IsOnNavMesh ? navData.NavMeshPosition : Bot.Transform.Position;
            return botPosition;
        }

        private bool CheckStuck(bool canTryVault, BotPathCorner activeCorner)
        {
            if (Time.time - _lastCheckStuckTime < 0.5f)
            {
                return false;
            }
            _lastCheckStuckTime = Time.time;
            if (Time.time - activeCorner.TimeStarted > 1f)
            {
                var currentMoveData = CurrentCornerMoveData;
                if (currentMoveData.Dot < 0.5f)
                {
                    Logger.LogDebug($"[{Bot.name}]:[{Id}]: recalc from Dot MoveData: " +
                        $"{currentMoveData.CornerDirectionFromBot}:" +
                        $"{currentMoveData.CornerDirectionFromBotNormal}:" +
                        $"{currentMoveData.Dot}:" +
                        $"{currentMoveData.SqrMagnitude}");
                    PathRecalcRequested = true;
                    return true;
                }

                if (_lastCheckedMoveData.SqrMagnitude - currentMoveData.SqrMagnitude > 0f)
                {
                    _timeNotMoving = -1f;
                }
                else if (_timeNotMoving < 0)
                {
                    _timeNotMoving = Time.time;
                }
                _lastCheckedMoveData = currentMoveData;
                if (_timeNotMoving > 0f)
                {
                    float timeSinceNoMove = Time.time - _timeNotMoving;
                    if (timeSinceNoMove > GlobalSettingsClass.Instance.Move.BOT_NOMOVE_RECALC_TIME)
                    {
                        Logger.LogDebug($"[{Bot.name}]:[{Id}]: recalc from no move: " +
                            $"{currentMoveData.CornerDirectionFromBot}:" +
                            $"{currentMoveData.CornerDirectionFromBotNormal}:" +
                            $"{currentMoveData.Dot}:" +
                            $"{currentMoveData.SqrMagnitude}");
                        PathRecalcRequested = true;
                        return true;
                    }
                    else if (timeSinceNoMove > GlobalSettingsClass.Instance.Move.BOT_NOMOVE_TRYJUMP_TIME)
                    {
                        Bot.Mover.TryJump();
                    }
                    else if (canTryVault && timeSinceNoMove > GlobalSettingsClass.Instance.Move.BOT_NOMOVE_TRYVAULT_TIME)
                    {
                        Bot.Mover.TryVault();
                    }
                }
            }
            return false;
        }

        private float _lastCheckStuckTime;
        private CornerMoveData _lastCheckedMoveData = new();

        private void BackUpFromDoor(DoorData doorData)
        {
            Vector3 doorPoint = doorData.MoveLocations.DoorBackupPoint;
            Bot.PlayerComponent.CharacterController.SetTargetMoveDirection(doorPoint - Bot.Position, doorPoint, Bot.PlayerComponent);
            DebugGizmos.DrawLine(Bot.Position + Vector3.up, doorPoint + Vector3.up, Color.yellow, 0.1f, 0.02f, true);
            DebugGizmos.DrawSphere(doorPoint, 0.15f, Color.yellow, 0.02f);
            Bot.Steering.SteerByPriority(Bot.GoalEnemy, false, true);
            //Logger.LogDebug($"[{Bot.name}]:[{Id}]:[{doorData.Link.Id}] Backing up from swing open door");
        }

        private void MoveToInteract(DoorData doorData, Vector3 doorHandleLook, Vector3 movePosition)
        {
            DebugGizmos.DrawLine(Bot.Transform.WeaponData.WeaponRoot, doorHandleLook, Color.magenta, 0.05f, 1f / 60f, true);
            DebugGizmos.DrawLine(Bot.Position + Vector3.up, movePosition + Vector3.up, Color.magenta, 0.1f, 0.02f, true);
            DebugGizmos.DrawSphere(movePosition, 0.15f, Color.magenta, 0.02f, "door move pos");
            Bot.PlayerComponent.CharacterController.SetTargetMoveDirection(movePosition - Bot.Position, movePosition, Bot.PlayerComponent);
            Bot.Steering.Lock();
            Bot.Steering.LookToPoint(doorHandleLook);
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
            _timeNotMoving = -1f;
        }

        private void CompleteCorner(int i)
        {
            BotPathCorner completeCorner = PathCorners[i];
            completeCorner.TimeComplete = Time.time;
            completeCorner.Status = EBotCornerStatus.Used;
            PathCorners[i] = completeCorner;
            _timeNotMoving = -1f;
        }

        private bool CheckContinueMove()
        {
            if (CancelRequested && _cancelTime < Time.time)
            {
                Logger.LogDebug($"[{Bot?.name}]:[{Id}]: canceled");
                return false;
            }
            if (Bot == null)
            {
                Logger.LogDebug($"[{Id}]: bot null");
                return false;
            }
            if (!Bot.SAINLayersActive && !Bot.HasEnemy)
            {
                Logger.LogDebug($"[{Bot.name}]:[{Id}]: sain not active");
                return false;
            }
            float reachDist = ReachDistance();
            if ((Destination - Bot.Position).sqrMagnitude < reachDist * reachDist)
            {
                Logger.LogDebug($"[{Bot.name}]:[{Id}]: Bot Arrived");
                return false;
            }
            if ((Destination - Bot.Transform.NavData.NavMeshPosition).sqrMagnitude < reachDist * reachDist)
            {
                Logger.LogDebug($"[{Bot.name}]:[{Id}]: Bot Arrived");
                return false;
            }
            return true;
        }

        private void FindMovePositionsAndLookPosition(DoorData doorData, bool opening, out Vector3 doorHandleLook, out Vector3 movePosition)
        {
            var locations = doorData.MoveLocations;
            Vector3 doorHandleFloor = opening ? locations.DoorHandleOpenFloorPoint : locations.DoorHandleCloseFloorPoint;
            doorHandleLook = opening ? locations.DoorHandleOpenLookPoint : locations.DoorHandleCloseLookPoint;
            movePosition = doorHandleFloor + (((Bot.Position - doorHandleFloor).normalized) * 1.0f);
            if (NavMesh.SamplePosition(movePosition, out var hit, 1f, -1))
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

        private void SetSprint(bool value, string reason)
        {
            if (value != _debugSetSprintVal)
            {
                Logger.LogError($"[{Bot.name}]:[{Id}]: SettingSprint Sprint {reason}");
                _debugSetSprintVal = value;
            }
            Bot.PlayerComponent.CharacterController.SetWantToSprint(value);
        }

        private bool _debugSetSprintVal;

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
                Magnitude = Mathf.Sqrt(sqrMag),
                PercentageComplete = activeCorner.GetPercentageOfCornerComplete(sqrMag),
            };
        }

        private void HandleSprinting(Enemy enemy)
        {
            if (WantToSprint)
            {
                Util.HandleSprinting(this, Bot.Player.MovementContext, Bot.Transform, enemy, Bot.Player.Physical.Stamina.NormalValue);
            }
            else
            {
                CurrentSprintStatus = EBotSprintStatus.None;
                _shallSprintNow = false;
            }
            SetSprint(_shallSprintNow, _shallSprintNow ? $"{CurrentSprintStatus}" : null);
        }

        private void SetPlayerSteering(Vector3 corner, BotComponent bot, Enemy enemy)
        {
            if (!WantToSprint)
            {
                bot.Steering.Unlock();
                return;
            }
            if (!_shallSprintNow &&
                CurrentSprintStatus != EBotSprintStatus.Turning &&
                CurrentSprintStatus != EBotSprintStatus.Running)
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

        internal void SetDestinationReachDistance(float reachDist)
        {
            DestinationReachDistance = reachDist;
        }

        private bool _shallSprintNow;
        private float _unpauseTime;
        private float _pauseStartTime;

        private float _cancelTime;
        private Coroutine _coroutine;
        private float _timeNotMoving;
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
                Vector3 debugOffset = Vector3.up * 0.25f;
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
                    ESprintUrgency.None or ESprintUrgency.Low => 0.25f,
                    ESprintUrgency.Middle => 0.15f,
                    ESprintUrgency.High => 0.01f,
                    _ => 0.25f,
                };
            }

            public static void HandleSprinting(BotPathData MoveData, MovementContext movementContext, PlayerTransformClass botTransform, Enemy enemy, float staminaNormal)
            {
                if (MoveData.CurrentCornerDistance < 0.5f)
                {
                    MoveData.CurrentSprintStatus = EBotSprintStatus.Turning;
                    MoveData._shallSprintNow = false;
                    return;
                }
                if (MoveData.CancelRequested)
                {
                    MoveData.CurrentSprintStatus = EBotSprintStatus.Canceling;
                    MoveData._shallSprintNow = false;
                    return;
                }
                if (MoveData.PausedRequested)
                {
                    MoveData.CurrentSprintStatus = EBotSprintStatus.Pausing;
                    MoveData._shallSprintNow = false;
                    return;
                }
                // I cant sprint :(
                if (!movementContext.CanSprint)
                {
                    MoveData.CurrentSprintStatus = EBotSprintStatus.CantSprint;
                    MoveData._shallSprintNow = false;
                    return;
                }

                bool sprintingNow = movementContext.IsSprintEnabled;
                // We are out of stamina, stop sprinting.
                if (sprintingNow && ShallPauseSprintStamina(staminaNormal, MoveData.SprintUrgency))
                {
                    Logger.LogDebug($"no stam paused {staminaNormal}");
                    MoveData.CurrentSprintStatus = EBotSprintStatus.NoStamina;
                    MoveData._shallSprintNow = false;
                    return;
                }
                // If we are not looking in the direction of the corner we are moving toward, dont sprint.
                Vector3 lookDir = enemy.Bot.PlayerComponent.CharacterController.CurrentControlLookDirection;
                if (FindHorizontalAngleFromLookDir(botTransform.Position, MoveData.GetCurrentCorner().Position, lookDir) > 45f)
                {
                    MoveData.CurrentSprintStatus = EBotSprintStatus.Turning;
                    MoveData._shallSprintNow = false;
                    return;
                }
                if (sprintingNow)
                {
                    MoveData.CurrentSprintStatus = EBotSprintStatus.Running;
                    MoveData._shallSprintNow = true;
                    return;
                }
                if (MoveData.CurrentCornerDistance < 0.25f)
                {
                    return;
                }
                // MoveData.CurrentCornerDistanceSqr > 0.5f &&
                // If we aren't already sprinting, and our corner were moving to is far enough away, and I have enough stamina, and the angle isn't too sharp... enable sprint
                if (ShallStartSprintStamina(staminaNormal, MoveData.SprintUrgency))
                {
                    //Logger.LogDebug($"start sprint {staminaNormal}");
                    MoveData.CurrentSprintStatus = EBotSprintStatus.Running;
                    MoveData._shallSprintNow = true;
                }
                else
                {
                    //Logger.LogDebug($"no stam {staminaNormal} : {MoveData.SprintUrgency}");
                }
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