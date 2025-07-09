using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Helpers;
using SAIN.Models.Enums;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Layers.Combat.Solo
{
    internal class RushEnemyAction : CombatAction, ISAINAction
    {
        public RushEnemyAction(BotOwner bot) : base(bot, nameof(RushEnemyAction))
        {
        }

        public override void Update(CustomLayer.ActionData data)
        {
            this.StartProfilingSample("Update");
            Bot.Mover.SetTargetPose(1f);
            Bot.Mover.SetTargetMoveSpeed(1f);
            updateRushBehavior();
            this.EndProfilingSample();
        }

        private void updateRushBehavior()
        {
            if (!checkHasEnemy())
            {
                Bot.Steering.SteerByPriority(null, true);
            }
            else if (_enemy.InLineOfSight)
            {
                enemyInSight();
            }
            else
            {
                checkUpdateMove();
                checkJump();
            }
        }

        public void Toggle(bool value)
        {
            ToggleAction(value);
        }

        private bool checkHasEnemy()
        {
            _enemy = Bot.Enemy;
            return _enemy != null;
        }

        private void enemyInSight()
        {
            checkJumpEnemyInSight();

            Shoot.ShootAnyVisibleEnemies(_enemy);
            Bot.Mover.Sprint(false);
            Bot.Mover.DogFight.DogFightMove(true, _enemy);

            if (_enemy.IsVisible && _enemy.CanShoot)
            {
                Bot.Steering.SteerByPriority(_enemy);
                return;
            }
            Bot.Steering.LookToEnemy(_enemy);
        }

        private void checkJump()
        {
            if (!Bot.Info.FileSettings.Move.JUMP_TOGGLE || !GlobalSettingsClass.Instance.Move.JUMP_TOGGLE)
            {
                return;
            }
            if (_shallTryJump && TryJumpTimer < Time.time && Bot.Player.IsSprintEnabled)
            {
                //&& Bot.Enemy.Path.PathDistance > 3f
                var corner = _enemy.Path.EnemyCorners.GroundPosition(ECornerType.Last);
                if (corner != null &&
                    (corner.Value - Bot.Position).sqrMagnitude < 1f)
                {
                    TryJumpTimer = Time.time + 3f;
                    Bot.Mover.TryJump();
                }
            }
        }

        private void checkJumpEnemyInSight()
        {
            if (!Bot.Info.FileSettings.Move.JUMP_TOGGLE || !GlobalSettingsClass.Instance.Move.JUMP_TOGGLE)
            {
                return;
            }
            if (_shallTryJump)
            {
                if (_shallBunnyHop)
                {
                    Bot.Mover.TryJump();
                }
                else if (TryJumpTimer < Time.time &&
                        Bot.Player.IsSprintEnabled)
                {
                    TryJumpTimer = Time.time + 3f;
                    if (!_shallBunnyHop
                        && EFTMath.RandomBool(Bot.Info.PersonalitySettings.Rush.BunnyHopChance))
                    {
                        _shallBunnyHop = true;
                    }
                    Bot.Mover.TryJump();
                }
            }
        }

        private void checkUpdateMove()
        {
            if (_updateMoveTime < Time.time)
            {
                if (Bot.Mover.PathFollower.Moving && Bot.Mover.PathFollower.MoveData.Canceling)
                {
                     _updateMoveTime = Time.time + 0.1f;
                    return;
                }
                if (updateMove(_enemy))
                {
                    _updateMoveTime = Time.time + 2f;
                }
                _updateMoveTime = Time.time + 0.1f;
            }
        }

        private Vector3 _lastMovePos;

        private const float CHANGE_MOVE_THRESHOLD = 1f;

        private float TryJumpTimer;

        private bool updateMove(Enemy enemy)
        {
            Vector3? lastKnown = enemy.KnownPlaces.LastKnownPosition;
            if (lastKnown == null)
            {
                return false;
            }

            var sprintController = Bot.Mover.PathFollower;
            float pathDistance = enemy.Path.PathLength;
            if (pathDistance <= 1f && sprintController.Moving)
            {
                return true;
            }
            if (sprintController.Moving && (_lastMovePos - lastKnown.Value).sqrMagnitude < CHANGE_MOVE_THRESHOLD)
            {
                return true;
            }
            _lastMovePos = lastKnown.Value;
            if (pathDistance > BotOwner.Settings.FileSettings.Move.RUN_TO_COVER_MIN && sprintController.RunToPointByWay(enemy.Path.PathToEnemy, SAINComponent.Classes.Mover.ESprintUrgency.High, true))
            {
                return true;
            }
            if (sprintController.Running)
            {
                return true;
            }
            if (Bot.Mover.GoToEnemy(enemy, -1, false, true))
            {
                return true;
            }
            return false;
        }

        private bool _shallBunnyHop = false;
        private float _updateMoveTime = 0f;

        public override void Start()
        {
            checkHasEnemy();
            Toggle(true);

            _shallTryJump = Bot.Info.PersonalitySettings.Rush.CanJumpCorners
                //&& Bot.Decision.CurrentSquadDecision != SquadDecision.PushSuppressedEnemy
                && EFTMath.RandomBool(Bot.Info.PersonalitySettings.Rush.JumpCornerChance);

            _shallBunnyHop = false;
        }

        private Enemy _enemy;
        private bool _shallTryJump = false;

        public override void Stop()
        {
            Toggle(false);
            Bot.Mover.DogFight.ResetDogFightStatus();
            //Bot.Mover.PathWalker.CancelRun(0.25f);
        }
    }
}