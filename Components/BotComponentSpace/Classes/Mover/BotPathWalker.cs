using EFT;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class BotPathWalker(BotComponent sain) : BotBase(sain)
    {
        public bool Moving => _moveToPointCoroutine != null;
        public bool Running => _moveToPointCoroutine != null && _wantToSprint;
        public bool Canceling { get; private set; }
        public Vector3 LastDestination { get; private set; }
        public MoveStatus CurrentMoveStatus { get; private set; }
        public NavMeshPath CurrentPath { get; private set; }
        public float DistanceToCurrentCorner { get; private set; }

        public void Pause(float duration)
        {
            _pauseTime = Time.time + duration;
        }

        private bool _wantToSprint;
        private float _pauseTime = -1;
        private bool _isPaused => _pauseTime > Time.time;

        public void Cancel(float afterTime = -1f)
        {
            if (Moving)
            {
                if (afterTime <= 0)
                {
                    StopMoveCoroutine();
                    return;
                }
                if (!Canceling)
                {
                    Canceling = true;
                    Bot.StartCoroutine(CancelMoveAfterTime(afterTime));
                }
            }
        }

        private void StopMoveCoroutine()
        {
            if (!Moving)
            {
                return;
            }
            _pauseTime = -1;
            Canceling = false;
            Bot.StopCoroutine(_moveToPointCoroutine);
            _moveToPointCoroutine = null;
            Bot.Mover.Sprint(false);
            _path.Clear();
        }

        private IEnumerator CancelMoveAfterTime(float afterTime)
        {
            yield return new WaitForSeconds(afterTime);
            StopMoveCoroutine();
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
                _wantToSprint = true;
                return true;
            }
            StartRun(way, point, urgency, callback);
            return true;
        }

        public bool WalkToPointByWay(NavMeshPath way, bool checkSameWay = true, System.Action callback = null)
        {
            if (!GetLastCorner(way, out Vector3 point))
            {
                return false;
            }
            ShallStopSprintWhenSeeEnemy = false;
            if (checkSameWay && IsPointSameWay(point))
            {
                return true;
            }
            StartWalk(way, point, callback);
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
                _wantToSprint = true;
                Bot.Mover.Prone.SetProne(false);
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

        public bool WalkToPoint(Vector3 point, bool checkSameWay = true, System.Action callback = null)
        {
            if (checkSameWay && IsPointSameWay(point))
            {
                _wantToSprint = false;
                return true;
            }
            if (!Bot.Mover.CanGoToPoint(point, out NavMeshPath path))
            {
                return false;
            }
            ShallStopSprintWhenSeeEnemy = false;
            StartWalk(path, point, callback);
            return true;
        }

        private bool IsPointSameWay(Vector3 point, float minDistSqr = 0.25f)
        {
            return Moving && (LastDestination - point).sqrMagnitude < minDistSqr;
        }

        private void StartRun(NavMeshPath path, Vector3 point, ESprintUrgency urgency, System.Action callback)
        {
            _wantToSprint = true;
            Bot.Mover.Prone.SetProne(false);
            TriggerNewMove(path, point, urgency, callback);
        }

        private void StartWalk(NavMeshPath path, Vector3 point, System.Action callback)
        {
            _wantToSprint = false;
            TriggerNewMove(path, point, ESprintUrgency.None, callback);
        }

        private void TriggerNewMove(NavMeshPath path, Vector3 point, ESprintUrgency urgency, System.Action callback)
        {
            StopMoveCoroutine();
            Bot.Aim.LoseAimTarget();
            LastDestination = point;
            CurrentPath = path;
            _lastUrgency = urgency;
            _moveToPointCoroutine = Bot.StartCoroutine(GoToPointCoRoutine(path.corners, urgency, callback));
        }

        public bool RecalcPath()
        {
            if (_wantToSprint)
                return RunToPoint(LastDestination, _lastUrgency, false);
            return WalkToPoint(LastDestination, false);
        }

        public Vector3 CurrentCornerDestination()
        {
            if (_path.Count <= _currentIndex)
            {
                return Vector3.zero;
            }
            return _path[_currentIndex];
        }

        private IEnumerator GoToPointCoRoutine(Vector3[] corners, ESprintUrgency urgency, System.Action callback = null)
        {
            _path.Clear();
            _path.AddRange(corners);

            isShortCorner = false;
            _timeStartCorner = Time.time;
            positionMoving = true;
            _timeNotMoving = -1f;
            _timeStartRun = Time.time;

            if (_wantToSprint)
                Bot.AimDownSightsController.SetADS(false);
            BotOwner.Mover.Stop();
            _currentIndex = 1;

            // First step, look towards the path we want to run
            //yield return firstTurn(path.corners[1]);

            // Start running!
            yield return ExecutePath(urgency);

            callback?.Invoke();

            CurrentMoveStatus = MoveStatus.None;
            StopMoveCoroutine();
        }

        private void MoveToNextCorner()
        {
            if (TotalCorners() > _currentIndex)
            {
                CheckCornerLength();
                _currentIndex++;
                //Vector3 currentCorner = _path[_currentIndex];
                //OnMoveStarted?.Invoke(currentCorner);
            }
        }

        private void CheckCornerLength()
        {
            Vector3 current = _path[_currentIndex];
            Vector3 next = _path[_currentIndex + 1];
            isShortCorner = (current - next).magnitude < 0.25f;
            _timeStartCorner = Time.time;
        }

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

        private IEnumerator ExecutePath(ESprintUrgency urgency)
        {
            int total = TotalCorners();
            for (int i = 1; i <= total; i++)
            {
                // Track distance to target corner in the path.
                float distToCurrent = float.MaxValue;
                while (distToCurrent > _moveSettings.BotSprintCornerReachDist)
                {
                    while (_isPaused)
                    {
                        if (_wantToSprint)
                        {
                            Bot.Steering.SteerByPriority(null, true, true);
                            Bot.Mover.EnableSprintPlayer(false);
                        }
                        yield return null;
                    }
                    if (BotOwner.Mover.IsMoving)
                    {
                        BotOwner.Mover.Stop(); // Backwards sprint / moonwalking fix
                    }

                    Bot.Mover.Prone.SetProne(Bot.Mover.Crawling);

                    Vector3 BotPos = BotPosition;
                    Vector3 destination = CurrentCornerDestination();

                    if (SAINPlugin.DebugMode)
                    {
                        Vector3 debugOffset = Vector3.up * 0.6f;
                        DebugGizmos.Sphere(destination, 0.2f, Color.white, true, 0.02f);
                        DebugGizmos.Line(destination, destination + debugOffset, Color.white, 0.075f, true, 0.02f);
                        DebugGizmos.Line(destination + debugOffset, BotPos + debugOffset, Color.white, 0.075f, true, 0.02f);
                    }

                    distToCurrent = (destination - BotPos).sqrMagnitude;
                    DistanceToCurrentCorner = distToCurrent;

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

                        Move(destination - Bot.Position);
                    }

                    Steer(destination);
                    yield return null;
                }
                MoveToNextCorner();
            }
        }

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
            if (!_wantToSprint)
            {
                CurrentMoveStatus = MoveStatus.None;
                Bot.Mover.EnableSprintPlayer(false);
                return;
            }

            // I cant sprint :(
            if (!Player.MovementContext.CanSprint)
            {
                CurrentMoveStatus = MoveStatus.CantSprint;
                return;
            }

            if (Canceling)
            {
                CurrentMoveStatus = MoveStatus.Canceling;
                Bot.Mover.EnableSprintPlayer(false);
                return;
            }

            if (ShallLookAtEnemy())
            {
                CurrentMoveStatus = MoveStatus.LookAtEnemyNoSprint;
                Bot.Mover.EnableSprintPlayer(false);
                return;
            }

            if (isShortCorner)
            {
                CurrentMoveStatus = MoveStatus.ShortCorner;
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
                CurrentMoveStatus = MoveStatus.InteractingWithDoor;
                Bot.Mover.EnableSprintPlayer(false);
                return;
            }

            // We are arriving to our destination, stop sprinting when you get close.
            float StopSprintDistSqr = _moveSettings.BotSprintDistanceToStopSprintDestination.Sqr();
            float LastCornerDistSqr = (LastCorner() - BotPos).sqrMagnitude;
            if (sprintingNow && LastCornerDistSqr <= StopSprintDistSqr)
            {
                Bot.Mover.EnableSprintPlayer(false);
                CurrentMoveStatus = MoveStatus.ArrivingAtDestination;
                return;
            }
            else if (!sprintingNow && LastCornerDistSqr <= StopSprintDistSqr * 1.1f)
            {
                CurrentMoveStatus = MoveStatus.ArrivingAtDestination;
                return;
            }

            float staminaValue = Player.Physical.Stamina.NormalValue;

            // We are out of stamina, stop sprinting.
            if (ShallPauseSprintStamina(staminaValue, urgency))
            {
                Bot.Mover.EnableSprintPlayer(false);
                CurrentMoveStatus = MoveStatus.NoStamina;
                return;
            }

            // We are approaching a sharp corner, or we are currently not looking in the direction we need to go, stop sprinting
            if (ShallPauseSprintAngle())
            {
                Bot.Mover.EnableSprintPlayer(false);
                CurrentMoveStatus = MoveStatus.Turning;
                return;
            }

            // If we arne't already sprinting, and our corner were moving to is far enough away, and I have enough stamina, and the angle isn't too sharp... enable sprint
            if (ShallStartSprintStamina(staminaValue, urgency) &&
                _timeStartCorner + 0.25f < Time.time)
            {
                Bot.Mover.EnableSprintPlayer(true);
                CurrentMoveStatus = MoveStatus.Running;
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
            if (_wantToSprint && !Bot.DoorOpener.Interacting)
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
            return CurrentMoveStatus switch {
                MoveStatus.Turning or
                MoveStatus.FirstTurn or
                MoveStatus.Running or
                MoveStatus.ShortCorner => false,
                _ => true,
            };
        }

        private void Move(Vector3 direction)
        {
            if (Bot.IsCheater)
            {
                direction *= 10f;
            }
            Bot.Mover.MovePlayerCharacter(direction);
        }

        public Vector2 FindMoveDirection(Vector3 direction)
        {
            Vector3 vector = Quaternion.Euler(0f, 0f, Player.Rotation.x) * new Vector2(direction.x, direction.z);
            return new Vector2(vector.x, vector.y);
        }

        private Coroutine _moveToPointCoroutine;
        private float _timeStartRun;
        private ESprintUrgency _lastUrgency;
        private int _currentIndex = 0;
        private bool ShallStopSprintWhenSeeEnemy;
        private readonly List<Vector3> _path = new();
        private float _timeStartCorner;
        private static MoveSettings _moveSettings => SAINPlugin.LoadedPreset.GlobalSettings.Move;
        private bool isShortCorner;
        private bool positionMoving;
        private Vector3 lastCheckPos;
        private float nextCheckPosTime;
        private float timeSinceNotMoving => positionMoving ? 0f : Time.time - _timeNotMoving;
        private float _timeNotMoving;
    }
}