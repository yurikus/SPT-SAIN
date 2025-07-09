using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Classes.Coverfinder;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.Classes.Mover;
using System.Text;
using UnityEngine;

namespace SAIN.Layers.Combat.Solo.Cover
{
    internal class RunToCoverAction : CombatAction, ISAINAction
    {
        public RunToCoverAction(BotOwner bot) : base(bot, "Run To Cover")
        {
        }

        public void Toggle(bool value)
        {
            ToggleAction(value);
        }

        private bool _runFailed;

        public override void Update(CustomLayer.ActionData data)
        {
            Bot.Mover.SetTargetMoveSpeed(1f);
            Bot.Mover.SetTargetPose(1f);
            checkJumpToCover();
            Enemy enemy = Bot.Enemy;
            UpdateCoverMove(enemy);

            if (!_moveSuccess)
            {
                Bot.Mover.DogFight.DogFightMove(true, enemy);
            }
            if ((!_moveSuccess || !Bot.Mover.PathFollower.Running) &&
                !Shoot.ShootAnyVisibleEnemies(enemy) &&
                !Bot.Steering.SteerByPriority(enemy, false))
            {
                Enemy targetEnemy = Bot.CurrentTarget.CurrentTargetEnemy;
                if (!Shoot.ShootAnyVisibleEnemies(targetEnemy) &&
                    !Bot.Steering.SteerByPriority(targetEnemy, false))
                {
                    Bot.Steering.LookToMovingDirection();
                    //Bot.Steering.LookToLastKnownEnemyPosition(enemy);
                }
            }
        }

        private void UpdateCoverMove(Enemy enemy)
        {
            if (_recalcMoveTimer < Time.time)
            {
                if (FindCoverToMoveTo(out bool sprinting, out CoverPoint coverDestination, false, enemy))
                {
                    _runFailed = false;
                    _moveSuccess = true;
                }
                else if (FindCoverToMoveTo(out sprinting, out coverDestination, true, enemy))
                {
                    _moveSuccess = true;
                    _runFailed = true;
                }
                else
                {
                    Bot.Mover.EnableSprintPlayer(false);
                    _moveSuccess = false;
                    _runFailed = true;
                }

                _sprinting = sprinting;

                if (_moveSuccess)
                {
                    _recalcMoveTimer = Time.time + 2f;
                    _shallJumpToCover = EFTMath.RandomBool(10)
                        && _sprinting
                        && BotOwner.Memory.IsUnderFire
                        && Bot.Info.Profile.IsPMC;

                    Bot.Cover.CoverInUse = coverDestination;
                    _runDestination = coverDestination.Position;
                }
                else
                {
                    _recalcMoveTimer = Time.time + 0.25f;
                    Bot.Cover.CoverInUse = null;
                }
            }
        }

        private void checkJumpToCover()
        {
            if (!Bot.Info.FileSettings.Move.JUMP_TOGGLE || !GlobalSettingsClass.Instance.Move.JUMP_TOGGLE)
            {
                return;
            }
            if (_shallJumpToCover &&
                _moveSuccess &&
                _sprinting &&
                Bot.Player.IsSprintEnabled &&
                _jumpTimer < Time.time)
            {
                CoverPoint coverInUse = Bot.Cover.CoverInUse;
                if (coverInUse != null)
                {
                    float sqrMag = (coverInUse.Position - Bot.Position).sqrMagnitude;
                    if (sqrMag < 3f * 3f && sqrMag > 1.5f * 1.5f)
                    {
                        _jumpTimer = Time.time + 5f;
                        Bot.Mover.TryJump();
                    }
                }
            }
        }

        private bool FindCoverToMoveTo(out bool sprinting, out CoverPoint coverDestination, bool tryWalk, Enemy enemy)
        {
            CoverPoint coverInUse = Bot.Cover.CoverInUse;
            if (TryRunToCoverPoint(coverInUse, out sprinting, tryWalk, enemy))
            {
                coverDestination = coverInUse;
                return true;
            }

            ECombatDecision currentDecision = Bot.Decision.CurrentCombatDecision;

            Bot.Cover.SortPointsByPathDist();

            sprinting = false;
            var coverPoints = Bot.Cover.CoverPoints;

            for (int i = 0; i < coverPoints.Count; i++)
            {
                CoverPoint coverPoint = coverPoints[i];
                if (TryRunToCoverPoint(coverPoint, out sprinting, tryWalk, enemy))
                {
                    coverDestination = coverPoint;
                    return true;
                }
            }
            coverDestination = null;
            return false;
        }

        private bool checkIfPointGoodEnough(CoverPoint coverPoint, float minDot = 0.1f)
        {
            if (coverPoint == null)
            {
                return false;
            }
            if (!coverPoint.CoverData.IsBad)
            {
                return true;
            }
            switch (coverPoint.PathDistanceStatus)
            {
                case CoverStatus.InCover:
                case CoverStatus.CloseToCover:
                    return true;

                default:
                    break;
            }
            Vector3 target = findTarget();
            if (target == Vector3.zero)
            {
                return true;
            }
            float dot = Vector3.Dot(coverPoint.CoverData.ProtectionDirection, (target - coverPoint.Position).normalized);
            return dot > minDot;
        }

