using SAIN.Helpers;
using SAIN.Models.Enums;
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

    public class SAINEnemyPath(Enemy enemy) : EnemyBase(enemy), IBotEnemyClass
    {
        public EPathDistance EPathDistance {
            get
            {
                float distance = PathDistance;
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

        public float PathDistance { get; private set; } = float.MaxValue;

        public float DistanceToEnemyPositionFromLastCorner { get; private set; }

        public EnemyCornerDictionary EnemyCorners { get; private set; } = new EnemyCornerDictionary(enemy.Bot.Transform, enemy.BotOwner.WeaponRoot);

        public NavMeshPath PathToEnemy { get; } = new NavMeshPath();
        public NavMeshPathStatus PathToEnemyStatus { get; private set; }

        protected List<PathSegment> PathVisionSegments = [];
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
            CheckCalcPath();
            base.ManualUpdate();
        }

        public override void Dispose()
        {
            Enemy.Events.OnEnemyKnownChanged.OnToggle -= OnEnemyKnownChanged;
            base.Dispose();
        }

        public void CheckCalcPath()
        {
            if (!ShallCalcNewPath())
            {
                return;
            }

            Vector3 enemyPosition = Enemy.KnownPlaces.LastKnownPosition.Value;
            PathToEnemy.ClearCorners();
            NavMesh.CalculatePath(Bot.Position, enemyPosition, -1, PathToEnemy);
            PathToEnemyStatus = PathToEnemy.status;
            PathCorners = PathToEnemy.corners;
            CalcPathDistanceAndCreateVisionCheckSegments();
            switch (PathToEnemyStatus)
            {
                case NavMeshPathStatus.PathInvalid:
                    EnemyCorners.Clear();
                    break;

                case NavMeshPathStatus.PathPartial:
                case NavMeshPathStatus.PathComplete:
                    findCorners(enemyPosition, PathToEnemyStatus, PathCorners);
                    break;
            }

            Enemy.Events.PathUpdated(PathToEnemyStatus);
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
            const float DistToCheckVision = 50.0f;
            const float CharacterHeight = 1.5f;
            const int GeneratePointStackHeight = 4;
            const int MaxPathPoints = 512;
            const int MaxPathPoints_AI = 128;
            const float DistanceBetweenPoints = 0.25f;
            const float DistanceBetweenPoints_AI = 0.5f;

            float distanceBetweenPoints = Enemy.IsAI ? DistanceBetweenPoints_AI : DistanceBetweenPoints;

            PathVisionSegments.Clear();
            VisionPathCheckPoints.Clear();
            PathDistance = 0f;
            int CornerCount = PathCorners.Length;
            for (int i = 0; i < CornerCount - 1; i++)
            {
                Vector3 Corner = PathCorners[i];
                Vector3 End = PathCorners[i + 1];
                //Vector3 Direction = Corner - End; OLD
                Vector3 Direction = End - Corner;
                float Magnitude = Direction.magnitude;
                PathDistance += Magnitude;

                // Dont include the corner index 0, as it is what the bot's position is, we dont need to see if thats visible or not.
                // Only add a segment if we are under our maximum length, or if we have no segments at all.
                if (i > 0)
                {
                    bool firstCorner = i == 1;
                    if (firstCorner)
                        VisionPathCheckPoints.Add(Corner);
                    bool lastCorner = i == CornerCount - 2;
                    if (PathDistance <= DistToCheckVision)
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

            int max = Enemy.IsAI ? MaxPathPoints_AI : MaxPathPoints;
            VisionPathPoints.Clear();
            for (int i = 0; i < VisionPathCheckPoints.Count; i++)
            {
                Vector.GeneratePointsAlongDirection(VisionPathPoints, VisionPathCheckPoints[i], Vector3.up, CharacterHeight, CharacterHeight / GeneratePointStackHeight);
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

            if (CornerCount > 0)
                DistanceToEnemyPositionFromLastCorner = (Enemy.LastKnownPosition.Value - PathCorners[CornerCount - 1]).magnitude;
            else
                DistanceToEnemyPositionFromLastCorner = 0;
        }

        private void findCorners(Vector3 enemyPosition, NavMeshPathStatus status, Vector3[] corners)
        {
            EnemyCorner first = findFirstCorner(enemyPosition, corners);
            EnemyCorners.AddOrReplace(ECornerType.First, first);

            EnemyCorner last = findLastCorner(enemyPosition, status, corners);
            EnemyCorners.AddOrReplace(ECornerType.Last, last);

            EnemyCorner lastKnown = createLastKnownCorner(enemyPosition, corners.Length - 1);
            EnemyCorners.AddOrReplace(ECornerType.LastKnown, lastKnown);
        }

        public void Clear()
        {
            _calcPathTime = 0;
            PathToEnemy.ClearCorners();
            PathToEnemyStatus = NavMeshPathStatus.PathInvalid;
            EnemyCorners.Clear();
            PathDistance = float.MaxValue;
            PathCorners = null;
            DistanceToEnemyPositionFromLastCorner = 0;
            PathVisionSegments.Clear();
            VisionPathCheckPoints.Clear();
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

        private EnemyCorner findLastCorner(Vector3 enemyPosition, NavMeshPathStatus pathStatus, Vector3[] corners)
        {
            int length = corners.Length;
            // find the last corner before arriving at an enemy position, and then check if we can see it.
            Vector3 lastCorner;
            int index;
            if (pathStatus == NavMeshPathStatus.PathComplete &&
                length > 2)
            {
                index = length - 2;
                lastCorner = corners[index];
            }
            else
            {
                index = length - 1;
                lastCorner = corners[index];
            }

            float signedAngle = Vector.FindFlatSignedAngle(lastCorner, enemyPosition, Bot.Transform.EyePosition);
            return new EnemyCorner(lastCorner, signedAngle, index);
        }

        private EnemyCorner findFirstCorner(Vector3 enemyPosition, Vector3[] corners)
        {
            if (corners.Length < 2)
            {
                return null;
            }

            Vector3 firstCorner = corners[1];
            float signedAngle = Vector.FindFlatSignedAngle(firstCorner, enemyPosition, Bot.Transform.EyePosition);
            return new EnemyCorner(firstCorner, signedAngle, 1);
        }

        private EnemyCorner createLastKnownCorner(Vector3 enemyPosition, int index)
        {
            return new EnemyCorner(enemyPosition, 0f, index);
        }

        private Vector3? _enemyLastPosChecked;
        private Vector3 _botLastPosChecked;
        private float _calcPathTime = 0f;
    }
}