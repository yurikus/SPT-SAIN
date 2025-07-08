using EFT;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace.PersonClasses
{
    public class PersonHeadData : PersonSubClass
    {
        private const float TRANSFORM_UPDATE_HEADLOOK_FREQ = 1f / 30f;
        public Vector3 LookDirection { get; private set; }
        public Vector3 Position { get; private set; }
        public BifacialTransform HeadTransform { get; }

        public void Update()
        {
            Position = HeadTransform.position;
            updateHeadLook();
        }

        private void updateHeadLook()
        {
            if (_nextUpdateHeadLookTime <= Time.time)
            {
                _nextUpdateHeadLookTime = Time.time + TRANSFORM_UPDATE_HEADLOOK_FREQ;
                //HeadLookDirection = Quaternion.Euler(_myHead.localRotation.y, _myHead.localRotation.x, 0) * _myHead.forward;
                Vector3 headLookDir = Quaternion.Euler(0, HeadTransform.rotation.x + 90, 0) * HeadTransform.forward;
                headLookDir.y = Person.Transform.LookDirection.y;
                LookDirection = headLookDir;
            }
        }

        public PersonHeadData(PersonClass person, PlayerData playerData) : base(person, playerData)
        {
            HeadTransform = playerData.Player.PlayerBones.Head;
        }

        private float _nextUpdateHeadLookTime;
    }
}