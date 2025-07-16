using EFT;
using SAIN.Components;
using SAIN.Layers.Combat.Solo;
using SAIN.Models.Enums;
using SAIN.SAINComponent.Classes.Decision;

namespace SAIN.Layers.Combat.Squad
{
    internal class CombatSquadLayer(BotOwner botOwner, int priority) : SAINLayer(botOwner, priority, Name, ESAINLayer.Squad)
    {
        public static readonly string Name = BuildLayerName("Squad Layer");

        public override Action GetNextAction()
        {
            LastActionDecision = Bot.Decision.CurrentSquadDecision;
            switch (LastActionDecision)
            {
                case ESquadDecision.Regroup:
                    return new Action(typeof(RegroupAction), $"{LastActionDecision}");

                case ESquadDecision.Suppress:
                    return new Action(typeof(SuppressAction), $"{LastActionDecision}");

                case ESquadDecision.Search:
                    return new Action(typeof(SearchAction), $"{LastActionDecision}");

                case ESquadDecision.GroupSearch:
                    if (Bot.Squad.IAmLeader)
                    {
                        return new Action(typeof(SearchAction), $"{LastActionDecision} : Lead Search Party");
                    }
                    return new Action(typeof(FollowSearchParty), $"{LastActionDecision} : Follow Squad Leader");

                case ESquadDecision.Help:
                    return new Action(typeof(SearchAction), $"{LastActionDecision}");

                case ESquadDecision.PushSuppressedEnemy:
                    return new Action(typeof(RushEnemyAction), $"{LastActionDecision}");

                default:
                    return new Action(typeof(RegroupAction), $"DEFAULT!");
            }
        }

        public override bool IsActive()
        {
            base.IsActive();
            BotComponent bot = Bot;
            if (bot != null && bot.BotActive)
            {
                SAINDecisionClass decisions = bot.Decision;
                if (decisions.CurrentSelfDecision == ESelfDecision.None &&
                    decisions.CurrentCombatDecision != ECombatDecision.DogFight &&
                    decisions.CurrentSquadDecision != ESquadDecision.None)
                {
                    setLayer(true);
                    return true;
                }
            }
            setLayer(false);
            return false;
        }

        public override bool IsCurrentActionEnding()
        {
            if (ResetAction)
            {
                ResetAction = false;
                return true;
            }
            BotComponent bot = Bot;
            if (bot != null && bot.BotActive && bot.Decision.CurrentSquadDecision != LastActionDecision)
            {
                return true;
            }
            return false;
        }

        private ESquadDecision LastActionDecision = ESquadDecision.None;
    }
}