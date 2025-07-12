using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System.Text;
using UnityEngine;

namespace SAIN.Layers.Combat.Solo.Cover
{
    internal class WalkToCoverAction : CombatAction, ISAINAction
    {
        public WalkToCoverAction(BotOwner bot) : base(bot, nameof(WalkToCoverAction))
        {
        }

        public void Toggle(bool value)
        {
            ToggleAction(value);
        }

        public override void Update(CustomLayer.ActionData data)
        {
            Bot.Mover.SetTargetMoveSpeed(1f);
            Bot.Mover.SetTargetPose(1f);
            Enemy enemy = Bot.GoalEnemy;
            if (!Shoot.ShootAnyVisibleEnemies(enemy) &&
                !Bot.Suppression.TrySuppressAnyEnemy(enemy, Bot.EnemyController.EnemyLists.KnownEnemies) &&
                !Bot.Steering.SteerByPriority(enemy, false))
            {
                Bot.Steering.LookToLastKnownEnemyPosition(enemy);
            }
                //bool shallCrawl =
                //    Bot.Decision.CurrentSelfDecision != ESelfDecision.None &&
                //    Bot.Player.MovementContext.CanProne &&
                //    (_wasCrawling || (coverPoint.StraightDistanceStatus == CoverStatus.FarFromCover && Bot.Mover.Prone.ShallProneHide(enemy)));
                //
        }

        private bool checkMoveToCover(CoverPoint coverPoint, Enemy enemy)
        {
            if (coverPoint != null &&
                !coverPoint.Spotted &&
                !coverPoint.CoverData.IsBad)
            {
                //bool shallCrawl =
                //    Bot.Decision.CurrentSelfDecision != ESelfDecision.None &&
                //    Bot.Player.MovementContext.CanProne &&
                //    (_wasCrawling || (coverPoint.StraightDistanceStatus == CoverStatus.FarFromCover && Bot.Mover.Prone.ShallProneHide(enemy)));
                //
            }
            return false;
        }

        public override void Start()
        {
            Toggle(true);
        }

        public override void Stop()
        {
            Toggle(false);
            Bot.Mover.DogFight.ResetDogFightStatus();
            Bot.Suppression.ResetSuppressing();
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Walk To Cover Info");
            var cover = Bot.Cover;
            stringBuilder.AppendLabeledValue("CoverFinder State", $"{cover.CurrentCoverFinderState}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Cover Seeking State", $"{cover.CoverSeekingState}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Cover Count", $"{cover.CoverPoints.Count}", Color.white, Color.yellow, true);
            DebugOverlay.AddMoveData(Bot, stringBuilder);
            if (Bot.Cover.CoverPoint_MovingTo != null)
            {
                stringBuilder.AppendLine("Cover Destination");
                stringBuilder.AppendLabeledValue("Status", $"{Bot.Cover.CoverPoint_MovingTo.StraightDistanceStatus}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Height / Value", $"{Bot.Cover.CoverPoint_MovingTo.CoverHeight} {Bot.Cover.CoverPoint_MovingTo.HardData.Value}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Path Length", $"{Bot.Cover.CoverPoint_MovingTo.PathData.PathLength}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Straight Distance", $"{(Bot.Cover.CoverPoint_MovingTo.Position - Bot.Position).magnitude}", Color.white, Color.yellow, true);
            }
        }
    }
}