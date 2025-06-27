using EFT;
using SAIN.Layers.Combat.Solo.Cover;
using SAIN.Models.Enums;

namespace SAIN.Layers.Combat.Solo
{
    internal class CombatSoloLayer : SAINLayer
    {
        public static readonly string Name = BuildLayerName("Combat Layer");

        public CombatSoloLayer(BotOwner bot, int priority) : base(bot, priority, Name, ESAINLayer.Combat)
        {
        }

        public override Action GetNextAction()
        {
            _lastSelfDecision = _currentSelfDecision;
            _lastDecision = _currentDecision;

            if (_doSurgeryAction)
            {
                _doSurgeryAction = false;
                return new Action(typeof(DoSurgeryAction), $"Surgery");
            }

            switch (_lastDecision)
            {
                case ECombatDecision.MoveToEngage:
                    return new Action(typeof(MoveToEngageAction), $"{_lastDecision}");

                case ECombatDecision.TagillaMelee:
                case ECombatDecision.MeleeAttack:
                    return new Action(typeof(MeleeAttackAction), $"{_lastDecision}");

                case ECombatDecision.RushEnemy:
                    return new Action(typeof(RushEnemyAction), $"{_lastDecision}");

                case ECombatDecision.ThrowGrenade:
                    return new Action(typeof(ThrowGrenadeAction), $"{_lastDecision}");

                case ECombatDecision.ShiftCover:
                    return new Action(typeof(ShiftCoverAction), $"{_lastDecision}");

                case ECombatDecision.RunToCover:
                    return new Action(typeof(RunToCoverAction), $"{_lastDecision}");

                case ECombatDecision.Retreat:
                    return new Action(typeof(RunToCoverAction), $"{_lastDecision} + {_lastSelfDecision}");

                case ECombatDecision.MoveToCover:
                    return new Action(typeof(WalkToCoverAction), $"{_lastDecision}");

                case ECombatDecision.DogFight:
                    return new Action(typeof(DogFightAction), $"{_lastDecision}");

                case ECombatDecision.ShootDistantEnemy:
                case ECombatDecision.StandAndShoot:
                    return new Action(typeof(StandAndShootAction), $"{_lastDecision}");

                case ECombatDecision.HoldInCover:
                    string label;
                    if (_lastSelfDecision != ESelfDecision.None)
                        label = $"{_lastDecision} + {_lastSelfDecision}";
                    else
                        label = $"{_lastDecision}";
                    return new Action(typeof(HoldinCoverAction), label);

                case ECombatDecision.Search:
                    return new Action(typeof(SearchAction), $"{_lastDecision}");

                case ECombatDecision.Freeze:
                    return new Action(typeof(FreezeAction), $"{_lastDecision}");

                default:
                    return new Action(typeof(StandAndShootAction), $"DEFAULT! {_lastDecision}");
            }
        }

        public override bool IsActive()
        {
            if (Bot == null)
            {
                return false;
            }
            bool active = _currentDecision != ECombatDecision.None;
            setLayer(active);
            return active;
        }

        public override bool IsCurrentActionEnding()
        {
            // this is dumb im sorry
            if (!_doSurgeryAction
                && _currentSelfDecision == ESelfDecision.Surgery
                && Bot.Cover.BotIsAtCoverInUse())
            {
                _doSurgeryAction = true;
                return true;
            }

            if (_lastSelfDecision == ESelfDecision.Surgery &&
                _currentSelfDecision != ESelfDecision.Surgery)
            {
                return true;
            }

            return _currentDecision != _lastDecision;
        }

        private bool _doSurgeryAction;

        private ECombatDecision _lastDecision = ECombatDecision.None;
        private ESelfDecision _lastSelfDecision = ESelfDecision.None;
        public ECombatDecision _currentDecision => Bot.Decision.CurrentCombatDecision;
        public ESelfDecision _currentSelfDecision => Bot.Decision.CurrentSelfDecision;
    }
}