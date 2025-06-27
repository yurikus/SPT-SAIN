using SAIN.Helpers.Events;
using SAIN.Models.Enums;
using System;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyEvents : EnemyBase, IBotClass
    {
        public EnemyToggleEventTimeTracked OnEnemyLineOfSightChanged { get; }
        public EnemyToggleEventTimeTracked OnEnemyKnownChanged { get; }
        public EnemyToggleEventTimeTracked OnActiveThreatChanged { get; }
        public EnemyToggleEventTimeTracked OnVisionChange { get; }
        public EnemyToggleEventTimeTracked OnSearch { get; }
        public EnemyToggleEventTimeTracked OnEnemyCanShootChanged { get; }

        public event Action<Enemy> OnEnemyInvalid;
        public event Action<Enemy> OnEnemyLocationsSearched;
        public event Action<Enemy> OnFirstSeen;
        public event Action<Enemy> OnEnemyShot;
        public event Action<Enemy> OnBeingShotByEnemy;
        public event Action<Enemy, EnemyPlace> OnPositionUpdated;
        public event Action<Enemy, SAINSoundType, bool, EnemyPlace> OnEnemyHeard;
        public event Action<Enemy, NavMeshPathStatus> OnPathUpdated;
        public event Action<Enemy, EEnemyAction> OnVulnerableStateChanged;
        public event Action<Enemy, ETagStatus> OnHealthStatusChanged;

        public EnemyEvents(Enemy enemy) : base(enemy)
        {
            OnEnemyLineOfSightChanged = new EnemyToggleEventTimeTracked(enemy, false);
            OnEnemyKnownChanged = new EnemyToggleEventTimeTracked(enemy, false);
            OnActiveThreatChanged = new EnemyToggleEventTimeTracked(enemy, false);
            OnVisionChange = new EnemyToggleEventTimeTracked(enemy, false);
            OnSearch = new EnemyToggleEventTimeTracked(enemy, false);
            OnEnemyCanShootChanged = new EnemyToggleEventTimeTracked(enemy, false);
        }

        public void Init()
        {
            EnemyPlayer.BeingHitAction += enemyHit;
        }

        public void Update()
        {

        }

        public void Dispose()
        {
            var player = EnemyPlayer;
            if (player != null)
                player.BeingHitAction -= enemyHit;
        }

        public void EnemyLocationsSearched()
        {
            OnEnemyLocationsSearched?.Invoke(Enemy);
        }

        public void LastKnownUpdated(EnemyPlace place)
        {
            OnPositionUpdated?.Invoke(Enemy, place);
        }

        public void SetEnemyAsInvalid()
        {
            OnEnemyInvalid?.Invoke(Enemy);
        }

        public void ShotByEnemy()
        {
            OnBeingShotByEnemy?.Invoke(Enemy);
        }

        public void HealthStatusChanged(ETagStatus status)
        {
            OnHealthStatusChanged?.Invoke(Enemy, status);
        }

        public void EnemyVulnerableChanged(EEnemyAction action)
        {
            OnVulnerableStateChanged?.Invoke(Enemy, action);
        }

        public void PathUpdated(NavMeshPathStatus status)
        {
            OnPathUpdated?.Invoke(Enemy, status);
        }

        public void EnemyFirstSeen()
        {
            OnFirstSeen?.Invoke(Enemy);
        }

        public void EnemyHeard(SAINSoundType type, bool gunFire, EnemyPlace place)
        {
            OnEnemyHeard?.Invoke(Enemy, type, gunFire, place);
        }

        private void enemyHit(DamageInfoStruct damage, EBodyPart _, float _2)
        {
            var damageSource = damage.Player?.iPlayer;
            if (damageSource == null)
            {
                return;
            }
            if (damageSource.ProfileId == Bot.ProfileId)
            {
                OnEnemyShot?.Invoke(Enemy);
            }
        }

        public class EnemyToggleEvent : ToggleEventForObject<Enemy>
        {
            public EnemyToggleEvent(Enemy enemy, bool defaultValue) : base(enemy, defaultValue)
            {
            }
        }

        public class EnemyToggleEventTimeTracked : ToggleEventForObjectTimeTracked<Enemy>
        {
            public EnemyToggleEventTimeTracked(Enemy enemy, bool defaultValue) : base(enemy, defaultValue)
            {
            }
        }
    }
}