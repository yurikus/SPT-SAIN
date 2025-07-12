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
            _enemy = Bot.GoalEnemy;
            return _enemy != null;
        }

        private void enemyInSight()
        {
            checkJumpEnemyInSight();

            if (Bot.Mover.Moving)
            {
                Bot.Mover.ActivePath.WantToSprint = false;
            }
            Bot.Mover.DogFight.DogFightMove(true, _enemy);

            if (Shoot.ShootAnyVisibleEnemies(_enemy))
            {
                Bot.Steering.SteerByPriority(_enemy, false);
                return;
            }
            if (Bot.Suppression.TrySuppressAnyEnemy(_enemy, Bot.EnemyController.EnemyLists.KnownEnemies))
            {
                Bot.Steering.SteerByPriority(_enemy, false);
            }
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
                NavMeshPath enemyPath = _enemy.Path.PathToEnemy;
                if (enemyPath.corners.Length > 2 && 
                    (enemyPath.corners[enemyPath.corners.Length - 2] - Bot.Position).sqrMagnitude < 1f)
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
                if (Bot.Mover.Moving && Bot.Mover.ActivePath.Canceling)
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

            var sprintController = Bot.Mover;
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
            if (pathDistance > BotOwner.Settings.FileSettings.Move.RUN_TO_COVER_MIN && sprintController.RunToPointByWay(enemy.Path.PathToEnemy, true, -1, SAINComponent.Classes.Mover.ESprintUrgency.High, true))
            {
                return true;
            }
            if (sprintController.Running)
            {
                return true;
            }
            if (Bot.Mover.WalkToPointByWay(enemy.Path.PathToEnemy))
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