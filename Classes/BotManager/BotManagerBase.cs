using EFT;
using SAIN.Components;
using SAIN.Components.BotController;

namespace SAIN
{
    public abstract class BotManagerBase
    {
        public BotManagerBase(BotManagerComponent botController)
        {
            BotController = botController;
        }

        public BotManagerComponent BotController { get; private set; }
        public BotDictionary Bots => BotController?.BotSpawnController?.BotDictionary;
        public GameWorld GameWorld => BotController.GameWorld;
        public GameWorldComponent SAINGameWorld => BotController.SAINGameWorld;
    }
}