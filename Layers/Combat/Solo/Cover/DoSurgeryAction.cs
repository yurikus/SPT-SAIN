using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Components;
using System.Text;
using UnityEngine;

namespace SAIN.Layers.Combat.Solo.Cover
{
    internal class DoSurgeryAction(BotOwner botOwner) : CombatAction(botOwner, "Surgery"), ISAINAction
    {
        public void Toggle(bool value)
        {
            ToggleAction(value);
        }

        public override void Update(CustomLayer.ActionData data)
        {
            this.StartProfilingSample("Update");
            checkDoSurgery();
            handleSteering();
            this.EndProfilingSample();
        }

        private void checkDoSurgery()
        {
            BotComponent bot = Bot;
            var sainSurgery = bot.Medical.Surgery;
            if (sainSurgery.AreaClearForSurgery)
            {
                bot.Mover.ActivePath?.Cancel(0.1f);
                bot.Mover.SetTargetMoveSpeed(0f);
                bot.Cover.DuckInCover(bot.GoalEnemy);
                var eftSurgery = bot.BotOwner.Medecine.SurgicalKit;
                if (_startSurgeryTime < Time.time
                    && !eftSurgery.Using
                    && eftSurgery.ShallStartUse())
                {
                    sainSurgery.SurgeryStarted = true;
                    eftSurgery.ApplyToCurrentPart(new System.Action(onSurgeryDone));
                }
                else if (_actionStartedTime + 30f < Time.time)
                {
                    bot.Player?.ActiveHealthController?.RestoreFullHealth();
                    bot.Decision.ResetDecisions(true);
                }
            }
            else
            {
                //Bot.Mover.SetTargetMoveSpeed(1);
                bot.Mover.SetTargetPose(1);

                bot.Medical.Surgery.SurgeryStarted = false;
                bot.Medical.TryCancelHeal();
                bot.Mover.DogFight.DogFightMove(false, bot.GoalEnemy);
            }
        }

        private void handleSteering()
        {
            if (!Bot.Steering.SteerByPriority(null, false) &&
                !Bot.Steering.LookToLastKnownEnemyPosition(Bot.GoalEnemy))
            {
                Bot.Steering.LookToRandomPosition();
            }
        }

        private void onSurgeryDone()
        {
            Bot.Medical.Surgery.SurgeryStarted = false;
            _actionStartedTime = Time.time;
            _startSurgeryTime = Time.time + 1f;

            if (BotOwner.Medecine.SurgicalKit.HaveWork)
            {
                if (Bot.GoalEnemy == null || Bot.GoalEnemy.TimeSinceSeen > 90f)
                {
                    Bot.Player?.ActiveHealthController?.RestoreFullHealth();
                    Bot.Decision.ResetDecisions(true);
                }
                return;
            }
            Bot.Decision.ResetDecisions(true);
        }

        public override void Start()
        {
            Toggle(true);
            Bot.Mover.PauseMovement(3f);
            _startSurgeryTime = Time.time + 1f;
            _actionStartedTime = Time.time;
        }

        private float _startSurgeryTime;
        private float _actionStartedTime;

        public override void Stop()
        {
            Toggle(false);
            Bot.Medical.Surgery.SurgeryStarted = false;
            BotOwner.MovementResume();
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine($"Health Status {Bot.Memory.Health.HealthStatus}");
            stringBuilder.AppendLine($"Surgery Started? {Bot.Medical.Surgery.SurgeryStarted}");
            stringBuilder.AppendLine($"Time Since Surgery Started {Time.time - Bot.Medical.Surgery.SurgeryStartTime}");
            stringBuilder.AppendLine($"Area Clear? {Bot.Medical.Surgery.AreaClearForSurgery}");
            stringBuilder.AppendLine($"ShallStartUse Surgery? {BotOwner.Medecine.SurgicalKit.ShallStartUse()}");
            stringBuilder.AppendLine($"IsBleeding? {BotOwner.Medecine.FirstAid.IsBleeding}");
        }
    }
}