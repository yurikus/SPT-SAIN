using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.SubComponents.CoverFinder;
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

        public override void Update(CustomLayer.ActionData data)
        {
            Bot.Cover.UpdateCover();
            Bot.Mover.SetTargetMoveSpeed(1f);
            Bot.Mover.SetTargetPose(1f);
            checkJumpToCover();
            Enemy enemy = Bot.GoalEnemy;

            if (!Bot.Mover.Running &&
                !Bot.Steering.SteeringLocked &&
                !Shoot.ShootAnyVisibleEnemies(enemy) &&
                !Bot.Suppression.TrySuppressAnyEnemy(enemy, Bot.EnemyController.EnemyLists.KnownEnemies) &&
                !Bot.Steering.SteerByPriority(enemy, false) && 
                !Bot.Steering.SteerByPriority(Bot.CurrentTarget.CurrentTargetEnemy, false))
            {
                Bot.Steering.LookToMovingDirection();
                //Bot.Steering.LookToLastKnownEnemyPosition(enemy);
            }
        }

        private void checkJumpToCover()
        {
            if (!Bot.Info.FileSettings.Move.JUMP_TOGGLE || !GlobalSettingsClass.Instance.Move.JUMP_TOGGLE)
            {
                return;
            }
            if (_shallJumpToCover &&
                Bot.Mover.Running &&
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

        public override void Start()
        {
            Toggle(true);
                    _shallJumpToCover = EFTMath.RandomBool(10)
                        && BotOwner.Memory.IsUnderFire
                        && Bot.Info.Profile.IsPMC;
        }

        public override void Stop()
        {
            Toggle(false);
            Bot.Mover.DogFight.ResetDogFightStatus();
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Run To Cover Info");

            DebugOverlay.AddMoveData(Bot, stringBuilder);

            var cover = Bot.Cover;
            stringBuilder.AppendLabeledValue("CoverFinder State", $"{cover.CurrentCoverFinderState}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Cover Seeking State", $"{cover.CoverSeekingState}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Cover Count", $"{cover.CoverPoints.Count}", Color.white, Color.yellow, true);

            var _coverDestination = Bot.Cover.CoverPoint_MovingTo;
            stringBuilder.AppendLabeledValue("Cover ID", $"{_coverDestination?.HardData.Id}", Color.white, Color.yellow, true);
            if (_coverDestination != null)
            {
                stringBuilder.AppendLine("Moving To Cover");
                stringBuilder.AppendLabeledValue("Is Bad?", $"{_coverDestination.CoverData.IsBad}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Straight Status", $"{_coverDestination.StraightDistanceStatus}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Straight Distance", $"{_coverDestination.Distance}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Path Length Status", $"{_coverDestination.PathDistanceStatus}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Path Length", $"{_coverDestination.PathData.PathLength}", Color.white, Color.yellow, true);
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

        private float _jumpTimer;
        private bool _shallJumpToCover;
    }
}