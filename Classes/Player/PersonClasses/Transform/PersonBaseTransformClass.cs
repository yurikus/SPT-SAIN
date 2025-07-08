using EFT;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace.PersonClasses
{
    public class PersonBaseTransformClass : PersonSubClass
    {
        public Vector3 Position { get; private set; }
        public Vector3 EyePosition { get; private set; }
        public Vector3 BodyPosition { get; private set; }

        public void Update()
        {
            updateTransform();
        }

        private void updateTransform()
        {
            Position = _transform.position;
            EyePosition = _eyePart.Center;
            BodyPosition = _bodyPart.position;
        }

        public PersonBaseTransformClass(PersonClass person, PlayerData playerData) : base(person, playerData)
        {
            _transform = playerData.Player.Transform;
            var _bones = playerData.Player.PlayerBones;
            _bodyPart = _bones.Ribcage;

            // zombies don't have eye part
            var eyePart = _bones.BodyPartCollidersDictionary.ContainsKey(EBodyPartColliderType.Eyes)
                ? EBodyPartColliderType.Eyes
                : EBodyPartColliderType.HeadCommon;
            _eyePart = _bones.BodyPartCollidersDictionary[eyePart];
        }

        private readonly BifacialTransform _transform;
        private readonly BifacialTransform _bodyPart;
        private readonly BodyPartCollider _eyePart;
    }
}