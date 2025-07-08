using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class TargetData
    {
        public HardTargetData HardData { get; }
        public string TargetProfileID => HardData.ProfileId;
        public Enemy TargetEnemy => HardData.Enemy;
        public Vector3 BotPosition { get; private set; }
        public Vector3 TargetPosition { get; private set; }
        public Vector3 DirBotToTarget { get; private set; }
        public Vector3 DirBotToTargetNormal { get; private set; }
        public float TargetDistance { get; private set; }
        public float TargetDistanceSqr { get; private set; }

        public void Update(Vector3 targetPos, Vector3 botPos)
        {
            BotPosition = botPos;
            TargetPosition = targetPos;
            this.UpdateDirections();
        }

        public void UpdateDirections()
        {
            Vector3 dir = TargetPosition - BotPosition;
            DirBotToTarget = dir;
            float sqrMag = dir.sqrMagnitude;
            TargetDistanceSqr = sqrMag;
            TargetDistance = Mathf.Sqrt(sqrMag);
            DirBotToTargetNormal = dir.normalized;
        }

        public TargetData(Enemy enemy)
        {
            HardData = new HardTargetData(enemy);
        }
    }

    public struct HardTargetData
    {
        public HardTargetData(Enemy enemy)
        {
            ProfileId = enemy.EnemyProfileId;
            Enemy = enemy;
        }
        public string ProfileId;
        public Enemy Enemy;
    }
}