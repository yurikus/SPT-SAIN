using EFT;
using SAIN.Components;

namespace SAIN.SAINComponent.Classes
{
    public abstract class BotMedicalBase
    {
        public BotMedicalBase(SAINBotMedicalClass medical)
        {
            Medical = medical;
        }

        protected SAINBotMedicalClass Medical { get; private set; }
        public BotComponent Bot => Medical.Bot;
        protected BotOwner BotOwner => Bot.Person.AIInfo.BotOwner;
        protected Player Player => Bot.Person.Player;
        protected IPlayer IPlayer => Bot.Person.IPlayer;
    }
}