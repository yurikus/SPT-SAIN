using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class TargetData(Enemy enemy, BotComponent bot)
    {
        public Enemy TargetEnemy { get; private set; } = enemy;

        public Vector3 BotPosition { get; private set; }
        public Vector3 TargetPosition { get; private set; }
        public Vector3 DirBotToTarget { get; private set; }
        public Vector3 DirBotToTargetNormal { get; private set; }
        public float TargetDistance { get; private set; }
        public float TargetDistanceSqr { get; private set; }

        public void Update()
        {
            BotPosition = _bot.Transform.NavData.Position;
            TargetPosition = TargetEnemy.LastKnownPosition.Value;
            DirBotToTarget =  TargetPosition - BotPosition;
            TargetDistanceSqr = DirBotToTarget.sqrMagnitude;;
            TargetDistance = TargetEnemy.KnownPlaces.BotDistanceFromLastKnown;
            DirBotToTargetNormal = DirBotToTarget.normalized;
        }

        private readonly BotComponent _bot = bot;
    }
}