using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace SAIN.Layers.Combat.Solo
{
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