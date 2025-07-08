using EFT;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Models.Structs;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace
{
    public struct PlayerDirectionData
    {
        public Vector3 OwnerViewPosition;
        public Vector3 OwnerPosition;
        public Vector3 OwnerLookDirection;

        public DirectionData MainData;
        public BodyPartDirectionData[] BodyParts;
    }

    public struct DirectionData
    {
        public Vector3 Position;
        public Vector3 Direction;
        public Vector3 DirectionNormalized;
        public float Distance;
        public float Dot;
        public float HorizontalAngle;
        public float VerticalAngle;
        public float YDifference;

        public void Update(Vector3 Origin)
        {
            Direction = Position - Origin;
            DirectionNormalized = Direction.normalized;
            Distance = Direction.magnitude;
        }

        public void UpdateDotProductAndCalcNormal(Vector3 Origin, Vector3 LookDirection)
        {
            UpdateDotProduct((Position - Origin).normalized, LookDirection);
        }

        public void UpdateDotProduct(Vector3 DirectionNormal, Vector3 LookDirection)
        {
            HorizontalAngle = EnemyAnglesClass.CalcHorizontalAngle(DirectionNormal, LookDirection);
            VerticalAngle = EnemyAnglesClass.CalcVerticalAngle(DirectionNormal, LookDirection, out float yDiff);
            YDifference = yDiff;
            Dot = Vector3.Dot(LookDirection, DirectionNormal);
        }

        public void UpdateDotProduct(Vector3 LookDirection)
        {
            Dot = Vector3.Dot(LookDirection, DirectionNormalized);
        }
    }

    public struct BodyPartDirectionData(EBodyPart Part)
    {
        public DirectionData DirectionData = new();
        public EBodyPart BodyPart = Part;
    }

    public class PlayerDistanceData
    {
        public PlayerDistanceData(PlayerComponent OtherPlayer)
        {
            var bodyParts = OtherPlayer.BodyParts.PartsArray;
            Data = new() {
                MainData = new(),
                BodyParts = new BodyPartDirectionData[bodyParts.Length]
            };
            for (int i = 0; i < bodyParts.Length; i++)
            {
                Data.BodyParts[i] = new(bodyParts[i].Type);
            }
        }

        public PlayerDirectionData GetPlayerDirectionData() => Data;

        public void SetPlayerDirectionData(PlayerDirectionData data)
        {
            Data = data;
            //foreach (var part in data.BodyParts)
            //    if (BodyPartDirectionData.ContainsKey(part.BodyPart))
            //        BodyPartDirectionData[part.BodyPart] = part;
        }

        public PlayerDirectionData GetUpdatedDirectionData(PlayerComponent Owner, PlayerComponent OtherPlayer)
        {
            PlayerDirectionData data = Data;

            // Prepare the owner's positions
            PersonTransformClass Transform = Owner.Transform;
            data.OwnerPosition = Transform.Position;
            data.OwnerLookDirection = Transform.LookDirection;
            data.OwnerViewPosition = Transform.EyePosition;

            // Prepare the new positions for the other player, and each body part.
            data.MainData.Position = OtherPlayer.Position;
            //PartDictionary otherPlayerParts = OtherPlayer.BodyParts.Parts;
            //var otherPlayerPartDirections = data.BodyParts;
            //for (int i = 0; i < otherPlayerPartDirections.Length; i++)
            //{
            //    if (otherPlayerParts.TryGetValue(otherPlayerPartDirections[i].BodyPart, out SAINBodyPart value))
            //    {
            //        otherPlayerPartDirections[i].DirectionData.Position = value.Transform.position;
            //    }
            //    else
            //    {
            //        otherPlayerPartDirections[i].DirectionData.Position = Vector3.zero;
            //    }
            //}

            Data = data;
            return Data;
        }

        public PlayerDirectionData Data { get; private set; }

        /// <summary>
        /// The Other Player's Position
        /// </summary>
        public Vector3 Position => Data.MainData.Position;

        /// <summary>
        /// Direction from the owner's position to the other player's position.
        /// </summary>
        public Vector3 Direction => Data.MainData.Direction;

        /// <summary>
        /// Normalized Direction from the owner's position to the other player's position.
        /// </summary>
        public Vector3 DirectionNormal => Data.MainData.DirectionNormalized;

        /// <summary>
        /// Dot Product from the owner's look direction to the direction of this player.
        /// </summary>
        public float DotProduct => Data.MainData.Dot;

        /// <summary>
        /// Distance, in Meters, from The Owner to the Other Player.
        /// </summary>
        public float Distance => Data.MainData.Distance;

        /// <summary>
        /// Position, Direction, Dot, ect of each body part of the other player
        /// </summary>
        public Dictionary<EBodyPart, BodyPartDirectionData> BodyPartDirectionData { get; } = new()
        {
            { EBodyPart.Head, new() },
            { EBodyPart.Chest, new() },
            { EBodyPart.Stomach, new() },
            { EBodyPart.LeftArm, new() },
            { EBodyPart.RightArm, new() },
            { EBodyPart.LeftLeg, new() },
            { EBodyPart.RightLeg, new() },
        };

        public BodyPartDirectionData GetBodyPartData(EBodyPart part)
        {
            return BodyPartDirectionData[part];
        }
    }
}