using EFT;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Helpers;
using SAIN.Models.Enums;
using SAIN.Preset.GlobalSettings;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.Debug;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.Classes.Info;
using SAIN.SAINComponent.Classes.Memory;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.Classes.Search;
using SAIN.SAINComponent.Classes.Talk;
using SAIN.SAINComponent.Classes.WeaponFunction;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent
{
    public enum EBotActiveState
    {
        Active,
        Combat,
        Sleep,
        Disposed,
    }

    public class BotComponent : BotComponentBase
    {
        public void ActivateIfBotActive(BotOwner botOwner, PersonClass Person)
        {
            if (botOwner.BotState == EBotState.Active)
            {
                Activate(botOwner);
            }
        }

        public void Activate(BotOwner botOwner)
        {
            if (_Activated)
            {
                return;
            }
            PersonClass person = botOwner.GetComponent<PlayerComponent>()?.Person;
            if (person == null)
            {
                Logger.LogError("Person Null");
                return;
            }
            if (botOwner.BotState == EBotState.Active)
            {
                if (InitializeBot(person))
                {
                    _Activated = true;
                    OnBotActivated?.Invoke(this);
                    return;
                }
                Dispose();
            }
        }

        public bool IsInCombat => _isBotInCombat;

        private bool _isBotInCombat = false;

        private bool _Activated = false;

        public event Action<BotComponent> OnBotActivated;

        public bool IsCheater { get; private set; }

        public bool BotActive => BotActivation.BotActive;
        public bool BotInStandBy => BotActivation.BotInStandBy;
        public AILimitSetting CurrentAILimit => AILimit.CurrentAILimit;

        public bool HasEnemy => EnemyController.GoalEnemy?.EnemyPerson.Active == true;
        public bool HasLastEnemy => EnemyController.LastGoalEnemy?.EnemyPerson.Active == true;
        public Enemy Enemy => HasEnemy ? EnemyController.GoalEnemy : null;
        public Enemy LastEnemy => HasLastEnemy ? EnemyController.LastGoalEnemy : null;

        public Vector3? CurrentTargetPosition => CurrentTarget.CurrentTargetPosition;
        public Vector3? CurrentTargetDirection => CurrentTarget.CurrentTargetDirection;
        public float CurrentTargetDistance => CurrentTarget.CurrentTargetDistance;

        public BotGlobalEventsClass GlobalEvents { get; private set; }
        public BotBusyHandsDetector BusyHandsDetector { get; private set; }
        public ShootDeciderClass Shoot { get; private set; }
        public BotWeightManagement WeightManagement { get; private set; }
        public SAINBotMedicalClass Medical { get; private set; }
        public SAINActivationClass BotActivation { get; private set; }
        public DoorOpener DoorOpener { get; private set; }
        public ManualShootClass ManualShoot { get; private set; }
        public CurrentTargetClass CurrentTarget { get; private set; }
        public BotBackpackDropClass BackpackDropper { get; private set; }
        public BotLightController BotLight { get; private set; }
        public SAINBotSpaceAwareness SpaceAwareness { get; private set; }
        public AimDownSightsController AimDownSightsController { get; private set; }
        public SAINAILimit AILimit { get; private set; }
        public SAINBotSuppressClass Suppression { get; private set; }
        public SAINVaultClass Vault { get; private set; }
        public SAINSearchClass Search { get; private set; }
        public SAINMemoryClass Memory { get; private set; }
        public SAINEnemyController EnemyController { get; private set; }
        public SAINNoBushESP NoBushESP { get; private set; }
        public SAINFriendlyFireClass FriendlyFire { get; private set; }
        public SAINVisionClass Vision { get; private set; }
        public SAINMoverClass Mover { get; private set; }
        public SAINBotUnstuckClass BotStuck { get; private set; }
        public SAINHearingSensorClass Hearing { get; private set; }
        public SAINBotTalkClass Talk { get; private set; }
        public SAINDecisionClass Decision { get; private set; }
        public SAINCoverClass Cover { get; private set; }
        public SAINBotInfoClass Info { get; private set; }
        public SAINSquadClass Squad { get; private set; }
        public SAINSelfActionClass SelfActions { get; private set; }
        public BotGrenadeManager Grenade { get; private set; }
        public SAINSteeringClass Steering { get; private set; }
        public AimClass Aim { get; private set; }

        public bool IsDead => !Person.ActivationClass.IsAlive;
        public bool GameEnding => BotActivation.GameEnding;
        public bool SAINLayersActive => BotActivation.SAINLayersActive;

        public float DistanceToAimTarget {
            get
            {
                if (BotOwner.AimingManager.CurrentAiming != null)
                {
                    return BotOwner.AimingManager.CurrentAiming.LastDist2Target;
                }
                return CurrentTarget.CurrentTargetDistance;
            }
        }

        public float LastCheckVisibleTime;

        public ESAINLayer ActiveLayer {
            get
            {
                return BotActivation.ActiveLayer;
            }
            set
            {
                BotActivation.SetActiveLayer(value);
            }
        }

        public void ManualUpdate()
        {
            BotOwner botOwner = BotOwner;
            if (botOwner != null)
            {
                Player player = botOwner.GetPlayer;
                if (player != null)
                {
                    DrawDebugGizmos();

                    float CurrentTime = Time.time;

                    TickClassGroup(AlwaysTickClasses, CurrentTime);

                    bool active = botOwner.BotState == EBotState.Active && player.HealthController.IsAlive;
                    if (active)
                    {
                        TickClassGroup(TickWhenActiveClasses, CurrentTime);
                    }

                    bool inStandBy = !active || BotInStandBy;
                    if (!inStandBy)
                    {
                        TickClassGroup(TickWhenNoSleepClasses, CurrentTime);
                        handleDumbShit();
                    }

                    _isBotInCombat = active && !inStandBy && SAINLayersActive && (HasEnemy || CurrentTarget.CurrentTargetEnemy != null);
                    if (_isBotInCombat)
                    {
                        TickClassGroup(TickWhenCombatClasses, CurrentTime);

                        Enemy enemy = CurrentTarget.CurrentTargetEnemy;
                        if (enemy?.EnemyPlayer?.IsYourPlayer == true)
                        {
                            EnemyPlace lastKnownPlace = enemy.KnownPlaces.LastKnownPlace;
                            if (lastKnownPlace != null)
                            {
                                foreach (var part in lastKnownPlace.BodyPartPositions.Values)
                                {
                                    DebugGizmos.Sphere(part, 0.1f, 0.02f);
                                }
                                DebugGizmos.Sphere(lastKnownPlace.EnemyHeadAtPosition(), 0.2f, 0.02f);
                            }
                        }
                    }
                    return;
                }
            }
            _isBotInCombat = false;
        }

        private void DrawDebugGizmos()
        {
            var enemy = CurrentTarget.CurrentTargetEnemy;
            //DebugGizmos.Line(Transform.WeaponRoot, Transform.WeaponRoot + PlayerComponent.TargetLookDirection.normalized * 1.5f, Color.white, 0.06f, true, 0.02f);
            DebugGizmos.Line(Transform.WeaponRoot, Transform.WeaponRoot + PlayerComponent.CurrentControlLookDirection, Color.yellow, 0.04f, true, 0.02f);
            DebugGizmos.Line(Transform.WeaponRoot, Transform.WeaponRoot + LookDirection * 0.66f, Color.green, 0.02f, true, 0.02f);
        }

        private static void TickClassGroup(List<IBotClass> List, float CurrentTime)
        {
            for (int i = 0; i < List.Count; i++)
            {
                IBotClass Class = List[i];
                if (Class != null)
                {
                    //&& Class.ShallTick(CurrentTime)
                    Class.ManualUpdate();
                }
            }
        }

        public bool InitializeBot(PersonClass person)
        {
            if (!base.Init(person))
            {
                return false;
            }
            if (!CreateClasses())
            {
                return false;
            }
            if (!addToSquad())
            {
                return false;
            }
            if (!initClasses())
            {
                return false;
            }
            if (!finishInit())
            {
                return false;
            }
            return true;
        }

        public NavMeshAgent NavMeshAgent { get; private set; }

        private bool CreateClasses()
        {
            try
            {
                // Must be first, other classes use it
                Info = new SAINBotInfoClass(this);

                NoBushESP = this.gameObject.AddComponent<SAINNoBushESP>();
                NavMeshAgent = this.gameObject.GetComponent<NavMeshAgent>();
                if (NavMeshAgent == null)
                {
                    Logger.LogWarning("agent null");
                }

                Squad = new SAINSquadClass(this);
                BusyHandsDetector = new BotBusyHandsDetector(this);
                GlobalEvents = new BotGlobalEventsClass(this);
                Shoot = new ShootDeciderClass(this);
                WeightManagement = new BotWeightManagement(this);
                Memory = new SAINMemoryClass(this);
                BotStuck = new SAINBotUnstuckClass(this);
                Hearing = new SAINHearingSensorClass(this);
                Talk = new SAINBotTalkClass(this);
                Decision = new SAINDecisionClass(this);
                Cover = new SAINCoverClass(this);
                SelfActions = new SAINSelfActionClass(this);
                Steering = new SAINSteeringClass(this);
                Grenade = new BotGrenadeManager(this);
                Mover = new SAINMoverClass(this);
                EnemyController = new SAINEnemyController(this);
                FriendlyFire = new SAINFriendlyFireClass(this);
                Vision = new SAINVisionClass(this);
                Search = new SAINSearchClass(this);
                Vault = new SAINVaultClass(this);
                Suppression = new SAINBotSuppressClass(this);
                AILimit = new SAINAILimit(this);
                AimDownSightsController = new AimDownSightsController(this);
                SpaceAwareness = new SAINBotSpaceAwareness(this);
                DoorOpener = new DoorOpener(this);
                Medical = new SAINBotMedicalClass(this);
                BotLight = new BotLightController(this);
                BackpackDropper = new BotBackpackDropClass(this);
                CurrentTarget = new CurrentTargetClass(this);
                ManualShoot = new ManualShootClass(this);
                BotActivation = new SAINActivationClass(this);
                Aim = new AimClass(this);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error When Creating Classes, Disposing... : {ex}");
                return false;
            }
            return true;
        }

        public void AddBotClass(IBotClass Class)
        {
            if (Class == null)
            {
                Logger.LogError($"Bot Class of is null, cannot add it to list!");
                return;
            }
            BotClasses.Add(Class);
        }

        public void AddBotTickClass(IBotClass Class)
        {
            if (Class.CanEverTick)
            {
                switch (Class.TickRequirement)
                {
                    case ESAINTickState.AlwaysUpdate:
                        AlwaysTickClasses.Add(Class);
                        break;

                    case ESAINTickState.OnlyBotActive:
                        TickWhenActiveClasses.Add(Class);
                        break;

                    case ESAINTickState.OnlyNoSleep:
                        TickWhenNoSleepClasses.Add(Class);
                        break;

                    case ESAINTickState.OnlyBotInCombat:
                        TickWhenCombatClasses.Add(Class);
                        break;

                    default:
                        break;
                }
            }
        }

        private bool addToSquad()
        {
            try
            {
                Squad.SquadInfo.AddMember(this);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error adding member to squad!: {ex}");
                return false;
            }
            return true;
        }

        private bool initClasses()
        {
            try
            {
                NoBushESP.Init(Person.AIInfo.BotOwner, this);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error When Initializing Components, Disposing... : {ex}");
                return false;
            }
            foreach (var botClass in BotClasses)
            {
                try
                {
                    botClass.Init();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error When Initializing Class [{botClass}], Disposing... : {ex}");
                    return false;
                }
            }
            return true;
        }

        private bool finishInit()
        {
            try
            {
                if (!verifyBrain(Person))
                {
                    Logger.LogError("Init SAIN ERROR, Disposing...");
                    return false;
                }

                try
                {
                    BotOwner.LookSensor.MaxShootDist = float.MaxValue;
                    if (BotOwner.AIData is GClass567 aiData)
                    {
                        aiData.IsNoOffsetShooting = false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error setting MaxShootDist during init, but continuing with initialization...: {ex}");
                }

                try
                {
                    var settings = GlobalSettingsClass.Instance.General.Jokes;
                    if (settings.RandomCheaters &&
                        (EFTMath.RandomBool(settings.RandomCheaterChance) || Player.Profile.Nickname.ToLower().Contains("solarint")))
                    {
                        IsCheater = true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Error when initializing dumb shit for this bot, continuing anyways since its some dumb shit. Error: {ex}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error When Finishing Bot Initialization, Disposing... : {ex}");
                return false;
            }
            return true;
        }

        private bool verifyBrain(PersonClass person)
        {
            string assignedBrainName = person?.AIInfo?.BotOwner?.Brain?.BaseBrain?.ShortName();

            if (Info.Profile.IsPMC)
            {
                IEnumerable<string> allowedBrainNames = AIBrains.GetAllowedPMCBrains().Select(brain => brain.ToString());
                return isAssignedBrainAllowed(assignedBrainName, allowedBrainNames, "PMC") ? true : false;
            }

            if (Info.Profile.IsPlayerScav)
            {
                IEnumerable<string> allowedBrainNames = AIBrains.GetAllowedPlayerScavBrains().Select(brain => brain.ToString());
                return isAssignedBrainAllowed(assignedBrainName, allowedBrainNames, "PlayerScav") ? true : false;
            }

            if (Info.Profile.IsScav)
            {
                IEnumerable<string> allowedBrainNames = AIBrains.GetAllowedScavBrains().Select(brain => brain.ToString());
                return isAssignedBrainAllowed(assignedBrainName, allowedBrainNames, "Scav") ? true : false;
            }

            return true;
        }

        private bool isAssignedBrainAllowed(string assignedBrainName, IEnumerable<string> allowedBrainNames, string botCategory)
        {
            if (allowedBrainNames.Contains(assignedBrainName))
            {
                return true;
            }

            Logger.LogAndNotifyError($"{BotOwner.name} is a ${botCategory} but does not have any of these BaseBrains: ${string.Join(", ", allowedBrainNames)}! Current Brain Assignment: [{assignedBrainName}] : SAIN Server mod is either missing or another mod is overwriting it. Destroying SAIN for this bot...");

            return false;
        }

        private void OnDisable()
        {
            BotActivation.SetActive(false);
            StopAllCoroutines();
        }

        public void LateUpdate()
        {
            //BotActivation?.LateUpdate();
            //EnemyController?.LateUpdate();
        }

        private void handleDumbShit()
        {
            if (IsCheater)
            {
                if (defaultMoveSpeed == 0)
                {
                    defaultMoveSpeed = Player.MovementContext.MaxSpeed;
                    defaultSprintSpeed = Player.MovementContext.SprintSpeed;
                }
                Player.Grounder.enabled = Enemy == null;
                if (Enemy != null)
                {
                    Player.MovementContext.SetCharacterMovementSpeed(350, true);
                    Player.MovementContext.SprintSpeed = 50f;
                    Player.ChangeSpeed(100f);
                    Player.UpdateSpeedLimit(100f, Player.ESpeedLimit.SurfaceNormal);
                    Player.MovementContext.ChangeSpeedLimit(100f, Player.ESpeedLimit.SurfaceNormal);
                    BotOwner.SetTargetMoveSpeed(100f);
                }
                else
                {
                    Player.MovementContext.SetCharacterMovementSpeed(defaultMoveSpeed, false);
                    Player.MovementContext.SprintSpeed = defaultSprintSpeed;
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            BotActivation?.SetActive(false);
            StopAllCoroutines();

            foreach (var botClass in BotClasses)
            {
                try
                {
                    botClass.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Dispose Class [{botClass}] Error: {ex}");
                }
            }

            if (NoBushESP != null)
                GameObject.Destroy(NoBushESP);
            if (BotOwner != null)
                BotOwner.OnBotStateChange -= resetBot;

            Destroy(this);
        }

        private void resetBot(EBotState state)
        {
            Decision.ResetDecisions(false);
        }

        /// <summary>
        /// All Bot Component classes.
        /// </summary>
        private readonly List<IBotClass> BotClasses = [];

        /// <summary>
        /// Bot classes that should tick no matter what.
        /// </summary>
        private readonly List<IBotClass> AlwaysTickClasses = [];

        /// <summary>
        /// Bot classes that should tick when a bot is active.
        /// </summary>
        private readonly List<IBotClass> TickWhenActiveClasses = [];

        /// <summary>
        /// Bot classes that should tick when a bot not sleeping.
        /// </summary>
        private readonly List<IBotClass> TickWhenNoSleepClasses = [];

        /// <summary>
        /// Bot classes that should tick when a bot is in combat.
        /// </summary>
        private readonly List<IBotClass> TickWhenCombatClasses = [];

        private float defaultMoveSpeed;
        private float defaultSprintSpeed;
    }
}