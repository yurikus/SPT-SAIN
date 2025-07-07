using EFT;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Mover
{

    public class SAINSprint(BotComponent sain) : BotBase(sain)
    {
        public bool Running => _runToPointCoroutine != null;

        public bool Canceling { get; private set; }

        public void CancelRun(float afterTime = -1f)
        {
            if (Running)
            {
                if (afterTime <= 0)
                {
                    StopRunCoroutine();
                    return;
                }
                if (!Canceling)
                {
                    Canceling = true;
                    Bot.StartCoroutine(CancelRunAfterTime(afterTime));
                }
            }
        }

        private void StopRunCoroutine()
        {
            if (!Running)
            {
                return;
            }
            Canceling = false;
            Bot.StopCoroutine(_runToPointCoroutine);
            _runToPointCoroutine = null;
            Bot.Mover.Sprint(false);
            _path.Clear();
        }

        private IEnumerator CancelRunAfterTime(float afterTime)
        {
            yield return new WaitForSeconds(afterTime);
            StopRunCoroutine();
        }

        public bool RunToPointByWay(NavMeshPath way, ESprintUrgency urgency, bool stopSprintEnemyVisible, bool checkSameWay = true, System.Action callback = null)
        {
            if (!GetLastCorner(way, out Vector3 point))
            {
                return false;
            }
            ShallStopSprintWhenSeeEnemy = stopSprintEnemyVisible;
            if (checkSameWay && IsPointSameWay(point))
            {
                return true;
            }
            StartRun(way, point, urgency, callback);
            return true;
        }

        private bool GetLastCorner(NavMeshPath way, out Vector3 result)
        {
            Vector3[] corners = way?.corners;
            if (corners == null)
            {
                result = Vector3.zero;
                return false;
            }
            if (way.status != NavMeshPathStatus.PathComplete)
            {
                result = Vector3.zero;
                return false;
            }

            Vector3? last = corners.LastElement();
            if (last == null)
            {
                result = Vector3.zero;
                return false;
            }

            result = last.Value;
            return true;
        }

        public bool RunToPoint(Vector3 point, ESprintUrgency urgency, bool stopSprintEnemyVisible, bool checkSameWay = true, System.Action callback = null)
        {
            if (checkSameWay && IsPointSameWay(point))
            {
                return true;
            }

            if (!Bot.Mover.CanGoToPoint(point, out NavMeshPath path))
            {
                return false;
            }
            ShallStopSprintWhenSeeEnemy = stopSprintEnemyVisible;
            StartRun(path, point, urgency, callback);
            return true;
        }

        private bool ShallStopSprintWhenSeeEnemy;

        private bool IsPointSameWay(Vector3 point, float minDistSqr = 0.5f)
        {
            return Running && (LastRunDestination - point).sqrMagnitude < minDistSqr;
        }

        private void StartRun(NavMeshPath path, Vector3 point, ESprintUrgency urgency, System.Action callback)
        {
            StopRunCoroutine();
            Bot.Aim.LoseAimTarget();
            LastRunDestination = point;
            CurrentPath = path;
            _lastUrgency = urgency;
            _runToPointCoroutine = Bot.StartCoroutine(RunToPointCoroutine(path.corners, urgency, callback));
        }

        private float _timeStartRun;

        private ESprintUrgency _lastUrgency;

        public NavMeshPath CurrentPath;

        public bool RecalcPath()
        {
            return RunToPoint(LastRunDestination, _lastUrgency, false);
        }

        public Vector3 LastRunDestination { get; private set; }

        private Coroutine _runToPointCoroutine;

        public EBotSprintStatus CurrentRunStatus { get; private set; }

        public Vector3 CurrentCornerDestination()
        {
            if (_path.Count <= _currentIndex)
            {
                return Vector3.zero;
            }
            return _path[_currentIndex];
        }

        private int _currentIndex = 0;

        private IEnumerator RunToPointCoroutine(Vector3[] corners, ESprintUrgency urgency, System.Action callback = null)
        {
            _path.Clear();
            _path.AddRange(corners);

            isShortCorner = false;
            _timeStartCorner = Time.time;
            positionMoving = true;
            _timeNotMoving = -1f;
            _timeStartRun = Time.time;

            Bot.AimDownSightsController.SetADS(false);
            BotOwner.Mover.Stop();
            _currentIndex = 1;

            // First step, look towards the path we want to run
            //yield return firstTurn(path.corners[1]);

            // Start running!
            yield return RunPath(urgency);

            callback?.Invoke();

            CurrentRunStatus = EBotSprintStatus.None;
            StopRunCoroutine();
        }

        private readonly List<Vector3> _path = new();

        private void MoveToNextCorner()
        {
            if (TotalCorners() > _currentIndex)
            {
                CheckCornerLength();
                _currentIndex++;
                Vector3 currentCorner = _path[_currentIndex];
            }
        }

        private void CheckCornerLength()
        {
            Vector3 current = _path[_currentIndex];
            Vector3 next = _path[_currentIndex + 1];
            isShortCorner = (current - next).magnitude < 0.25f;
            _timeStartCorner = Time.time;
        }

        private float _timeStartCorner;

        private int TotalCorners()
        {
            return _path.Count - 1;
        }

        private Vector3 LastCorner()
        {
            int count = _path.Count;
            if (count == 0)
            {
                return Vector3.zero;
            }
            return _path[count - 1];
        }

        private static MoveSettings _moveSettings => SAINPlugin.LoadedPreset.GlobalSettings.Move;

        private IEnumerator RunPath(ESprintUrgency urgency)
        {
            int total = TotalCorners();
            for (int i = 1; i <= total; i++)
            {
                // Track distance to target corner in the path.
                float distToCurrent = float.MaxValue;
                while (distToCurrent > _moveSettings.BotSprintCornerReachDist)
                {
                    Vector3 BotPos = BotPosition;
                    //if (BotOwner.Mover.IsMoving)
                    //{
                    //    BotOwner.Mover.Stop(); // Backwards sprint / moonwalking fix
                    //}
                    Vector3 destination = CurrentCornerDestination();
                    distToCurrent = (destination - BotPos).sqrMagnitude;
                    DistanceToCurrentCorner = distToCurrent;

                    if (SAINPlugin.DebugMode)
                    {
                        //DebugGizmos.Sphere(current, 0.1f);
                        //DebugGizmos.Line(current, Bot.Position, 0.1f, 0.1f);
                    }

                    // Start or stop sprinting with a buffer
                    HandleSprinting(BotPos, urgency);

                    if (!Bot.DoorOpener.Interacting &&
                        !Bot.DoorOpener.BreachingDoor)
                    {
                        TrackMovement(BotPos);
                        float timeSinceNoMove = timeSinceNotMoving;
                        if (timeSinceNoMove > _moveSettings.BotSprintRecalcTime && Time.time - _timeStartRun > 2f)
                        {
                            RecalcPath();
                            yield break;
                        }
                        //else if (timeSinceNoMove > _moveSettings.BotSprintTryJumpTime)
                        //{
                        //    SAINBot.Mover.TryJump();
                        //}
                        else if (Bot.Info.FileSettings.Move.VAULT_TOGGLE
                            && GlobalSettingsClass.Instance.Move.VAULT_TOGGLE
                            && timeSinceNoMove > _moveSettings.BotSprintTryVaultTime)
                        {
                            Bot.Mover.TryVault();
                        }

                        Move((destination - Bot.Position).normalized);
                    }

                    Steer(destination);
                    yield return null;
                }
                MoveToNextCorner();
            }
        }

        private bool isShortCorner;
        public float DistanceToCurrentCorner { get; private set; }

        private float FindStartSprintStamina(ESprintUrgency urgency)
        {
            switch (urgency)
            {
                case ESprintUrgency.None:
                case ESprintUrgency.Low:
                    return 0.75f;

                case ESprintUrgency.Middle:
                    return 0.5f;

                case ESprintUrgency.High:
                    return 0.2f;

                default:
                    return 0.5f;
            }
        }

        private float FindEndSprintStamina(ESprintUrgency urgency)
        {
            switch (urgency)
            {
                case ESprintUrgency.None:
                case ESprintUrgency.Low:
                    return 0.4f;

                case ESprintUrgency.Middle:
                    return 0.2f;

                case ESprintUrgency.High:
                    return 0.01f;

                default:
                    return 0.25f;
            }
        }

        private bool ShallLookAtEnemy()
        {
            return ShallStopSprintWhenSeeEnemy && Bot.Enemy?.IsVisible == true;
        }

        private void HandleSprinting(Vector3 BotPos, ESprintUrgency urgency)
        {
            // I cant sprint :(
            if (!Player.MovementContext.CanSprint)
            {
                CurrentRunStatus = EBotSprintStatus.CantSprint;
                return;
            }

            if (Canceling)
            {
                CurrentRunStatus = EBotSprintStatus.Canceling;
                Bot.Mover.EnableSprintPlayer(false);
                return;
            }

            if (ShallLookAtEnemy())
            {
                CurrentRunStatus = EBotSprintStatus.LookAtEnemyNoSprint;
                Bot.Mover.EnableSprintPlayer(false);
                return;
            }

            if (isShortCorner)
            {
                CurrentRunStatus = EBotSprintStatus.ShortCorner;
                Bot.Mover.EnableSprintPlayer(false);
                return;
            }

            bool sprintingNow = Player.IsSprintEnabled;
            if (sprintingNow)
            {
                if (Bot.IsCheater)
                {
                    Player.MovementContext.SprintSpeed = 50f;
                }
                else if (_moveSettings.EditSprintSpeed)
                {
                    Player.MovementContext.SprintSpeed = 1.5f;
                }
            }

            // Were messing with a door, dont sprint
            if (Bot.DoorOpener.ShallPauseSprintForOpening())
            {
                CurrentRunStatus = EBotSprintStatus.InteractingWithDoor;
                Bot.Mover.EnableSprintPlayer(false);
                return;
            }

            // We are arriving to our destination, stop sprinting when you get close.
            float StopSprintDistSqr = _moveSettings.BotSprintDistanceToStopSprintDestination.Sqr();
            float LastCornerDistSqr = (LastCorner() - BotPos).sqrMagnitude;
            if (sprintingNow && LastCornerDistSqr <= StopSprintDistSqr)
            {
                Bot.Mover.EnableSprintPlayer(false);
                CurrentRunStatus = EBotSprintStatus.ArrivingAtDestination;
                return;
            }
            else if (!sprintingNow && LastCornerDistSqr <= StopSprintDistSqr * 1.1f)
            {
                CurrentRunStatus = EBotSprintStatus.ArrivingAtDestination;
                return;
            }

            float staminaValue = Player.Physical.Stamina.NormalValue;

            // We are out of stamina, stop sprinting.
            if (ShallPauseSprintStamina(staminaValue, urgency))
            {
                Bot.Mover.EnableSprintPlayer(false);
                CurrentRunStatus = EBotSprintStatus.NoStamina;
                return;
            }

            // We are approaching a sharp corner, or we are currently not looking in the direction we need to go, stop sprinting
            if (ShallPauseSprintAngle())
            {
                Bot.Mover.EnableSprintPlayer(false);
                CurrentRunStatus = EBotSprintStatus.Turning;
                return;
            }

            // If we arne't already sprinting, and our corner were moving to is far enough away, and I have enough stamina, and the angle isn't too sharp... enable sprint
            if (ShallStartSprintStamina(staminaValue, urgency) &&
                _timeStartCorner + 0.25f < Time.time)
            {
                Bot.Mover.EnableSprintPlayer(true);
                CurrentRunStatus = EBotSprintStatus.Running;
                return;
            }
        }

        private bool ShallPauseSprintStamina(float stamina, ESprintUrgency urgency) => stamina <= FindEndSprintStamina(urgency);

        private bool ShallStartSprintStamina(float stamina, ESprintUrgency urgency) => stamina >= FindStartSprintStamina(urgency);

        private bool ShallPauseSprintAngle()
        {
            Vector3? currentCorner = this.CurrentCornerDestination();
            return currentCorner != null && CheckShallPauseSprintFromTurn(currentCorner.Value, _moveSettings.BotSprintCurrentCornerAngleMax);
        }

        private bool CheckShallPauseSprintFromTurn(Vector3 currentCorner, float angleThresh = 25f)
        {
            return FindAngleFromLook(currentCorner) >= angleThresh;
        }

        private float FindAngleFromLook(Vector3 end)
        {
            Vector3 origin = BotOwner.WeaponRoot.position;
            Vector3 aDir = Bot.LookDirection;
            Vector3 bDir = end - origin;
            aDir.y = 0;
            bDir.y = 0;
            return Vector3.Angle(aDir, bDir);
        }

        private void TrackMovement(Vector3 botPos)
        {
            if (nextCheckPosTime < Time.time)
            {
                nextCheckPosTime = Time.time + _moveSettings.BotSprintNotMovingCheckFreq;
                positionMoving = (botPos - lastCheckPos).sqrMagnitude > _moveSettings.BotSprintNotMovingThreshold;
                if (positionMoving)
                {
                    _timeNotMoving = -1f;
                    lastCheckPos = botPos;
                }
                else if (_timeNotMoving < 0)
                {
                    _timeNotMoving = Time.time;
                }
            }
        }

        private bool positionMoving;
        private Vector3 lastCheckPos;
        private float nextCheckPosTime;
        private float timeSinceNotMoving => positionMoving ? 0f : Time.time - _timeNotMoving;
        private float _timeNotMoving;

        private Vector3 BotPosition {
            get
            {
                Vector3 botPos = Bot.Position;
                if (NavMesh.SamplePosition(botPos, out var hit, 1f, -1))
                {
                    botPos.y = hit.position.y;
                }
                return botPos;
            }
        }

        private float DistanceToCurrentCornerSqr(Vector3 BotPos)
        {
            Vector3 destination = CurrentCornerDestination();
            //Vector3 testPoint = destination + Vector3.up;
            //if (Physics.Raycast(testPoint, Vector3.down, out var hit, 1.5f, LayerMaskClass.HighPolyWithTerrainMask))
            //{
            //    destination = hit.point;
            //}
            return (destination - BotPos).sqrMagnitude;
        }

        private void Steer(Vector3 target)
        {
            if (!Bot.DoorOpener.Interacting)
            {
                if (ShallLookAtEnemy())
                {
                    Bot.Steering.LookToEnemy(Bot.Enemy);
                }
                else if (!ShallSteerbyPriority() || !Bot.Steering.SteerByPriority(Bot.Enemy, false, true))
                {
                    target += Bot.Steering.WeaponRootOffset;
                    Vector3 targetLookDir = (target - Bot.Transform.WeaponRoot);
                    Bot.Steering.LookToDirection(targetLookDir, true);
                }
            }
        }

        private bool ShallSteerbyPriority()
        {
            return CurrentRunStatus switch {
                EBotSprintStatus.Turning or
                EBotSprintStatus.FirstTurn or
                EBotSprintStatus.Running or
                EBotSprintStatus.ShortCorner => false,
                _ => true,
            };
        }

        private void Move(Vector3 direction)
        {
            if (Bot.IsCheater)
            {
                direction *= 10f;
            }
            Player.CharacterController.SetSteerDirection(direction);
            BotOwner.AimingManager.CurrentAiming.Move(Player.Speed);
            if (BotOwner.Mover != null)
            {
                BotOwner.Mover.IsMoving = true;
            }
            Player.Move(FindMoveDirection(direction));
        }

        public Vector2 FindMoveDirection(Vector3 direction)
        {
            Vector3 vector = Quaternion.Euler(0f, 0f, Player.Rotation.x) * new Vector2(direction.x, direction.z);
            return new Vector2(vector.x, vector.y);
        }

        private bool IsSprintEnabled => Player.IsSprintEnabled;
    }

    public enum ESprintUrgency
    {
        None = 0,
        Low = 1,
        Middle = 2,
        High = 3,
    }
}