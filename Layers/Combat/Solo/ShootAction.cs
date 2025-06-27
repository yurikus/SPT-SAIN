using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace SAIN.Layers.Combat.Solo
{
    internal class ShootAction : CombatAction, ISAINAction
    {
        public ShootAction(BotOwner bot) : base(bot, nameof(ShootAction))
        {
        }

        public void Toggle(bool value)
        {
            ToggleAction(value);
        }

        public override void Start()
        {
            Toggle(true);
        }

        public override void Stop()
        {
            Toggle(false);
        }

        public override void Update(CustomLayer.ActionData data)
        {
            this.StartProfilingSample("Update");
            Bot.Steering.SteerByPriority();
            Shoot.CheckAimAndFire();
            this.EndProfilingSample();
        }
    }
}