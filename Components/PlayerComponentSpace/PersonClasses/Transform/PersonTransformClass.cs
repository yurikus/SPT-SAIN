using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace.PersonClasses
{
    public class PersonTransformClass : PersonSubClass
    {
        public PersonBaseTransformClass TransformData { get; }
        public PersonDirectionsClass DirectionData { get; }
        public PersonHeadData HeadData { get; }
        public PersonWeaponTransform WeaponData { get; }
        public PersonVelocityClass VelocityData { get; }
        public NavMeshChecker NavData { get; }

        public Vector3 Position => TransformData.Position;
        public Vector3 EyePosition => TransformData.EyePosition;
        public Vector3 BodyPosition => TransformData.BodyPosition;

        public Vector3 LookDirection => DirectionData.LookDirection;

        public Vector3 HeadLookDirection => HeadData.LookDirection;
        public Vector3 HeadPosition => HeadData.Position;

        public Vector3 WeaponFirePort => WeaponData.FirePort;
        public Vector3 WeaponPointDirection => WeaponData.PointDirection;
        public Vector3 WeaponRoot => WeaponData.Root;

        public float VelocityMagnitudeNormal => VelocityData.MagnitudeNormal;
        public float VelocityMagnitude => VelocityData.Magnitude;
        public Vector3 VelocityVector => VelocityData.Vector;

        public void Update()
        {
            TransformData.Update();
            DirectionData.Update();
            HeadData.Update();
            WeaponData.Update();
            NavData.Update();
            VelocityData.Update();
        }

        public PersonTransformClass(PersonClass person, PlayerData playerData) : base(person, playerData)
        {
            TransformData = new PersonBaseTransformClass(person, playerData);
            NavData = new NavMeshChecker(person, playerData);
            HeadData = new PersonHeadData(person, playerData);
            WeaponData = new PersonWeaponTransform(person, playerData);
            VelocityData = new PersonVelocityClass(person, playerData);
            DirectionData = new PersonDirectionsClass(person, playerData);
        }
    }
}