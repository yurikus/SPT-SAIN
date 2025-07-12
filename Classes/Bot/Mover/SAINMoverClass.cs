using EFT;
using SAIN.Components;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class SAINMoverClass : BotComponentClassBase
    {
        public SAINMoverClass(BotComponent sain) : base(sain)
        {
            TickRequirement = ESAINTickState.OnlyNoSleep;
            BlindFire = new BlindFireController(sain);
            SideStep = new SideStepClass(sain);
            Lean = new LeanClass(sain);
            Prone = new ProneClass(sain);
            Pose = new PoseClass(sain);
            PathFollower = new BotPathFollowerClass(sain);
            DogFight = new DogFight(sain);
        }
        public DogFight DogFight { get; private set; }
        public BotPathFollowerClass PathFollower { get; private set; }

        public override void ManualUpdate()
        {
            if (Crawling && !PathFollower.Moving)
            {
                Crawling = false;
            }

            //updateStamina();
            Pose.ManualUpdate();
            Lean.ManualUpdate();
            BlindFire.ManualUpdate();

            if (!Player.IsSprintEnabled)
            {
                UpdateStance(Time.time);
            }
            //CheckSetBotToNavMesh();

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
            // Credit to Fontaine, these checks are taken from realism mod's code.
            var firearmController = Bot.Transform.WeaponData.FirearmController;
            if (firearmController != null &&
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

        private void CheckSetBotToNavMesh()
        {
            if (Player.UpdateQueue != EUpdateQueue.Update)
            {
                return;
            }
            // Is the bot currently Moving somewhere?
            if (PathFollower.Moving || BotOwner.Mover.HasPathAndNoComplete)
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
                ResetToNavMesh();
            }
        }

        private float _ungroundedTime;
        private float _movingTime;

        public void ResetToNavMesh()
        {
            if (BotOwner.Mover == null)
            {
                Logger.LogWarning("Bot Mover Null");
                return;
            }
            Vector3 position = Bot.Position;
            if ((_prevLinkPos - position).sqrMagnitude > 0.01f)
            {
                Vector3 castPoint = position + Vector3.up * 0.3f;
                BotOwner.Mover.SetPlayerToNavMesh(castPoint);
                _prevLinkPos = position;

                BotOwner.Mover.PositionOnWayInner = BotOwner.Mover.botOwner_0.Position;
                BotOwner.Mover.LocalAvoidance.DropOffset();
            }
        }

        private Vector3 _prevLinkPos;

        private readonly float _timeAfterJumpVaultReset = 1.25f;

        public override void Dispose()
        {
            PathFollower?.Dispose();
            base.Dispose();
        }

        public BlindFireController BlindFire { get; private set; }
        public SideStepClass SideStep { get; private set; }
        public LeanClass Lean { get; private set; }
        public PoseClass Pose { get; private set; }
        public ProneClass Prone { get; private set; }

        public NavMeshObstacle BotBodyObstacle { get; private set; }

        public bool Crawling { get; private set; }

        public Vector3 CurrentMoveDestination { get; private set; }

        public bool Moving => PathFollower.Moving || BotOwner.Mover?.IsMoving == true || BotOwner.Mover.HasPathAndNoComplete;

        public bool GoToPoint(Vector3 point, out bool calculating, float reachDist = -1f, bool crawl = false, bool slowAtEnd = true, bool mustHaveCompletePath = true)
        {
            calculating = false;
            if (PathFollower.WalkToPoint(point, true, mustHaveCompletePath))
            {
                CurrentPathStatus = NavMeshPathStatus.PathComplete;
                Crawling = crawl && Bot.Info.FileSettings.Move.PRONE_TOGGLE && GlobalSettingsClass.Instance.Move.PRONE_TOGGLE;
                Prone.SetProne(Crawling);
                CurrentMoveDestination = point;
                return true;
            }
            return false;
        }

        public bool RunToPoint(Vector3 point, ESprintUrgency urgency, bool stopSprintEnemyVisible, bool checkSameWay = true, bool mustHaveCompletePath = true)
        {
            if (PathFollower.RunToPoint(point, urgency, stopSprintEnemyVisible, checkSameWay, mustHaveCompletePath))
            {
                CurrentPathStatus = NavMeshPathStatus.PathComplete;
                Crawling = false;
                Prone.SetProne(false);
                CurrentMoveDestination = point;
                return true;
            }
            return false;
        }

        public bool GoToEnemy(Enemy enemy, float reachDist = -1f, bool crawl = false, bool mustHaveCompletePath = true)
        {
            if (enemy == null)
            {
                return false;
            }
            if (reachDist < 0f)
            {
                reachDist = BotOwner.Settings.FileSettings.Move.REACH_DIST;
            }

            var status = enemy.Path.PathToEnemyStatus;
            switch (status)
            {
                case NavMeshPathStatus.PathInvalid:
                    return false;

                case NavMeshPathStatus.PathPartial:
                    if (mustHaveCompletePath)
                    {
                        return false;
                    }
                    break;

                default:
                    break;
            }

            Vector3[] corners = enemy.Path.PathCorners;
            if (corners.Length >= 2)
            {
                if ((corners[corners.Length - 1] - Bot.Position).sqrMagnitude < reachDist)
                {
                    return GoToPoint(enemy.EnemyTransform.Position, out _, reachDist, false, true, false);
                }
                CurrentPathStatus = status;
                return GoToPointByWay(enemy.Path.PathToEnemy, reachDist, crawl);
            }

            CurrentPathStatus = NavMeshPathStatus.PathInvalid;
            return false;
        }

        public bool GoToPointByWay(NavMeshPath Path, float reachDist = -1f, bool crawl = false)
        {
            if (Path == null || Path.corners.Length < 2)
            {
                return false;
            }
            int length = Path.corners.Length;

            if (crawl && Bot.Info.FileSettings.Move.PRONE_TOGGLE && GlobalSettingsClass.Instance.Move.PRONE_TOGGLE)
                Prone.SetProne(true);

            if (PathFollower.WalkToPointByWay(Path))
            {
                CurrentMoveDestination = Path.corners[length - 1];
                return true;
            }
            return false;
        }

        public NavMeshPathStatus CurrentPathStatus { get; private set; } = NavMeshPathStatus.PathInvalid;

        public bool CanGoToPoint(Vector3 point, out NavMeshPath path, bool mustHaveCompletePath = true, float navSampleRange = 1f)
        {
            if (NavMesh.SamplePosition(point, out NavMeshHit targetHit, navSampleRange, -1)
                && NavMesh.SamplePosition(Bot.Transform.Position, out NavMeshHit botHit, navSampleRange, -1))
            {
                path = new NavMeshPath();
                if (NavMesh.CalculatePath(botHit.position, targetHit.position, -1, path) && path.corners.Length > 1)
                {
                    if (path.status == NavMeshPathStatus.PathInvalid)
                    {
                        return false;
                    }

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

        public bool SetTargetPose(float pose)
        {
            return Pose.SetTargetPose(pose);
        }

        public void SetTargetMoveSpeed(float speed)
        {
            Pose.SetTargetSpeed(speed);
        }

        public void StopMove(float delay = 0.1f)
        {
            if (Player?.IsSprintEnabled == true)
            {
                Sprint(false);
            }
            if (delay <= 0f)
            {
                Stop();
                return;
            }
            if (!_stopping && Bot.Mover.PathFollower.Moving)
            {
                _stopping = true;
                Bot.StartCoroutine(StopAfterDelay(delay));
            }
        }

        private IEnumerator StopAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Stop();
        }

        private void Stop()
        {
            Bot?.Mover.PathFollower.Cancel();
            _stopping = false;
        }

        public void PauseMovement(float forDuration)
        {
            if (forDuration > 0)
            {
                PathFollower.Pause(forDuration);
            }
        }

        public void ResetPath(float delay)
        {
            //Bot.StartCoroutine(ResetPathCoroutine(0.2f));
        }

        //private IEnumerator ResetPathCoroutine(float delay)
        //{
        //yield return StopAfterDelay(delay);
        //BotOwner?.Mover?.RecalcWay();
        //}

        private bool _stopping;

        public void Sprint(bool value)
        {
            if (BotOwner == null)
            {
                return;
            }
            EnableSprintPlayer(value);
        }

        public void EnableSprintPlayer(bool value)
        {
            if (BotOwner?.DoorOpener?.Interacting == true)
            {
                value = false;
            }
            if (value)
            {
                Player.MovementContext.SetTilt(0);
            }
            Player.EnableSprint(value);
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
                    LeftStanceController leftStance = movementContext.LeftStanceController;

                    bool wantToPatrolStance = !movementContext.IsSprintEnabled && Bot.CurrentTarget.CurrentTargetEnemy == null;
                    if (wantToPatrolStance != movementContext._isInPatrol)
                    {
                        // If we are in left stance and want to patrol, reset back to normal  before setting patrol next update.
                        if (wantToPatrolStance && leftStance?.LeftStance == true)
                        {
                            leftStance.ToggleLeftStance();
                            return;
                        }
                        movementContext.SetPatrol(wantToPatrolStance && CanSetPatrol());
                        return;
                    }
                    if (leftStance != null && leftStance.LeftStance != _wantLeftStance)
                    {
                        leftStance.ToggleLeftStance();
                    }
                }
            }
        }
        private bool _wantLeftStance => Lean.LeanAngleValue.LastSmoothedValue < 0;

        private float _nextJumpTime = 0f;

        private const float CHANGE_STANCE_INTERVAL = 0.33f;
        private float _nextChangeStanceTime;
    }
}