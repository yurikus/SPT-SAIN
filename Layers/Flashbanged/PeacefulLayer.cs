using EFT;
using SAIN.Models.Enums;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.Memory;
using UnityEngine;

namespace SAIN.Layers.Peace
{
    internal class FlashBangedLayer : SAINLayer
    {
        public static readonly string Name = BuildLayerName("FlashBanged");

        public FlashBangedLayer(BotOwner bot, int priority) : base(bot, priority, Name, ESAINLayer.Peace)
        {
        }

        public override Action GetNextAction()
        {
            return new Action(typeof(ExtractAction), $"Extract : {Bot.Memory.Extract.ExtractReason}");
        }

        public override bool IsActive()
        {
            base.IsActive();
            setLayer(false);
            return false;
        }

        public override bool IsCurrentActionEnding()
        {
            return false;
        }
    }
}