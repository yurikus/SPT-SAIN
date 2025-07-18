using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.Types.Jobs;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public struct PathSegment
    {
        public Vector3 Corner;
        public Vector3 EndPoint;
        public Vector3 Direction;
        public Vector3 DirectionNormal;
        public float SegmentLength;
        public int Index;
        public List<Vector3> SegmentPoints;
    }

    public class SAINEnemyPath(EnemyData enemy) : EnemyBase(enemy), IBotEnemyClass
    {
        public EPathDistance EPathDistance {
            get
            {
                float distance = PathLength;
                if (distance <= ENEMY_DISTANCE_VERYCLOSE)
                {
                    return EPathDistance.VeryClose;
                }
                if (distance <= ENEMY_DISTANCE_CLOSE)
                {
                    return EPathDistance.Close;
                }
                if (distance <= ENEMY_DISTANCE_MID)
                {
                    return EPathDistance.Mid;
                }
                if (distance <= ENEMY_DISTANCE_FAR)
                {
                    return EPathDistance.Far;
                }
                return EPathDistance.VeryFar;
            }
        }

        private const float ENEMY_DISTANCE_VERYCLOSE = 10f;
        private const float ENEMY_DISTANCE_CLOSE = 20f;
        private const float ENEMY_DISTANCE_MID = 80f;
        private const float ENEMY_DISTANCE_FAR = 150f;

        public float PathLength { get; private set; } = float.MaxValue;

        public float DistanceToEnemyPositionFromLastCorner { get; private set; }

        public NavMeshPath PathToEnemy { get; } = new NavMeshPath();
        public NavMeshPathStatus PathToEnemyStatus { get; private set; }
        public Vector3[] PathCorners { get; private set; }

        public List<BotVisiblePathNode> VisionPathNodes { get; } = [];
        public List<Vector3> VisionPathNodePoints { get; } = [];

        public override void Init()
        {
            Enemy.Events.OnEnemyKnownChanged.OnToggle += OnEnemyKnownChanged;
            base.Init();
        }

        public void OnEnemyKnownChanged(bool known, Enemy enemy)
        {
            if (!known)
            {
                Clear();
            }
        }

        public override void ManualUpdate()
        {
            base.ManualUpdate();
        }

        public override void Dispose()
        {
            Enemy.Events.OnEnemyKnownChanged.OnToggle -= OnEnemyKnownChanged;
            foreach (var node in VisionPathNodes)
                node.Dispose();
            VisionPathNodes.Clear();
            VisionPathNodePoints.Clear();
            PathLength = 0f;
            base.Dispose();
        }

        public void CheckCalcPath()
        {
            if (ShallCalcNewPath())
            {
                Vector3 enemyPosition = Enemy.KnownPlaces.LastKnownPosition.Value;
                PathToEnemy.ClearCorners();
                NavMesh.CalculatePath(Bot.Position, enemyPosition, -1, PathToEnemy);
                PathToEnemyStatus = PathToEnemy.status;
                PathCorners = PathToEnemy.corners;
                CalcPathDistanceAndCreateVisionCheckSegments();
                Enemy.Events.PathUpdated(PathToEnemyStatus);
            }
        }

        public bool ShallCalcNewPath()
        {
            if (Enemy?.EnemyKnown == false)
            {
                return false;
            }

            Vector3? LastKnown = Enemy.KnownPlaces.LastKnownPosition;
            if (LastKnown == null)
            {
                return false;
            }

            bool isCurrentEnemy = Enemy.IsCurrentEnemy;
            if (!isCurrentEnemy && !isEnemyInRange())
            {
                return false;
            }

            // Did we already check the current enemy position and has the bot not moved? dont recalc path then
            if (!checkPositionsChanged(Bot.Position, LastKnown.Value))
            {
                return false;
            }

            if (_calcPathTime + calcDelayOnDistance() > Time.time)
            {
                return false;
            }
            _calcPathTime = Time.time;
            return true;
        }

        private void CalcPathDistanceAndCreateVisionCheckSegments()
        {
            const float CharacterHeight = 1.5f;
            var settings = GlobalSettingsClass.Instance.Steering;
            float distanceBetweenPoints = Enemy.IsAI ? settings.DistanceBetweenPoints_AI : settings.DistanceBetweenPoints;
            int pointCount = Mathf.RoundToInt(settings.GeneratePointStackHeight);

            foreach (var node in VisionPathNodes)
                node.Dispose();
            VisionPathNodes.Clear();
            VisionPathNodePoints.Clear();
            PathLength = 0f;

            int CornerCount = PathCorners.Length;
            for (int i = 0; i < CornerCount - 1; i++)
            {
                Vector3 cornerA = PathCorners[i];
                Vector3 cornerB = PathCorners[i + 1];
                Vector3 cornerDir = cornerB - cornerA;
                Vector3 cornerDirNormal = cornerDir.normalized;
                float Magnitude = cornerDir.magnitude;
                PathLength += Magnitude;

                if (PathLength <= settings.DistToCheckVision)
                {
                    Vector3 step = cornerDirNormal * distanceBetweenPoints;
                    int count = Mathf.FloorToInt(Magnitude / distanceBetweenPoints);
                    for (int j = 0; j <= count; j++)
                    {
                        Vector3 nodePosition = cornerA + step * j;
                        VisionPathNodes.Add(new BotVisiblePathNode(nodePosition, cornerDirNormal, CharacterHeight, pointCount));
                    }
                }
                else if (i == CornerCount - 2) // Last corner?
                {
                    VisionPathNodes.Add(new BotVisiblePathNode(cornerB, cornerDirNormal, CharacterHeight, pointCount));
                }
            }

            //int max = Mathf.RoundToInt(Enemy.IsAI ? GlobalSettingsClass.Instance.Steering.MaxPathPoints_AI : GlobalSettingsClass.Instance.Steering.MaxPathPoints);
            for (int i = 0; i < VisionPathNodes.Count; i++)
            {
                BotVisiblePathNode node = VisionPathNodes[i];
                for (int j = 0; j < node.PointStack.Length; j++)
                {
                    VisionPathNodePoints.Add(node.PointStack[j].Point);
                }
                //if (VisionPathNodePoints.Count >= max)
                //{
                //    break;
                //}
            }

            //Logger.LogDebug($"Found [{VisionPathNodePoints.Count}] Total Points in [{VisionPathNodes.Count}] Nodes");
            //if (Enemy.Player.IsYourPlayer)
            //{
            //foreach (var node in VisionPathNodes)
            //{
                //DebugGizmos.DrawSphere(node.GroundPosition, 0.075f, Color.yellow, 2f);
                //foreach (var point in node.PointStack)
                //{
                //    DebugGizmos.DrawSphere(point.Point, 0.075f, Color.yellow, 2f);
                //}
            //}

            //GameObject line = DebugGizmos.DrawLine(Vector3.zero, Vector3.forward, Color.yellow, 0.1f, 2f);
            //Vector3[] linePoints = new Vector3[VisionPathNodes.Count];
            //for (int i = 0; i < linePoints.Length; i++)
            //{
            //    linePoints[i] = VisionPathNodes[i].GroundPosition;
            //}
            //DebugGizmos.SetLinePositions(line, linePoints);
            //}

            if (CornerCount > 0)
                DistanceToEnemyPositionFromLastCorner = (Enemy.LastKnownPosition.Value - PathCorners[CornerCount - 1]).magnitude;
            else
                DistanceToEnemyPositionFromLastCorner = 0;
        }

        public void Clear()
        {
            _calcPathTime = 0;
            PathToEnemy.ClearCorners();
            PathToEnemyStatus = NavMeshPathStatus.PathInvalid;
            PathLength = float.MaxValue;
            PathCorners = null;
            DistanceToEnemyPositionFromLastCorner = 0;

            foreach (var node in VisionPathNodes) node.Dispose();
            VisionPathNodes.Clear();
            VisionPathNodePoints.Clear();
        }

        private float calcDelayOnDistance()
        {
            bool performanceMode = SAINPlugin.LoadedPreset.GlobalSettings.General.Performance.PerformanceMode;
            bool currentEnemy = Enemy.IsCurrentEnemy;
            bool isAI = Enemy.IsAI;
            bool searchingForEnemy = Enemy.Events.OnSearch.Value;
            float distance = Enemy.RealDistance;

            float maxDelay = isAI ? MAX_FREQ_CALCPATH_AI : MAX_FREQ_CALCPATH;
            if (currentEnemy)
                maxDelay *= CURRENTENEMY_COEF;
            if (performanceMode)
                maxDelay *= PERFORMANCE_MODE_COEF;
            if (searchingForEnemy)
                maxDelay *= ACTIVE_SEARCH_COEF;

            if (distance > MAX_FREQ_CALCPATH_DISTANCE)
            {
                return maxDelay;
            }

            float minDelay = isAI ? MIN_FREQ_CALCPATH_AI : MIN_FREQ_CALCPATH;
            if (currentEnemy)
                minDelay *= CURRENTENEMY_COEF;
            if (performanceMode)
                minDelay *= PERFORMANCE_MODE_COEF;
            if (searchingForEnemy)
                minDelay *= ACTIVE_SEARCH_COEF;

            if (distance < MIN_FREQ_CALCPATH_DISTANCE)
            {
                return minDelay;
            }

            float difference = distance - MIN_FREQ_CALCPATH_DISTANCE;
            float distanceRatio = difference / DISTANCE_DIFFERENCE;
            float delayDifference = maxDelay - minDelay;

            float result = distanceRatio * delayDifference + minDelay;
            float clampedResult = Mathf.Clamp(result, minDelay, maxDelay);

            if (_nextLogTime < Time.time)
            {
                _nextLogTime = Time.time + 10f;
                //Logger.LogDebug($"{BotOwner.name} calcPathFreqResults for [{Enemy.EnemyPerson.Nickname}] Result: [{result}] preClamped: [[{result}] [{distanceRatio} * {delayDifference} + {minDelay}]] : Distance: [{distance}] : IsAI? [{isAI}] : Current Enemy? [{currentEnemy}] : MinDelay [{minDelay}] : MaxDelay [{maxDelay}]");
            }

            return clampedResult;
        }

        private float _nextLogTime;

        private const float ACTIVE_SEARCH_COEF = 0.5f;

        private const float MAX_FREQ_CALCPATH = 2f;
        private const float MAX_FREQ_CALCPATH_AI = 4f;
        private const float MAX_FREQ_CALCPATH_DISTANCE = 250f;

        private const float MIN_FREQ_CALCPATH = 0.33f;
        private const float MIN_FREQ_CALCPATH_AI = 0.66f;
        private const float MIN_FREQ_CALCPATH_DISTANCE = 50f;

        private const float DISTANCE_DIFFERENCE = MAX_FREQ_CALCPATH_DISTANCE - MIN_FREQ_CALCPATH_DISTANCE;
        private const float PERFORMANCE_MODE_COEF = 1.5f;
        private const float CURRENTENEMY_COEF = 0.5f;

        private bool isEnemyInRange()
        {
            return Enemy.IsAI && Enemy.RealDistance <= MAX_CALCPATH_RANGE_AI ||
                !Enemy.IsAI && Enemy.RealDistance <= MAX_CALCPATH_RANGE;
        }

        private const float MAX_CALCPATH_RANGE = 500f;
        private const float MAX_CALCPATH_RANGE_AI = 300f;

        private bool checkPositionsChanged(Vector3 botPosition, Vector3 enemyPosition)
        {
            if (Enemy.Events.OnSearch.Value)
            {
                return true;
            }
            // Did we already check the current enemy position and has the bot not moved? dont recalc path then
            if (_enemyLastPosChecked != null
                && (_enemyLastPosChecked.Value - enemyPosition).sqrMagnitude < 0.025f
                && (_botLastPosChecked - botPosition).sqrMagnitude < 0.025f)
            {
                return false;
            }

            // cache the positions we are currently checking
            _enemyLastPosChecked = enemyPosition;
            _botLastPosChecked = botPosition;
            return true;
        }

        public float CalculatePathLength(Vector3[] corners)
        {
            if (corners == null)
            {
                return float.MaxValue;
            }
            float result = 0f;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Vector3 a = corners[i];
                Vector3 b = corners[i + 1];
                result += (a - b).magnitude;
            }
            return result;
        }

        private Vector3? _enemyLastPosChecked;
        private Vector3 _botLastPosChecked;
        private float _calcPathTime = 0f;
    }
}