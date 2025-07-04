using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Text;

namespace SAIN.Layers.Combat.Solo
{
    internal class DogFightAction : CombatAction, ISAINAction
    {
        public DogFightAction(BotOwner bot) : base(bot, "Dog Fight")
        {
        }

        public override void Update(CustomLayer.ActionData data)
        {
            this.StartProfilingSample("Update");
            Enemy Enemy = Bot.Decision.DogFightDecision.DogFightTarget ?? Bot.Enemy;
            Bot.Mover.SetTargetPose(1f);
            Bot.Steering.SteerByPriority(Enemy);
            Bot.Mover.DogFight.DogFightMove(true, Enemy);
            Shoot.CheckAimAndFire(Enemy);
            this.EndProfilingSample();
        }

        public override void Start()
        {
            Toggle(true);
            Bot.Mover.Sprint(false);
            BotOwner.Mover.SprintPause(0.5f);
        }

        public override void Stop()
        {
            Toggle(false);
            Bot.Mover.DogFight.ResetDogFightStatus();
            BotOwner.MovementResume();
        }

        public void Toggle(bool value)
        {
            ToggleAction(value);
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            DebugOverlay.AddBaseInfo(Bot, BotOwner, stringBuilder);
        }
    }
}