using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Preset;
using System;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public abstract class EnemyBase : BotBase
    {
        protected EnemyBase(Enemy enemy) : base(enemy.Bot)
        {
            Enemy = enemy;
            enemy.OnEnemyDisposed += Dispose;
        }

        public override void Dispose()
        {
            Enemy.OnEnemyDisposed -= Dispose;
            base.Dispose();
        }

        protected Enemy Enemy { get; }
        protected EnemyInfo EnemyInfo => Enemy.EnemyInfo;
        protected PersonClass EnemyPerson => Enemy.EnemyPerson;
        protected PlayerComponent EnemyPlayerComponent => EnemyPerson.PlayerComponent;
        protected Player EnemyPlayer => EnemyPerson.Player;
        protected IPlayer EnemyIPlayer => EnemyPerson.IPlayer;
        protected PersonTransformClass EnemyTransform => EnemyPerson.Transform;
        protected Vector3 EnemyCurrentPosition => EnemyTransform.Position;
    }
}