        private bool isRunning => Bot.Mover.PathFollower.Running;

        private Vector3 findTarget()
        {
            Vector3 target;
            Vector3? grenade = Bot.Grenade.GrenadeDangerPoint;
            if (grenade != null)
            {
                target = grenade.Value;
            }
            else if (Bot.CurrentTargetPosition != null)
            {
                target = Bot.CurrentTargetPosition.Value;
            }
            else
            {
                target = Vector3.zero;
            }
            return target;
        }

        private bool tooCloseToGrenade(Vector3 pos)
        {
            Vector3? grenadePos = Bot.Grenade.GrenadeDangerPoint;
            if (grenadePos != null &&
                (grenadePos.Value - pos).sqrMagnitude < 3f * 3f)
            {
                return true;
            }
            return false;
        }

        private bool TryRunToCoverPoint(CoverPoint coverPoint, out bool sprinting, bool tryWalk, Enemy enemy)
        {
            bool result = false;
            sprinting = false;

            if (!checkIfPointGoodEnough(coverPoint))
            {
                return false;
            }

            Vector3 destination = coverPoint.Position;

            if (!tryWalk &&
                coverPoint.PathLength >= Bot.Info.FileSettings.Move.RUN_TO_COVER_MIN &&
                Bot.Mover.RunToPoint(destination, getUrgency(), false))
            {
                _wasCrawling = false;
                sprinting = true;
                return true;
            }

            if (tryWalk)
            {
                bool shallCrawl =
                    Bot.Decision.CurrentSelfDecision != ESelfDecision.None &&
                    Bot.Player.MovementContext.CanProne &&
                    coverPoint.StraightDistanceStatus == CoverStatus.FarFromCover &&
                    (_wasCrawling || Bot.Mover.Prone.ShallProneHide(enemy)
                    );

                if (Bot.Mover.GoToPoint(destination, out _, -1f, shallCrawl, false))
                {
                    _wasCrawling = Bot.Mover.Crawling;
                    return true;
                }
                _wasCrawling = Bot.Mover.Crawling;
            }
            return result;
        }

        private bool _wasCrawling;

        private ESprintUrgency getUrgency()
        {
            bool isUrgent =
                BotOwner.Memory.IsUnderFire ||
                Bot.Suppression.IsSuppressed ||
                Bot.Decision.CurrentSelfDecision != ESelfDecision.None;

            return isUrgent ? ESprintUrgency.High : ESprintUrgency.Middle;
        }

        public override void Start()
        {
            Toggle(true);
            _recalcMoveTimer = 0f;
            _shallJumpToCover = false;
            _sprinting = false;
            _moveSuccess = false;
            _runFailed = false;
            _wasCrawling = false;
        }

        public override void Stop()
        {
            Toggle(false);
            Bot.Mover.DogFight.ResetDogFightStatus();
            Bot.Cover.CheckResetCoverInUse();
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Run To Cover Info");

            var pathFinder = Bot.Mover.PathFollower;
            stringBuilder.AppendLabeledValue("Move Success?", $"{_moveSuccess}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Run Success?", $"{!_runFailed}", Color.white, Color.yellow, true);

            DebugOverlay.AddMoveData(Bot, stringBuilder);

            var cover = Bot.Cover;
            stringBuilder.AppendLabeledValue("CoverFinder State", $"{cover.CurrentCoverFinderState}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Cover Count", $"{cover.CoverPoints.Count}", Color.white, Color.yellow, true);

            var _coverDestination = Bot.Cover.CoverInUse;
            if (_coverDestination != null)
            {
                stringBuilder.AppendLine("CoverInUse");
                stringBuilder.AppendLabeledValue("Is Bad?", $"{_coverDestination.CoverData.IsBad}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Straight Status", $"{_coverDestination.StraightDistanceStatus}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Straight Distance", $"{_coverDestination.Distance}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Path Length Status", $"{_coverDestination.PathDistanceStatus}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Path Length", $"{_coverDestination.PathLength}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Path Calc Status", $"{_coverDestination.PathToPoint.status}", Color.white, Color.yellow, true);

                Vector3? lastCorner = _coverDestination.PathToPoint.LastCorner();
                if (lastCorner != null)
                {
                    float difference = (lastCorner.Value - _coverDestination.Position).magnitude;
                    stringBuilder.AppendLabeledValue("Distance To Last Corner", $"{(lastCorner.Value - Bot.Position).magnitude}", Color.white, Color.yellow, true);
                    stringBuilder.AppendLabeledValue("Last Path Corner to Position Difference", $"{(lastCorner.Value - _coverDestination.Position).magnitude}", Color.white, Color.yellow, true);
                }
                stringBuilder.AppendLabeledValue("Height / Value", $"{_coverDestination.CoverHeight} {_coverDestination.HardData.Value}", Color.white, Color.yellow, true);
            }
        }

        private bool _moveSuccess;
        private float _recalcMoveTimer;
        private float _jumpTimer;
        private bool _shallJumpToCover;
        private bool _sprinting;
        private Vector3 _runDestination;
    }
}