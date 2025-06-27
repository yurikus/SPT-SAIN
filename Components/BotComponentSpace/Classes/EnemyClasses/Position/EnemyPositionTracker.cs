using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyPositionTracker : EnemyBase, IBotEnemyClass
    {
        private const float CHECK_MOVE_DIR_FREQ = 0.25f;
        private const float TRACK_MOVE_AFTER_SENSE_TIME = 3f;
        private const float MOVE_TRACK_LENGTH_CAP = 50f;
        private const float MOVE_TRACK_LENGTH_CAP_SQR = MOVE_TRACK_LENGTH_CAP * MOVE_TRACK_LENGTH_CAP;
        private const float MOVE_TRACK_INCREMENT = 1f;

        public Vector3? EnemyWalkDirection { get; private set; }

        public Vector3? EnemySprintDirection { get; private set; }

        public Vector3 EnemyMoveDirectionTrend { get; private set; }

        public EnemyPositionTracker(Enemy enemy) : base(enemy)
        {
        }

        public void Init()
        {
            Enemy.Events.OnPositionUpdated += positionUpdated;
        }

        public void Update()
        {
            checkTrackMovement();
        }

        public void Dispose()
        {
            Enemy.Events.OnPositionUpdated -= positionUpdated;
        }

        private void checkTrackMovement()
        {
            if (_trackForTime < Time.time)
            {
                Vector3 moveDir = EnemyPlayer.MovementContext.MovementDirection.normalized;
                EnemyMoveDirectionTrend += moveDir;
            }
        }

        public void OnEnemyKnownChanged(bool known, Enemy enemy)
        {
            if (known)
            {
                return;
            }
            EnemyWalkDirection = null;
        }

        private void positionUpdated(Enemy enemy, EnemyPlace place)
        {
            EnemyMoveDirectionTrend = Vector3.zero;
            _trackForTime = Time.time + TRACK_MOVE_AFTER_SENSE_TIME;
        }

        private float _trackForTime;
    }
}