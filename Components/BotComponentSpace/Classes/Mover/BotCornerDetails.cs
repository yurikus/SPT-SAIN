using UnityEngine;

namespace SAIN.SAINComponent.Classes.Mover
{
    public struct BotCornerDetails
    {
        public void SetStarted(float currentTime)
        {
            Status = EBotCornerStatus.Active;
            TimeStarted = currentTime;
        }

        public void SetComplete(float currentTime)
        {
            Status = EBotCornerStatus.Used;
            TimeComplete = currentTime;
        }

        public void UpdateType(EBotCornerType type)
        {
            Type = type;
        }

        /// <summary>
        /// Set the direction property, and calculate length
        /// </summary>
        /// <param name="direction"></param>
        public void SetDirection(Vector3 direction)
        {
            Direction = direction;
            Length = direction.magnitude;
        }

        public readonly bool ShortCorner => Type == EBotCornerType.PathShortTurn;
        public readonly bool LastCorner => Type == EBotCornerType.PathEnd;

        public EBotCornerStatus Status;
        public EBotCornerType Type;
        public int Index;
        public Vector3 Position;
        public Vector3 Direction;
        public float Length;
        public float TimeStarted;
        public float TimeComplete;

        public static BotCornerDetails Create(ref Vector3[] corners, float shortCornerConfigDistance, int count, int i)
        {
            BotCornerDetails details = new() {
                Position = corners[i],
                Index = i,
                Status = EBotCornerStatus.Awaiting,
            };
            if (i < count - 1)
            {
                details.Direction = corners[i + 1] - details.Position;
                details.Length = details.Direction.magnitude;
                if (i == 0)
                {
                    details.Type = EBotCornerType.PathStart;
                }
                else if (i < count - 2)
                {
                    bool shortTurn = details.Length <= shortCornerConfigDistance;
                    details.Type = shortTurn ? EBotCornerType.PathShortTurn : EBotCornerType.PathTurn;
                }
                else
                {
                    details.Type = EBotCornerType.PathEndApproach;
                }
            }
            else if (i == count - 1)
            {
                details.Type = EBotCornerType.PathEnd;
            }
            return details;
        }

        public static BotCornerDetails Create(Vector3 corner, Vector3 nextCorner, EBotCornerType Type, int index)
        {
            BotCornerDetails details = new() {
                Position = corner,
                Index = index,
                Status = EBotCornerStatus.Awaiting,
                Type = Type,
            };
            details.SetDirection(nextCorner - corner);
            return details;
        }

        public static BotCornerDetails Create(Vector3 corner, EBotCornerType Type, int index)
        {
            BotCornerDetails details = new() {
                Position = corner,
                Index = index,
                Status = EBotCornerStatus.Awaiting,
                Type = Type,
            };
            return details;
        }
    }
}