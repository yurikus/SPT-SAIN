using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes.EnemyClasses;

namespace SAIN.Layers.Combat.Solo
{
    internal class FightZombiesAction : CombatAction, ISAINAction
    {
        public FightZombiesAction(BotOwner bot) : base(bot, "Fight Zombies")
        {
        }

        public override void Update(CustomLayer.ActionData data)
        {
            Enemy priorityEnemy = Bot.CurrentTarget.CurrentTargetEnemy;
            Enemy shotEnemy = Shoot.GetEnemyToShoot(priorityEnemy);
            if (shotEnemy != null)
            {
                if (shotEnemy.RealDistance < 10f)
                {
                    Bot.Mover.DogFight.BackUpFromEnemy(shotEnemy);
                    Bot.Mover.SetTargetMoveSpeed(1f);
                    Bot.Mover.SetTargetPose(1f);
                }
                else if (shotEnemy.RealDistance > 20f)
                {
                    Bot.Mover.Stop();
                }
                return;
            }
            Bot.Mover.DogFight.DogFightMove(true, priorityEnemy);
            Bot.Steering.SteerByPriority(priorityEnemy, true, false);
        }

        public override void Start()
        {
            Toggle(true);
        }

        public override void Stop()
        {
            Toggle(false);
        }

        public void Toggle(bool value)
        {
            ToggleAction(value);
        }
    }
    internal class MeleeAttackAction : CombatAction, ISAINAction
    {
        public MeleeAttackAction(BotOwner bot) : base(bot, "Melee Attack")
        {
        }

        public override void Update(CustomLayer.ActionData data)
        {
            this.StartProfilingSample("Update");
            BotOwner.WeaponManager.Melee.RunToEnemyUpdate();
            this.EndProfilingSample();
        }

        public override void Start()
        {
            Toggle(true);
        }

        public override void Stop()
        {
            Toggle(false);
        }

        public void Toggle(bool value)
        {
            ToggleAction(value);
        }
    }
}