
namespace SAIN.SAINComponent.Classes.Decision
{
    public class SAINDecisionClass : BotBase, IBotClass
    {
        public bool HasDecision => DecisionManager.HasDecision;

        public ECombatDecision CurrentCombatDecision => DecisionManager.CurrentCombatDecision;
        public ECombatDecision PreviousCombatDecision => DecisionManager.PreviousCombatDecision;

        public ESquadDecision CurrentSquadDecision => DecisionManager.CurrentSquadDecision;
        public ESquadDecision PreviousSquadDecision => DecisionManager.PreviousSquadDecision;

        public ESelfDecision CurrentSelfDecision => DecisionManager.CurrentSelfDecision;
        public ESelfDecision PreviousSelfDecision => DecisionManager.PreviousSelfDecision;

        public float ChangeDecisionTime => DecisionManager.ChangeDecisionTime;
        public float TimeSinceChangeDecision => DecisionManager.TimeSinceChangeDecision;

        public bool RunningToCover
        {
            get
            {
                switch (CurrentCombatDecision)
                {
                    case ECombatDecision.Retreat:
                    case ECombatDecision.RunAway:
                    case ECombatDecision.RunToCover:
                        return true;

                    default:
                        return false;
                }
            }
        }

        public bool IsSearching =>
            CurrentCombatDecision == ECombatDecision.Search ||
            CurrentSquadDecision == ESquadDecision.Search ||
            CurrentSquadDecision == ESquadDecision.GroupSearch;

        public BotDecisionManager DecisionManager { get; }
        public DogFightDecisionClass DogFightDecision { get; }
        public SelfActionDecisionClass SelfActionDecisions { get; }
        public EnemyDecisionClass EnemyDecisions { get; }
        public SquadDecisionClass SquadDecisions { get; }

        public SAINDecisionClass(BotComponent bot) : base(bot)
        {
            DecisionManager = new BotDecisionManager(this);
            SelfActionDecisions = new SelfActionDecisionClass(bot);
            EnemyDecisions = new EnemyDecisionClass(bot);
            SquadDecisions = new SquadDecisionClass(bot);
            DogFightDecision = new DogFightDecisionClass(bot);
        }

        public void Init()
        {
            DecisionManager.Init();
            SelfActionDecisions.Init();
            EnemyDecisions.Init();
            SquadDecisions.Init();
            DogFightDecision.Init();
        }

        public void Update()
        {
            DecisionManager.Update();
            SelfActionDecisions.Update();
            EnemyDecisions.Update();
            SquadDecisions.Update();
            DogFightDecision.Update();
        }

        public void Dispose()
        {
            DecisionManager.Dispose();
            SelfActionDecisions.Dispose();
            EnemyDecisions.Dispose();
            SquadDecisions.Dispose();
            DogFightDecision?.Dispose();
        }

        public void ResetDecisions(bool active) => DecisionManager.ResetDecisions(active);
    }
}