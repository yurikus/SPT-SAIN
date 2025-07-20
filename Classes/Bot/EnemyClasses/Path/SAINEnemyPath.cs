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

        
        /// <summary>
        /// Ground Positions
        /// </summary>
        public List<Vector3> VisionPathCheckPoints { get; } = [];

        /// <summary>
        /// Raycast Positions
        /// </summary>
        public List<Vector3> VisionPathPoints { get; } = [];

        /// <summary>
        /// Raycast Positions Cached, this list is used by the job so it should not be altered here.
        /// </summary>
        public List<Vector3> VisionPathPoints_Cache { get; } = [];

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

            VisionPathCheckPoints.Clear();
            PathLength = 0f;
            int CornerCount = PathCorners.Length;
            for (int i = 0; i < CornerCount - 1; i++)
            {
                Vector3 Corner = PathCorners[i];
                Vector3 End = PathCorners[i + 1];
                Vector3 Direction = End - Corner;
                float Magnitude = Direction.magnitude;
                PathLength += Magnitude;

                // Dont include the corner index 0, as it is what the bot's position is, we dont need to see if thats visible or not.
                // Only add a segment if we are under our maximum length, or if we have no segments at all.
                if (i > 0)
                {
                    bool firstCorner = i == 1;
                    if (firstCorner)
                        VisionPathCheckPoints.Add(Corner);
                    bool lastCorner = i == CornerCount - 2;
                    if (PathLength <= settings.DistToCheckVision)
                    {
                        // Create Equal dist points along the line between two corners.
                        if (Magnitude > distanceBetweenPoints)
                        {
                            if (Magnitude > distanceBetweenPoints * 2f)
                            {
                                Vector.GeneratePointsAlongDirection(VisionPathCheckPoints, Corner, Direction, Magnitude, distanceBetweenPoints);
                            }
                            else
                            {
                                VisionPathCheckPoints.Add(Corner + Direction * 0.5f);
                            }
                        }
                        VisionPathCheckPoints.Add(End);
                    }
                    else if (lastCorner)
                    {
                        VisionPathCheckPoints.Add(End);
                    }
                }
            }

            int max = Mathf.RoundToInt(Enemy.IsAI ? settings.MaxPathPoints_AI : settings.MaxPathPoints);
            VisionPathPoints.Clear();
            for (int i = 0; i < VisionPathCheckPoints.Count; i++)
            {
                Vector.GeneratePointsAlongDirection(VisionPathPoints, VisionPathCheckPoints[i], Vector3.up, CharacterHeight, CharacterHeight / settings.GeneratePointStackHeight);
                if (VisionPathPoints.Count >= max)
                {
                    break;
                }
            }


            //if (EnemyPlayer.IsYourPlayer)
            //{
            //    foreach (var point in VisionPathCheckPoints)
            //    {
            //        DebugGizmos.Sphere(point + Vector3.up * 0.5f, 0.025f, Color.white, true, 0.5f);
            //    }
            //}

            VisionPathCheckPoints.Clear();

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
            VisionPathCheckPoints.Clear();
            VisionPathPoints.Clear();
        }

        private float calcDelayOnDistance()
        {
            bool performanceMode = SAINPlugin.LoadedPreset.GlobalSettings.General.Performance.PerformanceMode;
            bool currentEnemy = Enemy.IsCurrentEnemy;
            bool isAI = Enemy.IsAI;
            //bool searchingForEnemy = Enemy.Events.OnSearch.Value;
            float distance = Enemy.RealDistance;

            float maxDelay = isAI ? MAX_FREQ_CALCPATH_AI : MAX_FREQ_CALCPATH;
            if (currentEnemy)
                maxDelay *= CURRENTENEMY_COEF;
            if (performanceMode)
                maxDelay *= PERFORMANCE_MODE_COEF;
            //if (searchingForEnemy)
            //    maxDelay *= ACTIVE_SEARCH_COEF;

            if (distance > MAX_FREQ_CALCPATH_DISTANCE)
            {
                return maxDelay;
            }

            float minDelay = isAI ? MIN_FREQ_CALCPATH_AI : MIN_FREQ_CALCPATH;
            if (currentEnemy)
                minDelay *= CURRENTENEMY_COEF;
            if (performanceMode)
                minDelay *= PERFORMANCE_MODE_COEF;
            //if (searchingForEnemy)
            //    minDelay *= ACTIVE_SEARCH_COEF;

            if (distance < MIN_FREQ_CALCPATH_DISTANCE)
            {
                return minDelay;
            }

            float difference = distance - MIN_FREQ_CALCPATH_DISTANCE;
            float distanceRatio = difference / DISTANCE_DIFFERENCE;
            float delayDifference = maxDelay - minDelay;

            float result = distanceRatio * delayDifference + minDelay;
            float clampedResult = Mathf.Clamp(result, minDelay, maxDelay);

            //if (_nextLogTime < Time.time)
            //{
            //    _nextLogTime = Time.time + 10f;
            //    //Logger.LogDebug($"{BotOwner.name} calcPathFreqResults for [{Enemy.EnemyPerson.Nickname}] Result: [{result}] preClamped: [[{result}] [{distanceRatio} * {delayDifference} + {minDelay}]] : Distance: [{distance}] : IsAI? [{isAI}] : Current Enemy? [{currentEnemy}] : MinDelay [{minDelay}] : MaxDelay [{maxDelay}]");
            //}

            return clampedResult;
        }

        private float _nextLogTime;

        private const float ACTIVE_SEARCH_COEF = 0.5f;

        private const float MAX_FREQ_CALCPATH = 3f;
        private const float MAX_FREQ_CALCPATH_AI = 6f;
        private const float MAX_FREQ_CALCPATH_DISTANCE = 250f;

        private const float MIN_FREQ_CALCPATH = 1f;
        private const float MIN_FREQ_CALCPATH_AI = 2f;
        private const float MIN_FREQ_CALCPATH_DISTANCE = 25f;

        private const float DISTANCE_DIFFERENCE = MAX_FREQ_CALCPATH_DISTANCE - MIN_FREQ_CALCPATH_DISTANCE;
        private const float PERFORMANCE_MODE_COEF = 1.5f;
        private const float CURRENTENEMY_COEF = 0.25f;

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
                && (_enemyLastPosChecked.Value - enemyPosition).sqrMagnitude < 0.1f
                && (_botLastPosChecked - botPosition).sqrMagnitude < 0.1f)
            {
                return false;
            }

            // cache the positions we are currently checking
            _enemyLastPosChecked = enemyPosition;
            _botLastPosChecked = botPosition;
            return true;
        }

        private Vector3? _enemyLastPosChecked;
        private Vector3 _botLastPosChecked;
        private float _calcPathTime = 0f;
    }
}