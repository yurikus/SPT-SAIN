using EFT;
using SAIN.Models.Enums;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.WeaponFunction
{
    public class ManualShootClass : BotBase, IBotClass
    {
        public ManualShootClass(BotComponent bot) : base(bot) { }

        public void Init()
        {

        }

        public void Update()
        {
            checkReset();
        }

        public void Dispose()
        {

        }

        private void checkReset()
        {
            if (Reason != EShootReason.None)
            {
                if (!BotOwner.WeaponManager.HaveBullets || _timeStartManualShoot + 2f < Time.time || !BotOwner.ShootData.Shooting)
                {
                    TryShoot(false, Vector3.zero);
                    return;
                }
                if (Shooting && !Bot.FriendlyFire.CheckFriendlyFire(ShootPosition))
                {
                    TryShoot(false, Vector3.zero);
                }
                return;
            }
            if (Shooting && !isEnemyVisibleForShoot(Bot.Enemy) && !isEnemyVisibleForShoot(Bot.LastEnemy))
            {
                //BotOwner.ShootData.EndShoot();
            }
        }

        private bool isEnemyVisibleForShoot(Enemy enemy)
        {
            if (enemy != null && !enemy.IsVisible && enemy.TimeSinceSeen > 0.25f)
            {
                return false;
            }
            return true;
        }

        public bool TryShoot(bool value, Vector3 targetPos, bool checkFF = true, EShootReason reason = EShootReason.None)
        {
            ShootPosition = targetPos;
            Reason = value ? reason : EShootReason.None;

            if (value)
            {
                if (!CanShoot(checkFF))
                {
                    Reason = EShootReason.None;
                    return false;
                }
                //Bot.Steering.LookToPoint(targetPos);
                if (Shooting)
                {
                    return false;
                }
                if (Bot.Steering.AngleToPointFromLookDir(targetPos) > 10)
                {
                    return false;
                }
                if (!Bot.FriendlyFire.CheckFriendlyFire(targetPos))
                {
                    return false;
                }
                if (BotOwner.ShootData.Shoot())
                {
                    _timeStartManualShoot = Time.time;
                    return true;
                }
                return false;
            }
            BotOwner.ShootData.EndShoot();
            Reason = EShootReason.None;
            return false;
        }

        public bool Shooting => BotOwner.ShootData.Shooting;

        public bool CanShoot(bool checkFF = true)
        {
            if (checkFF && !Bot.FriendlyFire.ClearShot)
            {
                //BotOwner.ShootData.EndShoot();
                //return false;
            }
            BotWeaponManager weaponManager = BotOwner.WeaponManager;
            if (weaponManager.IsMelee)
            {
                return false;
            }
            if (!weaponManager.IsWeaponReady)
            {
                return false;
            }
            if (weaponManager.Reload.Reloading)
            {
                return false;
            }
            if (!BotOwner.ShootData.CanShootByState)
            {
                return false;
            }
            if (!weaponManager.HaveBullets)
            {
                return false;
            }
            return true;
        }

        private float _timeStartManualShoot;

        public Vector3 ShootPosition { get; private set; }

        public EShootReason Reason { get; private set; }

    }
}
