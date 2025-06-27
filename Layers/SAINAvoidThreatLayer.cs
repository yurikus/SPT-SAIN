using EFT;
using SAIN.Layers.Combat.Solo;
using SAIN.Layers.Combat.Solo.Cover;
using SAIN.Models.Enums;

namespace SAIN.Layers
{
    internal class SAINAvoidThreatLayer : SAINLayer
    {
        public SAINAvoidThreatLayer(BotOwner bot, int priority) : base(bot, priority, Name, ESAINLayer.AvoidThreat)
        {
        }

        public static readonly string Name = BuildLayerName("Avoid Threat");

        public override Action GetNextAction()
        {
            _lastActionDecision = CurrentDecision;
            switch (_lastActionDecision)
            {
                case ECombatDecision.DogFight:
                    if (Bot.Decision.DogFightDecision.DogFightTarget != null)
                    {
                        return new Action(typeof(DogFightAction), $"Dog Fight - Enemy Close!");
                    }
                    else if (Bot.Cover.CoverInUse?.Spotted == true)
                    {
                        return new Action(typeof(DogFightAction), $"Dog Fight - My Cover is Spotted!");
                    }
                    else if (Bot.Cover.SpottedInCover)
                    {
                        return new Action(typeof(DogFightAction), $"Dog Fight - Shot while in cover!");
                    }
                    else
                    {
                        return new Action(typeof(DogFightAction), $"Dog Fight - No Reason");
                    }

                case ECombatDecision.AvoidGrenade:
                    return new Action(typeof(RunToCoverAction), $"Avoid Grenade");

                default:
                    return new Action(typeof(DogFightAction), $"NO DECISION - ERROR IN LOGIC");
            }
        }

        public override bool IsActive()
        {
            bool active =
                Bot?.BotActive == true &&
                (CurrentDecision == ECombatDecision.DogFight ||
                CurrentDecision == ECombatDecision.AvoidGrenade);

            setLayer(active);
            return active;
        }

        public override bool IsCurrentActionEnding()
        {
            return Bot?.BotActive == true && _lastActionDecision != CurrentDecision;
        }

        private ECombatDecision _lastActionDecision;
        public ECombatDecision CurrentDecision => Bot.Decision.CurrentCombatDecision;
    }
}