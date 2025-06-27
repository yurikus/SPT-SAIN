using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using UnityEngine;

namespace SAIN.SAINComponent
{
    public abstract class PlayerComponentBase
    {
        public PlayerComponentBase(PlayerComponent player)
        {
            PlayerComponent = player;
        }

        public PlayerComponent PlayerComponent { get; private set; }

        public Vector3 Position => PlayerComponent.Position;
        public Vector3 LookDirection => PlayerComponent.LookDirection;
        public PersonTransformClass Transform => PlayerComponent.Transform;
        public Player Player => PlayerComponent.Player;
        public IPlayer IPlayer => PlayerComponent.IPlayer;
    }
}