using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace
{
    public class PlayerDistanceData
    {
        public Vector3 Position { get; private set; } = Vector3.zero;
        public Vector3 Direction { get; private set; } = Vector3.zero;
        public Vector3 DirectionNormal { get; private set; } = Vector3.zero;
        public float Distance { get; private set; } = float.MaxValue;

        public readonly Dictionary<EBodyPart, float> BodyPartDistances = new()
        {
            { EBodyPart.Head, float.MaxValue },
            { EBodyPart.Chest, float.MaxValue },
            { EBodyPart.Stomach, float.MaxValue },
            { EBodyPart.LeftArm, float.MaxValue },
            { EBodyPart.RightArm, float.MaxValue },
            { EBodyPart.LeftLeg, float.MaxValue },
            { EBodyPart.RightLeg, float.MaxValue },
        };

        public void Update(Vector3 position, Vector3 direction, Vector3 directionNormal, float distance)
        {
            Position = position;
            Direction = direction;
            DirectionNormal = directionNormal;
            Distance = distance;
        }

        public void UpdateBodyPart(EBodyPart part, float distance)
        {
            if (BodyPartDistances.ContainsKey(part))
                BodyPartDistances[part] = distance;
        }
    }
}