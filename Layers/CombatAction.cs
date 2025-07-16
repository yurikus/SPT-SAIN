using EFT;
using SAIN.SAINComponent.Classes;

namespace SAIN.Layers
{
    public abstract class CombatAction : BotAction
    {
        protected SAINShootData Shoot => Bot.Shoot;

        public CombatAction(BotOwner botOwner, string name) : base(botOwner, name)
        {
        }

    }
}
