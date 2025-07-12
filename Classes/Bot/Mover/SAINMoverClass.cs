using EFT;
using SAIN.Classes.Transform;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class SAINMoverClass : BotComponentClassBase
    {
        public bool Moving => _activePath != null && _activePath.Moving;
        public bool Running => _activePath != null && _activePath.Running;

        public SAINMoverClass(BotComponent sain) : base(sain)
        {
            TickRequirement = ESAINTickState.OnlyNoSleep;
            BlindFire = new BlindFireController(sain);
            SideStep = new SideStepClass(sain);
            Lean = new LeanClass(sain);
            Prone = new ProneClass(sain);
            Pose = new PoseClass(sain);
            DogFight = new DogFight(sain);
        }

        public DogFight DogFight { get; private set; }
        
        public IBotMoveData ActivePath {
            get
            {
                return _activePath;
            }
        }

        private BotPathData _activePath;

        public bool RunToPoint(Vector3 point, bool mustHaveCompletePath = true, float reachDist = -1, ESprintUrgency urgency = ESprintUrgency.Low, bool stopSprintEnemyVisible = false, bool checkSameWay = true, Action<OperationResult> onComplete = null)
        {
            if (checkSameWay && TryUpdatePath(point, true))
            {
                _activePath.ShallStopSprintWhenSeeEnemy = stopSprintEnemyVisible;
                return true;
            }

            if (Bot.Mover.CanGoToPoint(point, out NavMeshPath path, mustHaveCompletePath))
            {
                TriggerNewMove(path.corners, point, true, urgency, onComplete);
                _activePath.ShallStopSprintWhenSeeEnemy = stopSprintEnemyVisible;
                _activePath.PathStatus = path.status;
                return true;
            }
            return false;
        }


        public bool RunToPointByWay(Vector3[] way, ESprintUrgency urgency, bool stopSprintEnemyVisible, bool checkSameWay = true, Action<OperationResult> onComplete = null)
        {
            if (way == null)
                return false;
            if (way.Length <= 1)
                return false;
            Vector3 lastCorner = way[way.Length - 1];
            if (checkSameWay && TryUpdatePath(lastCorner, true))
            {
                _activePath.ShallStopSprintWhenSeeEnemy = stopSprintEnemyVisible;
                return true;
            }
            TriggerNewMove(way, lastCorner, true, urgency, onComplete);
            _activePath.ShallStopSprintWhenSeeEnemy = stopSprintEnemyVisible;
            return true;
        }

        public bool RunToPointByWay(NavMeshPath way, bool mustHaveCompletePath = true, float reachDist = -1, ESprintUrgency urgency = ESprintUrgency.Low, bool stopSprintEnemyVisible = false, bool checkSameWay = true, Action<OperationResult> onComplete = null)
        {
            if (way == null) return false;
            if (way.status == NavMeshPathStatus.PathInvalid) return false;
            if (mustHaveCompletePath && way.status != NavMeshPathStatus.PathComplete) return false;
            if (RunToPointByWay(way?.corners, urgency, stopSprintEnemyVisible, checkSameWay, onComplete))
            {
                _activePath.PathStatus = way.status;
                return true;
            }
            return false;
        }

        public bool WalkToPoint(Vector3 point, bool mustHaveCompletePath = true, float reachDist = -1, bool checkSameWay = true, Action<OperationResult> onComplete = null)
        {
            if (checkSameWay && TryUpdatePath(point, false))
            {
                return true;
            }
            if (Bot.Mover.CanGoToPoint(point, out NavMeshPath path, mustHaveCompletePath))
            {
                TriggerNewMove(path.corners, point, false, ESprintUrgency.None, onComplete);
                _activePath.PathStatus = path.status;
            }
            return false;
        }

        public NavMeshPathStatus _pathStatus;

        public bool WalkToPointByWay(Vector3[] way, bool checkSameWay = true, Action<OperationResult> onComplete = null)
        {
            if (way == null) return false;
            if (way.Length <= 1) return false;
            Vector3 lastCorner = way[way.Length - 1];
            if (checkSameWay && TryUpdatePath(lastCorner, false)) return true;
            TriggerNewMove(way, lastCorner, false, ESprintUrgency.None, onComplete);
            return true;
        }

        public bool WalkToPointByWay(NavMeshPath way, bool mustHaveCompletePath = true, bool checkSameWay = true, Action<OperationResult> onComplete = null)
        {
            if (way == null) return false;
            if (way.status == NavMeshPathStatus.PathInvalid) return false;
            if (mustHaveCompletePath && way.status == NavMeshPathStatus.PathPartial) return false;
            return WalkToPointByWay(way.corners, checkSameWay, onComplete);
        }

        private bool TryUpdatePath(Vector3 point, bool shallSprint)
        {
            if (_activePath != null && _activePath.TryUpdatePath(point))
            {
                _activePath.WantToSprint = shallSprint;
                return true;
            }
            return false;
        }

        private void TriggerNewMove(Vector3[] path, Vector3 point, bool shallSprint, ESprintUrgency urgency, Action<OperationResult> onComplete = null)
        {
            _activePath?.Stop(false, "newMove");
            _activePath = new BotPathData(Bot, Bot.Transform.NavData.NavMeshPosition, point, shallSprint, urgency, path, onComplete);
            _activePath.Start();
        }

        public bool RecalcPath()
        {
            if (_activePath == null) return false;
            if (_activePath.WantToSprint)
                return RunToPoint(_activePath.Destination.Position, true, -1, _activePath.SprintUrgency, false, true, _activePath.OnPathComplete);
            return WalkToPoint(_activePath.Destination.Position, true, -1, false, _activePath.OnPathComplete);
        }

        public override void ManualUpdate()
        {
            if (Crawling && _activePath != null && _activePath.WantToSprint)
            {
                Crawling = false;
            }

            Pose.ManualUpdate();
            Lean.ManualUpdate();
            BlindFire.ManualUpdate();

            if (_activePath != null && _activePath.PathRecalcRequested)
            {
                RecalcPath();
            }
            if (_activePath == null || !_activePath.WantToSprint)
            {
                UpdateStance(Time.time);
            }
            base.ManualUpdate();
        }

        public void MovePlayerCharacterInDirection(Vector3 direction)
        {
            PlayerComponent.SmoothController.SetTargetMoveDirection(direction, Player);
        }

        public void MovePlayerCharacterToPoint(Vector3 point)
        {
            PlayerComponent.SmoothController.SetTargetMovePoint(point, Player);
        }

        private bool CanSetPatrol()
        {
            if (BotOwner.WeaponManager?.Reload?.Reloading == true) return false;
            if (BotOwner.Medecine?.Using == true) return false;

            // Credit to Fontaine, these checks are taken from realism mod's code.
            if (Player.HandsController is Player.FirearmController firearmController &&
                !firearmController.IsAiming &&
                !firearmController.IsInReloadOperation() &&
                !firearmController.IsInventoryOpen() &&
                !firearmController.IsInInteractionStrictCheck() &&
                !firearmController.IsInSpawnOperation() &&
                !firearmController.IsHandsProcessing())
            {
                return true;
            }
            return false;
        }


        public override void Dispose()
        {
            _activePath?.Dispose();
            _activePath = null;
            base.Dispose();
        }

        public BlindFireController BlindFire { get; private set; }
        public SideStepClass SideStep { get; private set; }
        public LeanClass Lean { get; private set; }
        public PoseClass Pose { get; private set; }
        public ProneClass Prone { get; private set; }
        public bool Crawling { get; private set; }

        public bool CrawlToPoint(Vector3 point, bool mustHaveCompletePath = true)
        {
            throw new NotImplementedException();
        }

        public bool CanGoToPoint(Vector3 point, out NavMeshPath path, bool mustHaveCompletePath = true, float navSampleRange = 0.5f)
        {
            var navData = Bot.Transform.NavData;
            if (navData.PlayerNavMeshStatus != EPlayerNavMeshDistance.OnNavMesh)
            {
                path = null;
                return false;
            }
            if (NavMesh.SamplePosition(point, out NavMeshHit targetHit, navSampleRange, -1))
            {
                path = new NavMeshPath();
                if (NavMesh.CalculatePath(navData.NavMeshPosition, targetHit.position, -1, path) && path.corners.Length > 1)
                {
                    if (mustHaveCompletePath
                        && path.status != NavMeshPathStatus.PathComplete)
                    {
                        return false;
                    }
                    return true;
                }
            }
            path = null;
            return false;
        }

        public bool GoToCoverPoint(CoverPoint point, bool sprint, ESprintUrgency urgency = ESprintUrgency.Low, bool stopSprintIfEnemyVisible = false, Action<OperationResult> onComplete = null)
        {
            if (CanGoToCoverPoint(point, Bot.Transform.NavData))
            {
                if (sprint)
                    return RunToPointByWay(point.PathData.Path, true, -1, urgency, stopSprintIfEnemyVisible, true, onComplete);
                return WalkToPointByWay(point.PathData.Path, true, true, onComplete);
            }
            return false;
        }

        private static bool CanGoToCoverPoint(CoverPoint point, PlayerNavData navData)
        {
            if (navData.PlayerNavMeshStatus == EPlayerNavMeshDistance.OnNavMesh)
            {
                PathData coverPathData = point.PathData;
                if (coverPathData.TimeSinceLastUpdated > 1f)
                {
                    return TryRecalcCoverPointPath(point, navData, coverPathData);
                }
                if (coverPathData.Path.status == NavMeshPathStatus.PathComplete)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool TryRecalcCoverPointPath(CoverPoint point, PlayerNavData navData, PathData coverPathData)
        {
            coverPathData.Path.ClearCorners();
            if (NavMesh.CalculatePath(navData.NavMeshPosition, point.Position, -1, coverPathData.Path) &&
                coverPathData.Path.status == NavMeshPathStatus.PathComplete)
            {
                return true;
            }
            return false;
        }

        public bool SetTargetPose(float pose)
        {
            return Pose.SetTargetPose(pose);
        }

        public void SetTargetMoveSpeed(float speed)
        {
            Pose.SetTargetSpeed(speed);
        }

        public void Stop()
        {
            BotOwner?.Mover?.Stop();
            _activePath?.Stop(false, "stopped");
            _activePath = null;
        }

        public void PauseMovement(float forDuration)
        {
            _activePath?.Pause(forDuration);
        }

        public void ResetPath()
        {
            if (_activePath != null) _activePath.PathRecalcRequested = true;
        }

        public bool TryJump()
        {
            if (_nextJumpTime < Time.time &&
                Player.MovementContext?.CanJump == true)
            {
                _nextJumpTime = Time.time + 0.5f;
                Player.MovementContext?.TryJump();
                TimeLastJumped = Time.time;
                return true;
            }
            return false;
        }

        public bool TryVault()
        {
            bool vaulted = Player?.MovementContext?.TryVaulting() == true;
            if (vaulted)
            {
                TimeLastVaulted = Time.time;
            }
            return vaulted;
        }

        public float TimeLastJumped { get; private set; }
        public float TimeLastVaulted { get; private set; }

        private void UpdateStance(float time)
        {
            if (_nextChangeStanceTime < time)
            {
                _nextChangeStanceTime = time + CHANGE_STANCE_INTERVAL;

                MovementContext movementContext = Player.MovementContext;
                if (movementContext != null)
                {
                    LeftStanceController leftStanceController = movementContext.LeftStanceController;

                    bool wantToPatrolStance = !movementContext.IsSprintEnabled && Bot.CurrentTarget.CurrentTargetEnemy == null;
                    if (wantToPatrolStance != movementContext._isInPatrol)
                    {
                        // If we are in left stance and want to patrol, reset back to normal  before setting patrol next update.
                        //if (wantToPatrolStance && leftStance?.LeftStance == true)
                        //{
                        //    leftStance.ToggleLeftStance();
                        //    return;
                        //}
                        movementContext.SetPatrol(wantToPatrolStance && CanSetPatrol());
                        return;
                    }

                    if (leftStanceController != null &&
                        leftStanceController.LeftStance != _wantLeftStance)
                    {
                        leftStanceController.ToggleLeftStance();
                    }
                }
            }
        }

        private void CheckSetBotToNavMesh()
        {
            if (Player.UpdateQueue != EUpdateQueue.Update)
            {
                return;
            }
            // Is the bot currently Moving somewhere?
            if (Moving || BotOwner.Mover.HasPathAndNoComplete)
            {
                _movingTime = Time.time + 1f;
                return;
            }
            if (_movingTime > Time.time)
            {
                return;
            }
            // Did the bot jump recently?
            if (Time.time - TimeLastJumped < _timeAfterJumpVaultReset)
            {
                return;
            }
            // Did the bot vault recently?
            if (Time.time - TimeLastVaulted < _timeAfterJumpVaultReset)
            {
                return;
            }
            // Is the bot currently falling?
            if (!Player.MovementContext.IsGrounded)
            {
                _ungroundedTime = Time.time + 1f;
                return;
            }
            if (_ungroundedTime < Time.time)
            {
            }
        }

        private float _ungroundedTime;
        private float _movingTime;

        private readonly float _timeAfterJumpVaultReset = 1.25f;
        private bool _wantLeftStance => Lean.LeanAngleValue.TargetValue < 0;

        private float _nextJumpTime = 0f;

        private const float CHANGE_STANCE_INTERVAL = 0.5f;
        private float _nextChangeStanceTime;
    }
}