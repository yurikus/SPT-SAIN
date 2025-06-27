using EFT;
using SAIN.Models.Enums;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class SAINEnemyStatus : EnemyBase, IBotEnemyClass
    {
        public ETagStatus EnemyHealthStatus { get; private set; }
        public EEnemyAction VulnerableAction { get; private set; }

        public void Init()
        {
            Enemy.Events.OnEnemyKnownChanged.OnToggle += OnEnemyKnownChanged;
        }

        public void Update()
        {
            if (Enemy.EnemyKnown)
            {
                UpdateVulnerableState();
                updateHealthStatus();
            }
        }

        public void Dispose()
        {
            Enemy.Events.OnEnemyKnownChanged.OnToggle -= OnEnemyKnownChanged;
        }

        private void updateHealthStatus()
        {
            if (_nextCheckHealthTime > Time.time)
            {
                return;
            }
            _nextCheckHealthTime = Time.time + 0.5f;

            ETagStatus lastStatus = EnemyHealthStatus;
            EnemyHealthStatus = EnemyPlayer.HealthStatus;
            if (lastStatus != EnemyHealthStatus)
            {
                Enemy.Events.HealthStatusChanged(EnemyHealthStatus);
            }
        }

        public void OnEnemyKnownChanged(bool known, Enemy enemy)
        {
            if (known)
            {
                return;
            }

            EnemyHealthStatus = ETagStatus.Healthy;
            SetVulnerableAction(EEnemyAction.None);
        }

        private EEnemyAction CheckVulnerableAction()
        {
            if (EnemyUsingSurgery)
            {
                return EEnemyAction.UsingSurgery;
            }
            if (EnemyIsReloading)
            {
                return EEnemyAction.Reloading;
            }
            if (EnemyHasGrenadeOut)
            {
                return EEnemyAction.HasGrenade;
            }
            if (EnemyIsHealing)
            {
                return EEnemyAction.Healing;
            }
            if (EnemyIsLooting)
            {
                return EEnemyAction.Looting;
            }
            return EEnemyAction.None;
        }

        private void UpdateVulnerableState()
        {
            EEnemyAction lastAction = VulnerableAction;
            VulnerableAction = CheckVulnerableAction();
            if (lastAction != VulnerableAction)
            {
                Enemy.Events.EnemyVulnerableChanged(VulnerableAction);
            }
        }

        public void SetVulnerableAction(EEnemyAction action)
        {
            if (action != VulnerableAction)
            {
                VulnerableAction = action;
                switch (action)
                {
                    case EEnemyAction.None:
                        ResetActions();
                        break;

                    case EEnemyAction.Reloading:
                        EnemyIsReloading = true;
                        break;

                    case EEnemyAction.HasGrenade:
                        EnemyHasGrenadeOut = true;
                        break;

                    case EEnemyAction.Healing:
                        EnemyIsHealing = true;
                        break;

                    case EEnemyAction.Looting:
                        EnemyIsLooting = true;
                        break;

                    case EEnemyAction.UsingSurgery:
                        EnemyUsingSurgery = true;
                        break;

                    default:
                        break;
                }
                Enemy.Events.EnemyVulnerableChanged(action);
            }
        }

        private void ResetActions()
        {
            HeardRecently = false;
            _enemyLookAtMe = false;
            ShotByEnemyRecently = false;
            EnemyUsingSurgery = false;
            EnemyIsLooting = false;
            EnemyHasGrenadeOut = false;
            EnemyIsHealing = false;
            EnemyIsReloading = false;
            ShotByEnemy = false;
            TimeFirstShot = 0f;
            LastShotPosition = null;
        }

        public bool PositionalFlareEnabled =>
            Enemy.EnemyKnown &&
            Enemy.KnownPlaces.EnemyDistanceFromLastKnown < _maxDistFromPosFlareEnabled;

        public bool HeardRecently
        {
            get
            {
                return _heardRecently.Value;
            }
            set
            {
                _heardRecently.Value = value;
            }
        }

        public bool EnemyLookingAtMe
        {
            get
            {
                if (_nextCheckEnemyLookTime < Time.time)
                {
                    _nextCheckEnemyLookTime = Time.time + 0.2f;
                    Vector3 directionToBot = (Bot.Position - EnemyCurrentPosition).normalized;
                    Vector3 enemyLookDirection = EnemyPerson.Transform.LookDirection.normalized;
                    float dot = Vector3.Dot(directionToBot, enemyLookDirection);
                    _enemyLookAtMe = dot >= 0.9f;
                }
                return _enemyLookAtMe;
            }
        }

        public bool ShotByEnemyRecently
        {
            get
            {
                return _shotByEnemy.Value;
            }
            set
            {
                if (value)
                {
                    UpdateShotStatus();
                    UpdateShotPos();
                }
                _shotByEnemy.Value = value;
            }
        }

        private void UpdateShotStatus()
        {
            if (!ShotByEnemy)
            {
                ShotByEnemy = true;
                TimeFirstShot = Time.time;
            }
        }

        private void UpdateShotPos()
        {
            Vector3 random = UnityEngine.Random.onUnitSphere;
            random.y = 0f;
            random = random.normalized;
            random *= UnityEngine.Random.Range(0.5f, Enemy.RealDistance / 5);
            LastShotPosition = Enemy.EnemyPosition + random;
        }

        public bool EnemyUsingSurgery
        {
            get
            {
                return _enemySurgery.Value;
            }
            set
            {
                _enemySurgery.Value = value;
            }
        }

        public bool EnemyIsLooting
        {
            get
            {
                return _enemyLooting.Value;
            }
            set
            {
                _enemyLooting.Value = value;
            }
        }

        public bool SearchingBecauseLooting { get; set; }

        public bool EnemyIsSuppressed
        {
            get
            {
                return _enemyIsSuppressed.Value;
            }
            set
            {
                _enemyIsSuppressed.Value = value;
            }
        }

        public bool ShotAtMeRecently
        {
            get
            {
                return _enemyShotAtMe.Value;
            }
            set
            {
                _enemyShotAtMe.Value = value;
            }
        }

        public bool EnemyIsReloading
        {
            get
            {
                return _enemyIsReloading.Value;
            }
            set
            {
                _enemyIsReloading.Value = value;
            }
        }

        public bool EnemyHasGrenadeOut
        {
            get
            {
                return _enemyHasGrenade.Value;
            }
            set
            {
                _enemyHasGrenade.Value = value;
            }
        }

        public bool EnemyIsHealing
        {
            get
            {
                return _enemyIsHealing.Value;
            }
            set
            {
                _enemyIsHealing.Value = value;
            }
        }

        public int NumberOfSearchesStarted { get; set; }

        public void GetHit(DamageInfoStruct DamageInfoStruct)
        {
            IPlayer player = DamageInfoStruct.Player?.iPlayer;
            if (player != null &&
                player.ProfileId == Enemy.EnemyProfileId)
            {
                ShotByEnemyRecently = true;
                Enemy.Events.ShotByEnemy();
            }
        }

        public bool ShotByEnemy { get; private set; }
        public float TimeFirstShot { get; private set; }
        public Vector3? LastShotPosition { get; private set; }

        public SAINEnemyStatus(Enemy enemy) : base(enemy)
        {
        }

        private readonly ExpirableBool _heardRecently = new(2f, 0.85f, 1.15f);
        private readonly ExpirableBool _enemyIsReloading = new(4f, 0.75f, 1.25f);
        private readonly ExpirableBool _enemyHasGrenade = new(4f, 0.75f, 1.25f);
        private readonly ExpirableBool _enemyIsHealing = new(4f, 0.75f, 1.25f);
        private readonly ExpirableBool _enemyShotAtMe = new(30f, 0.75f, 1.25f);
        private readonly ExpirableBool _enemyIsSuppressed = new(4f, 0.85f, 1.15f);
        private readonly ExpirableBool _enemyLooting = new(30f, 0.85f, 1.15f);
        private readonly ExpirableBool _enemySurgery = new(8f, 0.85f, 1.15f);
        private readonly ExpirableBool _shotByEnemy = new(2f, 0.75f, 1.25f);
        private bool _enemyLookAtMe;
        private float _nextCheckEnemyLookTime;
        private const float _maxDistFromPosFlareEnabled = 10f;
        private float _nextCheckHealthTime;
    }
}