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
            tryGetBot(botOwner);
        }

        /// <summary>
        /// Contains a check to get SAIN Bot Component, its annoying but there isn't really a better way to do this with big brain. always returns false.
        /// </summary>
        /// <returns>false</returns>
        public override bool IsActive()
        {
            if (!foundBot)
            {
                tryGetBot(BotOwner);
            }
            return false;
        }

        private void tryGetBot(BotOwner botOwner)
        {
            if (!foundBot)
            {
                if (_bot == null)
                {
                    _bot = botOwner.GetComponent<BotComponent>();
                }
                if (_bot != null)
                {
                    foundBot = true;
                    if (_bot.Decision == null || _bot.Decision.DecisionManager == null)
                    {
                        _bot.OnBotActivated += OnBotActivated;
                    }
                    else
                    {
                        OnBotActivated(_bot);
                    }
                }
            }
        }

        protected virtual void OnBotActivated(BotComponent bot)
        {
            bot.Decision.DecisionManager.OnDecisionMade += BotDecisionMade;
        }

        private bool foundBot = false;

        protected void BotDecisionMade(ECombatDecision combatDecision, ESquadDecision squadDecision, ESelfDecision selfDecision, BotComponent bot)
        {
            if (Bot.ActiveLayer == ELayer)
            {
                ResetAction = true;
            }
        }

        protected bool ResetAction = false;

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

        public static SAINBotController BotController => SAINBotController.Instance;

        public BotComponent Bot => _bot;

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