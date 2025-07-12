using EFT;
using SAIN.Classes.Transform;
using SAIN.Components.PlayerComponentSpace;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyData(Enemy enemy)
    {
        public Enemy Enemy { get; } = enemy;
        public EnemyInfo EnemyInfo { get; } = enemy.EnemyInfo;
        public PlayerTransformClass EnemyTransform { get; } = enemy.EnemyTransform;
        public PlayerComponent EnemyPlayerComponent { get; } = enemy.EnemyPlayerComponent;
        public Player EnemyPlayer { get; } = enemy.EnemyPlayer;
        public Vector3 EnemyCurrentPosition => EnemyTransform.Position;
    }

    public abstract class EnemyBase : BotBase
    {
        protected EnemyBase(EnemyData enemyData) : base(enemyData.Enemy.Bot)
        {
            _enemyData = enemyData;
            Enemy.OnEnemyDisposed += Dispose;
        }

        public override void Dispose()
        {
            Enemy.OnEnemyDisposed -= Dispose;
            base.Dispose();
        }

        protected Enemy Enemy => _enemyData.Enemy;
        protected EnemyInfo EnemyInfo => _enemyData.EnemyInfo;
        protected PlayerTransformClass EnemyTransform => _enemyData.EnemyTransform;
        protected PlayerComponent EnemyPlayerComponent => _enemyData.EnemyPlayerComponent;
        protected Player EnemyPlayer => _enemyData.EnemyPlayer;
        protected Vector3 EnemyCurrentPosition => _enemyData.EnemyCurrentPosition;

        private readonly EnemyData _enemyData;
    }
}