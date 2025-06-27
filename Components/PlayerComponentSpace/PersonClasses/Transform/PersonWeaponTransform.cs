using EFT;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace.PersonClasses
{
    public class PersonWeaponTransform : PersonSubClass
    {
        public Vector3 FirePort { get; private set; }
        public Vector3 PointDirection { get; private set; }
        public Vector3 Root { get; private set; }

        public void Update()
        {
            Root = _weaponRootTransform.position;
            getWeaponTransforms();
        }

        private void getWeaponTransforms()
        {
            var controller = FirearmController;
            if (controller != null)
            {
                var currentFirePort = controller.CurrentFireport;
                if (currentFirePort != null)
                {
                    Vector3 firePort = currentFirePort.position;
                    Vector3 pointDir = currentFirePort.Original.TransformDirection(Player.LocalShotDirection);
                    controller.AdjustShotVectors(ref firePort, ref pointDir);
                    FirePort = firePort;
                    PointDirection = pointDir;
                    return;
                }
            }

            // we failed to get fireport info, set the positions to a fallback
            FirePort = Root;
            PointDirection = Player.LookDirection;
        }

        public Player.FirearmController FirearmController
        {
            get
            {
                if (_fireArmController == null)
                {
                    _fireArmController = (Player.HandsController as Player.FirearmController);
                }
                return _fireArmController;
            }
        }

        public PersonWeaponTransform(PersonClass person, PlayerData playerData) : base(person, playerData)
        {
            _weaponRootTransform = playerData.Player.WeaponRoot;
        }

        private Player.FirearmController _fireArmController;
        private readonly BifacialTransform _weaponRootTransform;
    }
}