using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Components;
using SAIN.Models.Enums;
using SAIN.SAINComponent;
using System.Text;

namespace SAIN.Layers
{
    public abstract class SAINLayer : CustomLayer
    {
        public static string BuildLayerName(string name)
        {
            return $"SAIN : {name}";
        }

        public SAINLayer(BotOwner botOwner, int priority, string layerName, ESAINLayer eSAINLayer) : base(botOwner, priority)
        {
            LayerName = layerName;
            ELayer = eSAINLayer;
        }

        private readonly string LayerName;
        private readonly ESAINLayer ELayer;

        protected void setLayer(bool active)
        {
            if (Bot == null)
            {
                return;
            }
            if (active)
            {
                Bot.ActiveLayer = ELayer;
            }
            else if (Bot != null && Bot.ActiveLayer == ELayer)
            {
                Bot.ActiveLayer = ESAINLayer.None;
            }
        }

        public override string GetName() => LayerName;

        public SAINBotController BotController => SAINBotController.Instance;

        public BotComponent Bot
        {
            get
            {
                if (_bot == null &&
                    BotController.GetSAIN(BotOwner, out var bot))
                {
                    _bot = bot;
                }
                if (_bot == null)
                {
                    _bot = BotOwner.GetComponent<BotComponent>();
                }
                return _bot;
            }
        }

        private BotComponent _bot;


        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            if (Bot != null)
            {
                DebugOverlay.AddBaseInfo(Bot, BotOwner, stringBuilder);
            }
        }
    }
}