using EFT;
using HarmonyLib;
using SAIN.Components;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.SubComponents;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.WeaponFunction
{
    public class GrenadeVelocityTracker : MonoBehaviour
    {
        public Vector3 Velocity { get; private set; }
        public float VelocityMagnitude { get; private set; }

        private const float GRENADE_UPDATE_FREQUENCY = 0.5f;

        public void Awake()
        {
            _grenade = this.GetComponent<Grenade>();
            _grenade.DestroyEvent += GrenadeDestroyed;
            _rigidBody = (Rigidbody)_rigidBodyField.GetValue(_grenade);
        }

        public void Update()
        {
            if (_grenade == null) return;
            if (_rigidBody == null)
            {
                GrenadeDestroyed(_grenade);
                return;
            }
            if (_nextUpdateTime < Time.time)
            {
                _nextUpdateTime = Time.time + GRENADE_UPDATE_FREQUENCY;
                Velocity = _rigidBody.velocity;
                VelocityMagnitude = Velocity.magnitude;
                //Logger.LogInfo($"Grenade {_grenade.Id} Velocity [{Velocity}] Magnitude: [{VelocityMagnitude}]");
            }
        }

        private Rigidbody _rigidBody;
        private Grenade _grenade;

        static GrenadeVelocityTracker()
        {
            _rigidBodyField = AccessTools.Field(typeof(Throwable), "Rigidbody");
        }

        private void GrenadeDestroyed(Throwable grenade)
        {
            if (grenade != null)
            {
                grenade.DestroyEvent -= GrenadeDestroyed;
            }
            Destroy(this);
        }

        private static FieldInfo _rigidBodyField;
        private float _nextUpdateTime;
    }

    public class GrenadeReactionClass : BotSubClass<BotGrenadeManager>, IBotClass
    {
        public GrenadeTracker DangerGrenade { get; private set; }
        public Vector3? GrenadeDangerPoint => DangerGrenade?.DangerPoint;
        public Dictionary<int, GrenadeTracker> EnemyGrenadesList { get; private set; } = new Dictionary<int, GrenadeTracker>();
        public Dictionary<int, Grenade> FriendlyGrenadesList { get; private set; } = new Dictionary<int, Grenade>();

        public GrenadeReactionClass(BotGrenadeManager ThrowWeapItemClass) : base(ThrowWeapItemClass)
        {
        }

        public void Init()
        {
            SAINBotController.Instance.GrenadeController.OnGrenadeCollision += GrenadeCollision;
            SAINBotController.Instance.GrenadeController.OnGrenadeThrown += EnemyGrenadeThrown;
        }

        public void Update()
        {
            foreach (var tracker in EnemyGrenadesList.Values)
            {
                tracker?.Update();
            }
            foreach (var grenade in FriendlyGrenadesList.Values)
            {
            }
        }

        public void Dispose()
        {
            SAINBotController.Instance.GrenadeController.OnGrenadeCollision -= GrenadeCollision;
            SAINBotController.Instance.GrenadeController.OnGrenadeThrown -= EnemyGrenadeThrown;

            foreach (var tracker in EnemyGrenadesList.Values)
            {
                if (tracker?.Grenade != null)
                {
                    tracker.Grenade.DestroyEvent -= RemoveGrenade;
                }
            }
            foreach (var grenade in FriendlyGrenadesList.Values)
            {
                if (grenade != null)
                {
                    grenade.DestroyEvent -= RemoveGrenade;
                }
            }
            EnemyGrenadesList.Clear();
            FriendlyGrenadesList.Clear();
        }

        public void EnemyGrenadeThrown(Grenade grenade, Vector3 dangerPoint, string profileId)
        {
            if (Bot == null || profileId == Bot.ProfileId || !Bot.BotActive)
            {
                return;
            }
            Enemy enemy = Bot.EnemyController.GetEnemy(profileId, false);
            if (enemy != null &&
                enemy.RealDistance <= MAX_ENEMY_GRENADE_DIST_TOCARE)
            {
                float reactionTime = GetReactionTime(Bot.Info.Profile.DifficultyModifier);
                EnemyGrenadesList.Add(grenade.Id, new GrenadeTracker(Bot, grenade, dangerPoint, reactionTime));
                grenade.DestroyEvent += RemoveGrenade;
                return;
            }
            //if (enemy == null && (dangerPoint - Bot.Position).sqrMagnitude <= MAX_FRIENDLY_GRENADE_DIST_TOCARE_SQR) {
            //    FriendlyGrenadesList.Add(grenade.Id, grenade);
            //}
        }

        private const float MAX_ENEMY_GRENADE_DIST_TOCARE = 100;
        private const float MAX_FRIENDLY_GRENADE_DIST_TOCARE = 60f;
        private const float MAX_FRIENDLY_GRENADE_DIST_TOCARE_SQR = MAX_FRIENDLY_GRENADE_DIST_TOCARE * MAX_FRIENDLY_GRENADE_DIST_TOCARE;
        private const float FRIENDLY_GRENADE_DIST_TORUN = 10f;
        private const float FRIENDLY_GRENADE_DIST_TORUN_SQR = FRIENDLY_GRENADE_DIST_TORUN * FRIENDLY_GRENADE_DIST_TORUN;

        private void GrenadeCollision(Grenade grenade, float maxRange)
        {
            if (Bot == null || grenade.ProfileId == Bot.ProfileId)
            {
                return;
            }

            foreach (var tracker in EnemyGrenadesList.Values)
            {
                if (tracker.Grenade == grenade)
                {
                    tracker.CheckHeardGrenadeCollision(maxRange);
                }
            }
        }

        private void RemoveGrenade(Throwable grenade)
        {
            if (grenade != null)
            {
                grenade.DestroyEvent -= RemoveGrenade;
                EnemyGrenadesList.Remove(grenade.Id);
                FriendlyGrenadesList.Remove(grenade.Id);
            }
        }

        private static float GetReactionTime(float diffMod)
        {
            float reactionTime = 0.25f;
            reactionTime /= diffMod;
            reactionTime *= Random.Range(0.75f, 1.25f);

            float min = 0.1f;
            float max = 0.5f;

            return Mathf.Clamp(reactionTime, min, max);
        }
    }
}