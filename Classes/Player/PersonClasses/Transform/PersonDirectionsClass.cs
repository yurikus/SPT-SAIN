using EFT;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace.PersonClasses
{
    public class PersonDirectionsClass : PersonSubClass
    {
        public Vector3 LookDirection { get; private set; }

        public Vector3 Right()
            => AngledLookDirection(0f, 90f, 0f);

        public Vector3 Left()
            => AngledLookDirection(0f, -90f, 0f);

        public Vector3 Back() => -LookDirection;

        public Vector3 AngledLookDirection(float x, float y, float z)
            => Quaternion.Euler(x, y, z) * LookDirection;

        public void Update()
        {
            LookDirection = Player.MovementContext.LookDirection;
        }

        public PersonDirectionsClass(PersonClass person, PlayerData playerData) : base(person, playerData)
        {
        }
    }
}