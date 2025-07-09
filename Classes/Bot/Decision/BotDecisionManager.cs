using EFT;
using SAIN.Classes.Coverfinder;
using SAIN.Components;
using SAIN.Helpers.Events;
using SAIN.Models.Enums;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class BotDecisionManager : BotSubClass<SAINDecisionClass>, IBotClass
    {
        private const float DECISION_FREQUENCY = 1f / 30;
        private const float DECISION_FREQUENCY_PEACE = 1f / 10;

        public event Action<ECombatDecision, ESquadDecision, ESelfDecision, BotComponent> OnDecisionMade;

        public ToggleEvent HasDecisionToggle { get; } = new ToggleEvent();

        public ECombatDecision CurrentCombatDecision { get; private set; }
        public ECombatDecision PreviousCombatDecision { get; private set; }
        public ESquadDecision CurrentSquadDecision { get; private set; }
        public ESquadDecision PreviousSquadDecision { get; private set; }
        public ESelfDecision CurrentSelfDecision { get; private set; }
        public ESelfDecision PreviousSelfDecision { get; private set; }

        public bool HasDecision => HasDecisionToggle.Value;
        public float ChangeDecisionTime { get; private set; }
        public float TimeSinceChangeDecision => Time.time - ChangeDecisionTime;

        public BotDecisionManager(SAINDecisionClass decisionClass) : base(decisionClass)
        {
        }

        public override void Init()
        {
            Bot.BotActivation.BotActiveToggle.OnToggle += resetDecisions;
            base.Init();
        }

        public override void ManualUpdate()
        {
            updateDecision();
        }

        public override void Dispose()
        {
            Bot.BotActivation.BotActiveToggle.OnToggle -= resetDecisions;
            base.Dispose();
        }

        private void updateDecision()
        {
            if (_nextGetDecisionTime < Time.time)
            {
                getDecision();
                float delay = HasDecision ? DECISION_FREQUENCY : DECISION_FREQUENCY_PEACE;
                _nextGetDecisionTime = Time.time + delay;
            }
        }

        private bool shallTagillaHammerAttack(Enemy enemy)
        {
            if (enemy == null)
            {
                return false;
            }
            bool alreadyAttacking = CurrentCombatDecision == ECombatDecision.MeleeAttack;
            ETagStatus status = Bot.Memory.Health.HealthStatus;

            if (!alreadyAttacking)
            {
                if (CurrentSelfDecision != ESelfDecision.None)
                    return false;
                if (status != ETagStatus.Healthy && status != ETagStatus.Injured)
                    return false;
                if (enemy.Path.PathToEnemyStatus != UnityEngine.AI.NavMeshPathStatus.PathComplete)
                    return false;
                if (enemy.RealDistance < 30 && enemy.Path.PathLength < 20 && enemy.Status.VulnerableAction != EEnemyAction.None)
                {
                    enemy.BotOwner.WeaponManager.Melee.ShallEndRun = false;
                    return true;
                }
                return false;
            }
            if (enemy.BotOwner.WeaponManager.Melee.ShallEndRun)
            {
                return false;
            }
            if (status != ETagStatus.Dying && enemy.RealDistance < 40 && enemy.Path.PathLength < 35)
            {
                return true;
            }
            return false;
        }

        private void getDecision()
        {
            Enemy enemy = Bot.Enemy;
            EnemyList knownEnemies = Bot.EnemyController.EnemyLists.KnownEnemies;
            BaseClass.EnemyDecisions.DebugShallSearch = null;
            if (Bot.Info.Profile.WildSpawnType == WildSpawnType.bossTagilla)
            {
                if (shallTagillaHammerAttack(enemy))
                {
                    SetDecisions(ECombatDecision.MeleeAttack, ESquadDecision.None, ESelfDecision.None);
                    return;
                }
                if (BotOwner.WeaponManager.IsMelee) BotOwner.WeaponManager.Selector.ChangeToMain();
            }

            if (enemy != null && enemy.IsZombie)
            {
                bool hasShooterContact = false;
                foreach (var knownEnemy in knownEnemies)
                    if (knownEnemy?.IsZombie != true)
                        hasShooterContact = true;
                if (!hasShooterContact)
                {
                    BaseClass.SelfActionDecisions.GetDecision(out ESelfDecision zombieDecision, enemy);
                    BaseClass.SquadDecisions.GetDecision(out ESquadDecision zombieSqdDecision, enemy);
                    SetDecisions(ECombatDecision.FightZombies, zombieSqdDecision, zombieDecision);
                    return;
                }
            }

            if (BaseClass.DogFightDecision.ShallDogFight(knownEnemies))
            {
                SetDecisions(ECombatDecision.DogFight, ESquadDecision.None, ESelfDecision.None);
                return;
            }
            if (BotOwner.WeaponManager.IsMelee)
            {
                SetDecisions(ECombatDecision.MeleeAttack, ESquadDecision.None, ESelfDecision.None);
                return;
            }
            if (BaseClass.SelfActionDecisions.GetDecision(out ESelfDecision selfDecision, enemy))
            {
                var selfCombat = Bot.Cover.InCover ? ECombatDecision.HoldInCover : ECombatDecision.Retreat;
                SetDecisions(selfCombat, ESquadDecision.None, selfDecision);
                return;
            }
            if (CheckContinueRetreat())
            {
                SetDecisions(ECombatDecision.Retreat, ESquadDecision.None, ESelfDecision.None);
                return;
            }
            if (BaseClass.SquadDecisions.GetDecision(out ESquadDecision squadDecision, enemy))
            {
                SetDecisions(ECombatDecision.None, squadDecision, ESelfDecision.None);
                return;
            }
            if (BaseClass.EnemyDecisions.GetDecision(out ECombatDecision combatDecision, enemy, knownEnemies))
            {
                SetDecisions(combatDecision, ESquadDecision.None, ESelfDecision.None);
                return;
            }
            SetDecisions(ECombatDecision.None, ESquadDecision.None, ESelfDecision.None);
        }

        private void SetDecisions(ECombatDecision solo, ESquadDecision squad, ESelfDecision self)
        {
            if (SAINPlugin.DebugMode)
            {
                if (SAINPlugin.ForceSoloDecision != ECombatDecision.None)
                {
                    solo = SAINPlugin.ForceSoloDecision;
                }
                if (SAINPlugin.ForceSquadDecision != ESquadDecision.None)
                {
                    squad = SAINPlugin.ForceSquadDecision;
                }
                if (SAINPlugin.ForceSelfDecision != ESelfDecision.None)
                {
                    self = SAINPlugin.ForceSelfDecision;
                }
            }

            if (checkForNewDecision(solo, squad, self))
            {
                bool hasDecision =
                    solo != ECombatDecision.None ||
                    self != ESelfDecision.None ||
                    squad != ESquadDecision.None;

                HasDecisionToggle.CheckToggle(hasDecision);

                // this is dumb sorry
                Bot.ManualShoot.Reset();
                Bot.Suppression.ResetSuppressing();

                ChangeDecisionTime = Time.time;
                OnDecisionMade?.Invoke(solo, squad, self, Bot);
            }
        }

        private bool checkForNewDecision(ECombatDecision newSoloDecision, ESquadDecision newSquadDecision, ESelfDecision newSelfDecision)
        {
            bool newDecision = false;

            if (newSoloDecision != CurrentCombatDecision)
            {
                PreviousCombatDecision = CurrentCombatDecision;
                CurrentCombatDecision = newSoloDecision;
                newDecision = true;
            }

            if (newSquadDecision != CurrentSquadDecision)
            {
                PreviousSquadDecision = CurrentSquadDecision;
                CurrentSquadDecision = newSquadDecision;
                newDecision = true;
            }

            if (newSelfDecision != CurrentSelfDecision)
            {
                PreviousSelfDecision = CurrentSelfDecision;
                CurrentSelfDecision = newSelfDecision;
                newDecision = true;
            }

            return newDecision;
        }

        public void ResetDecisions(bool active)
        {
            bool hasDecision = HasDecision;
            resetDecisions(false);
            if (active && hasDecision)
            {
                BotOwner.CalcGoal();
            }
        }

        private void resetDecisions(bool value)
        {
            if (!value)
            {
                SetDecisions(ECombatDecision.None, ESquadDecision.None, ESelfDecision.None);
            }
        }

        private bool CheckContinueRetreat()
        {
            bool runningToCover = CurrentCombatDecision == ECombatDecision.Retreat || CurrentCombatDecision == ECombatDecision.RunToCover;
            if (!runningToCover)
            {
                return false;
            }
            if (!Bot.Mover.PathFollower.Running)
            {
                return false;
            }
            if (Bot.Cover.InCover)
            {
                return false;
            }

            float timeChangeDec = Bot.Decision.TimeSinceChangeDecision;
            if (timeChangeDec < 0.5f)
            {
                return true;
            }

            if (timeChangeDec > 3 &&
                !Bot.BotStuck.BotHasChangedPosition)
            {
                return false;
            }

            CoverPoint coverInUse = Bot.Cover.CoverInUse;
            if (coverInUse == null)
            {
                return false;
            }

            switch (coverInUse.PathDistanceStatus)
            {
                case CoverStatus.InCover:
                    return false;

                case CoverStatus.CloseToCover:
                    return true;

                default:
                    return !coverInUse.CoverData.IsBad;
            }
        }

        private float _nextGetDecisionTime;
    }
}