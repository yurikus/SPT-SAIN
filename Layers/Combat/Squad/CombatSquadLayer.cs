using EFT;
using SAIN.Layers.Combat.Solo;
using SAIN.Models.Enums;

namespace SAIN.Layers.Combat.Squad
{
    internal class CombatSquadLayer : SAINLayer
    {
        public static readonly string Name = BuildLayerName("Squad Layer");

        public CombatSquadLayer(BotOwner bot, int priority) : base(bot, priority, Name, ESAINLayer.Squad)
        {
        }

        public override Action GetNextAction()
        {
            var Decision = SquadDecision;
            LastActionDecision = Decision;
            switch (Decision)
            {
                case ESquadDecision.Regroup:
                    return new Action(typeof(RegroupAction), $"{Decision}");

                case ESquadDecision.Suppress:
                    return new Action(typeof(SuppressAction), $"{Decision}");

                case ESquadDecision.Search:
                    return new Action(typeof(SearchAction), $"{Decision}");

                case ESquadDecision.GroupSearch:
                    if (Bot.Squad.IAmLeader)
                    {
                        return new Action(typeof(SearchAction), $"{Decision} : Lead Search Party");
                    }
                    return new Action(typeof(FollowSearchParty), $"{Decision} : Follow Squad Leader");

                case ESquadDecision.Help:
                    return new Action(typeof(SearchAction), $"{Decision}");

                case ESquadDecision.PushSuppressedEnemy:
                    return new Action(typeof(RushEnemyAction), $"{Decision}");

                default:
                    return new Action(typeof(RegroupAction), $"DEFAULT!");
            }
        }

        public override bool IsActive()
        {
            bool active =
                Bot?.BotActive == true &&
                SquadDecision != ESquadDecision.None &&
                Bot.Decision.CurrentSelfDecision == ESelfDecision.None;

            if (active && Bot.Cover.CoverInUse != null)
            {
                Bot.Cover.CoverInUse = null;
            }

            setLayer(active);

            return active;
        }

        public override bool IsCurrentActionEnding()
        {
            return Bot?.BotActive == true &&
                SquadDecision != LastActionDecision;
        }

        private ESquadDecision LastActionDecision = ESquadDecision.None;
        public ESquadDecision SquadDecision => Bot.Decision.CurrentSquadDecision;
    }
}