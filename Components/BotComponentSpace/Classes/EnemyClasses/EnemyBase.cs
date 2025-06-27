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
        public EnemyBase(Enemy enemy) : base(enemy.Bot)
        {
            Enemy = enemy;
            enemy.OnEnemyDisposed += dispose;
        }

        private void dispose()
        {
            Enemy.OnEnemyDisposed -= dispose;
            UnSubscribeToPreset();
            _disposeFunc?.Invoke();
        }

        public void SubscribeToDispose(Action disposeFunc)
        {
            if (disposeFunc == null)
            {
                Logger.LogError("Dispose Func is null!");
                return;
            }
            _disposeFunc = disposeFunc;
        }

        protected override void SubscribeToPreset(Action<SAINPresetClass> func)
        {
            if (func != null)
            {
                func.Invoke(SAINPresetClass.Instance);
                _autoUpdater.Subscribe(func);
            }
        }

        protected override void UnSubscribeToPreset()
        {
            if (_autoUpdater.Subscribed)
            {
                _autoUpdater.UnSubscribe();
            }
        }

        private Action _disposeFunc;

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