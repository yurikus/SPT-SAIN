using EFT;
using SAIN.Components;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINFriendlyFireClass : BotBase, IBotClass
    {
        public bool ClearShot => FriendlyFireStatus != FriendlyFireStatus.FriendlyBlock;
        public FriendlyFireStatus FriendlyFireStatus { get; private set; }

        public SAINFriendlyFireClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
        }

        public void Update()
        {
            if (FriendlyFireStatus == FriendlyFireStatus.FriendlyBlock)
            {
                StopShooting();
            }
        }

        public void Dispose()
        {
        }

        public bool CheckFriendlyFire(Vector3? target = null)
        {
            FriendlyFireStatus = CheckFriendlyFireStatus(target);
            return FriendlyFireStatus != FriendlyFireStatus.FriendlyBlock;
        }

        private FriendlyFireStatus CheckFriendlyFireStatus(Vector3? target = null)
        {
            var members = Bot.Squad?.Members;
            if (members == null || members.Count <= 1)
            {
                return FriendlyFireStatus.None;
            }

            if (target != null)
            {
                return CheckFriendlyFire(target.Value);
            }

            var aimData = BotOwner.AimingManager.CurrentAiming;
            if (aimData == null)
            {
                return FriendlyFireStatus.None;
            }


            FriendlyFireStatus friendlyFire = CheckFriendlyFire(aimData.RealTargetPoint);
            if (friendlyFire != FriendlyFireStatus.FriendlyBlock)
            {
                friendlyFire = CheckFriendlyFire(aimData.EndTargetPoint);
            }

            return friendlyFire;
        }

        private FriendlyFireStatus CheckFriendlyFire(Vector3 target)
        {
            var hits = SphereCastAll(target);
            int count = hits.Length;
            if (count == 0)
            {
                return FriendlyFireStatus.None;
            }

            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider == null)
                    continue;

                Player player = GameWorldComponent.Instance.GameWorld.GetPlayerByCollider(hit.collider);
                if (player == null)
                    continue;
                if (player.ProfileId == Bot.ProfileId)
                    continue;

                if (!Bot.EnemyController.IsPlayerAnEnemy(player.ProfileId))
                    return FriendlyFireStatus.FriendlyBlock;
            }
            return FriendlyFireStatus.Clear;
        }

        private RaycastHit[] SphereCastAll(Vector3 target)
        {
            Vector3 firePort = Bot.Transform.WeaponFirePort;
            float distance = (target - firePort).magnitude + 1;
            float sphereCastRadius = 0.2f;
            var hits = Physics.SphereCastAll(firePort, sphereCastRadius, Bot.Transform.WeaponPointDirection, distance, LayerMaskClass.PlayerMask);
            return hits;
        }

        public void StopShooting()
        {
            BotOwner.ShootData?.EndShoot();
        }
    }
}