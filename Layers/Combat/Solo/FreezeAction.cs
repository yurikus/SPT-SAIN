using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes.EnemyClasses;

namespace SAIN.Layers.Combat.Solo
{
    internal class FreezeAction : CombatAction
    {
        public FreezeAction(BotOwner bot) : base(bot, nameof(FreezeAction))
        {
        }

        public void Toggle(bool value)
        {
            ToggleAction(value);
        }

        public override void Update(CustomLayer.ActionData data)
        {
            this.StartProfilingSample("Update");
            Bot.Mover.SetTargetPose(0f);
            Enemy Enemy = Bot.Enemy;
            if (Enemy != null)
            {
                Shoot.CheckAimAndFire(Enemy);
                if (!Bot.Steering.SteerByPriority(Enemy, false))
                {
                    Bot.Steering.LookToLastKnownEnemyPosition(Enemy);
                }
            }
            this.EndProfilingSample();
        }

        public override void Start()
        {
            Toggle(true);
            Bot.Mover.StopMove();
        }

        public override void Stop()
        {
            Toggle(false);
        }
    }
}