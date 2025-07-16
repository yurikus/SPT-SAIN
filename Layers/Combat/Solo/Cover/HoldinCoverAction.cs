using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System.Text;
using UnityEngine;

namespace SAIN.Layers.Combat.Solo.Cover
{
    internal class HoldinCoverAction : CombatAction, ISAINAction
    {
        public HoldinCoverAction(BotOwner bot) : base(bot, nameof(HoldinCoverAction))
        {
        }

        public void Toggle(bool value)
        {
            ToggleAction(value);
        }

        public override void Update(CustomLayer.ActionData data)
        {
            Enemy Enemy = Bot.GoalEnemy;
            Bot.Cover.UpdateCover(Enemy);
            CoverPoint coverInUse = CoverInUse;
            if (coverInUse != null)
            {
                Bot.Cover.TrySetProneConditional(Enemy);
                checkSetProne();
                checkSetLean();
            }
            if (!Shoot.ShootAnyVisibleEnemies(Enemy) && !Bot.Suppression.TrySuppressEnemy(Enemy))
            {
                Bot.Steering.SteerByPriority(Enemy);
            }
        }

        private void checkSetProne()
        {
            if (!Bot.Info.FileSettings.Move.PRONE_TOGGLE || !GlobalSettingsClass.Instance.Move.PRONE_TOGGLE)
            {
                return;
            }
            if (Bot.GoalEnemy != null
                && Bot.Player.MovementContext.CanProne
                && Bot.Player.PoseLevel <= 0.1
                && Bot.GoalEnemy.IsVisible
                && BotOwner.WeaponManager.Reload.Reloading)
            {
                Bot.Mover.Prone.SetProne(true);
            }
        }

        private void checkSetLean()
        {
            if (!Bot.Info.FileSettings.Move.LEAN_INCOVER_TOGGLE
                || !GlobalSettingsClass.Instance.Move.LEAN_INCOVER_TOGGLE
                || Bot.Suppression.IsSuppressed
                || Bot.Decision.CurrentSelfDecision != ESelfDecision.None)
            {
                Bot.Mover.Lean.FastLean(LeanSetting.None);
                CurrentLean = LeanSetting.None;
                return;
            }

            if (CurrentLean != LeanSetting.None && ShallHoldLean())
            {
                Bot.Mover.Lean.FastLean(CurrentLean);
                ChangeLeanTimer = Time.time + 0.66f;
                return;
            }

            if (ChangeLeanTimer < Time.time)
            {
                setLean();
            }
        }

        private void setLean()
        {
            LeanSetting newLean;
            switch (CurrentLean)
            {
                case LeanSetting.Left:
                case LeanSetting.Right:
                    newLean = LeanSetting.None;
                    ChangeLeanTimer = Time.time + Random.Range(0.75f, 4f);
                    break;

                default:
                    newLean = Bot.Mover.Lean.FindLeanFromBlindCornerAngle(Bot.GoalEnemy);
                    if (newLean == LeanSetting.None)
                    {
                        newLean = EFTMath.RandomBool() ? LeanSetting.Left : LeanSetting.Right;
                    }
                    ChangeLeanTimer = Time.time + Random.Range(0.5f, 2f);
                    break;
            }

            if (checkLeanIntoObject(newLean))
            {
                return;
            }
            CurrentLean = newLean;
            Bot.Mover.Lean.FastLean(newLean);
        }

        private const float RAYCAST_LEAN_HITOBJECT_DIST = 0.5f;

        private bool checkLeanIntoObject(LeanSetting lean)
        {
            Vector3 headPos = Bot.Transform.EyePosition;
            Vector3 rayEnd = lean == LeanSetting.Right ? Bot.Transform.Right() : Bot.Transform.Left();
            switch (lean)
            {
                case LeanSetting.Right:
                case LeanSetting.Left:
                    return Vector.Raycast(headPos, headPos + (rayEnd * RAYCAST_LEAN_HITOBJECT_DIST), LayerMaskClass.HighPolyWithTerrainMask);

                default:
                    return false;
            }
        }

        private bool ShallHoldLean()
        {
            if (Bot.Suppression.IsSuppressed)
            {
                return false;
            }
            Enemy enemy = Bot.GoalEnemy;
            if (enemy == null || !enemy.Seen)
            {
                return false;
            }
            if (enemy.IsVisible && enemy.CanShoot)
            {
                return true;
            }
            if (enemy.TimeSinceSeen < 3f)
            {
                return true;
            }
            return false;
        }

        private LeanSetting CurrentLean;
        private float ChangeLeanTimer;

        private CoverPoint CoverInUse => Bot.Cover.CoverInUse;

        public override void Start()
        {
            Toggle(true);
            ChangeLeanTimer = Time.time + 2f;
        }

        public override void Stop()
        {
            Toggle(false);
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Hold In Cover Info");
            var cover = Bot.Cover;
            stringBuilder.AppendLabeledValue("CoverFinder State", $"{cover.CurrentCoverFinderState}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Cover Count", $"{cover.CoverPoints.Count}", Color.white, Color.yellow, true);

            stringBuilder.AppendLabeledValue("Current Cover Status", $"{CoverInUse?.StraightDistanceStatus}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Current Cover Height", $"{CoverInUse?.HardData.Height}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Current Cover Value", $"{CoverInUse?.HardData.Value}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("CoverFinder State", $"{cover.CurrentCoverFinderState}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Cover Count", $"{cover.CoverPoints.Count}", Color.white, Color.yellow, true);

            if (CoverInUse != null)
            {
                stringBuilder.AppendLine("Cover In Use");
                stringBuilder.AppendLabeledValue("Status", $"{CoverInUse.StraightDistanceStatus}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Height / Value", $"{CoverInUse.CoverHeight} {CoverInUse.HardData.Value}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Path Length", $"{CoverInUse.PathData.PathLength}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Straight Distance", $"{(CoverInUse.Position - Bot.Position).magnitude}", Color.white, Color.yellow, true);
            }
        }
    }
}