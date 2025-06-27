using EFT;
using SAIN.Models.Enums;

namespace SAIN.Layers.Combat.Run
{
    internal class BotUnstuckLayer : SAINLayer
    {
        public BotUnstuckLayer(BotOwner bot, int priority) : base(bot, priority, Name, ESAINLayer.Unstuck)
        {
        }

        public static readonly string Name = BuildLayerName("Unstuck");

        public override Action GetNextAction()
        {
            return new Action(typeof(GetUnstuckAction), $"Getting Unstuck");
        }

        public override bool IsActive()
        {
            setLayer(false);
            return false;
        }

        public override bool IsCurrentActionEnding()
        {
            if (Bot == null) return true;

            return false;
        }
    }
}