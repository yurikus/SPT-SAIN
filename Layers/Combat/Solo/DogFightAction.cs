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
            Enemy Enemy = Bot.GoalEnemy;
            Bot.Mover.SetTargetPose(1f);
            Shoot.ShootAnyVisibleEnemies(Enemy);
            Bot.Steering.SteerByPriority(Enemy);
            Bot.Mover.DogFight.DogFightMove(true, Enemy);
            this.EndProfilingSample();
        }

        public override void Start()
        {
            Toggle(true);
        }

        public override void Stop()
        {
            Toggle(false);
            Bot.Mover.DogFight.ResetDogFightStatus();
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