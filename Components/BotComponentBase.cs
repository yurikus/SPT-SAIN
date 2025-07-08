using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using System;
using UnityEngine;

namespace SAIN.Components
{
    public abstract class BotComponentBase : MonoBehaviour, IDisposable
    {
        public event Action OnDispose;

        public string ProfileId { get; private set; }
        public PersonClass Person { get; private set; }

        public PlayerComponent PlayerComponent => Person.PlayerComponent;
        public BotOwner BotOwner => Person.AIInfo.BotOwner;
        public Player Player => Person.Player;
        public PersonTransformClass Transform => Person.Transform;

        public Vector3 Position => Person.Transform.Position;
        public Vector3 LookDirection => Person.Transform.LookDirection;

        public virtual bool Init(PersonClass person)
        {
            if (person == null || person.Player == null)
            {
                return false;
            }
            Person = person;
            ProfileId = person.ProfileId;
            person.Player.ActiveHealthController.SetDamageCoeff(1f);
            return true;
        }

        public virtual void Dispose()
        {
            OnDispose?.Invoke();
        }
    }
}