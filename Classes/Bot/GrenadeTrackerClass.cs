using EFT;
using SAIN.Components;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static RootMotion.FinalIK.AimPoser;

namespace SAIN.SAINComponent.SubComponents
{
    public class GrenadeTrackerClass
    {
        public GrenadeTrackerClass(BotComponent bot, Grenade grenade, Vector3 dangerPoint, float reactionTime)
        {
            Bot = bot;
            ReactionTime = reactionTime;
            DangerPoint = dangerPoint;
            Grenade = grenade;
            if ((grenade.transform.position - bot.Position).magnitude < 10f)
            {
                setSpotted();
            }
        }

        public void CheckHeardGrenadeCollision(float maxRange)
        {
            if (_spotted)
            {
                return;
            }
            maxRange *= 0.75f;
            if (GrenadeDistance < maxRange)
            {
                setSpotted();
            }
        }

        private readonly BotComponent Bot;
        private BotOwner BotOwner => Bot.BotOwner;

        public float GrenadeDistance { get; private set; }

        public void Update()
        {
            if (BotOwner == null || BotOwner.IsDead || Grenade == null || _sentToBot)
            {
                return;
            }

            if (!_sentToBot && CanReact)
            {
                _sentToBot = true;
                var collisionSound = Grenade.GrenadeSettings.CollisionSound;
                bool isFrag = collisionSound == GrenadeSettings.CollisionSounds.frag;
                var trigger = isFrag ? EPhraseTrigger.OnEnemyGrenade : EPhraseTrigger.Look;
                Bot.Talk.GroupSay(trigger, ETagStatus.Combat, false, 100);

                Vector3 pos = DangerPoint;
                BotOwner.BewareGrenade.AddGrenadeDanger(pos, Grenade);
                return;
            }

            if (_spotted)
            {
                return;
            }

            GrenadeDistance = (Grenade.transform.position - BotOwner.Position).magnitude;
            if (GrenadeDistance < 3f)
            {
                setSpotted();
                return;
            }

            if (_nextCheckRaycastTime < Time.time)
            {
                _nextCheckRaycastTime = Time.time + 0.05f;
                if (checkVisibility())
                {
                    setSpotted();
                }
            }
        }

        private bool _sentToBot;

        private void setSpotted()
        {
            if (!_spotted)
            {
                _timeSpotted = Time.time;
                _spotted = true;
            }
        }

        private bool checkVisibility()
        {
            Vector3 lookPoint = Bot.Transform.WeaponRoot;
            Vector3 lookDir = Bot.LookDirection;

            Vector3 grenadePos = Grenade.transform.position + (Vector3.up * 0.05f);
            Vector3 grenadeDir = grenadePos - lookPoint;
            if (Vector3.Dot(lookDir, grenadeDir.normalized) < 0.25f)
            {
                return false; // Not looking in the right direction
            }
            return !Physics.Raycast(lookPoint, grenadeDir, 1f, LayerMaskClass.HighPolyWithTerrainMaskAI);
        }

        public void UpdateGrenadeDanger(Vector3 Danger)
        {
            DangerPoint = Danger;
            if (_sentToBot && !_updated)
            {
                _updated = true;
                BotOwner.BewareGrenade.AddGrenadeDanger(Danger, Grenade);
            }
        }

        private bool _updated;

        private float _timeSpotted { get; set; }
        public float TimeSinceSpotted => _spotted ? Time.time - _timeSpotted : 0f;
        public Grenade Grenade { get; private set; }
        public Vector3 DangerPoint { get; set; }
        private bool _spotted { get; set; }
        public bool CanReact => _spotted && TimeSinceSpotted > ReactionTime;

        private readonly float ReactionTime;
        private float _nextCheckRaycastTime;
    }
}