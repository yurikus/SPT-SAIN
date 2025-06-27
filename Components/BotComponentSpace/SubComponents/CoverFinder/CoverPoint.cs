using EFT;
using SAIN.Helpers;
using SAIN.Models.Structs;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class CoverPoint
    {
        public event Action<Vector3> OnPositionUpdated;

        private const int SPOTTED_HITINCOVER_COUNT_TOTAL = 3;
        private const int SPOTTED_HITINCOVER_COUNT_THIRDPARTY = 1;
        private const int SPOTTED_HITINCOVER_COUNT_CANTSEE = 2;
        private const int SPOTTED_HITINCOVER_COUNT_LEGS = 2;
        private const int SPOTTED_HITINCOVER_COUNT_UNKNOWN = 1;
        private const float HITINCOVER_MAX_DAMAGE = 120f;
        private const float HITINCOVER_MIN_DAMAGE = 40f;
        private const float HITINCOVER_DAMAGE_COEF = 3f;
        private const float CHECKDIST_MAX_DIST = 50;
        private const float CHECKDIST_MIN_DIST = 10;
        private const float CHECKDIST_MAX_DELAY = 1f;
        private const float CHECKDIST_MIN_DELAY = 0.1f;
        private const float DIST_COVER_INCOVER = 1f;
        private const float DIST_COVER_INCOVER_STAY = 1.25f;
        private const float DIST_COVER_CLOSE = 10f;
        private const float DIST_COVER_MID = 20f;

        public CoverData CoverData { get; } = new CoverData();

        public Vector3 Position
        {
            get
            {
                return CoverData.Position;
            }
            set
            {
                var current = CoverData.Position;
                if ((value - current).sqrMagnitude < 0.001)
                {
                    //Logger.LogWarning($"new Pos is the same as old pos!");
                    return;
                }
                updateDirAndPos(value);
                OnPositionUpdated?.Invoke(value);
            }
        }

        public float Distance
        {
            get
            {
                if (_nextGetDistTime < Time.time)
                {
                    float dist = (Position - Bot.Position).magnitude;
                    CoverData.BotDistance = dist;
                    _nextGetDistTime = Time.time + calcMagnitudeDelay(dist);
                }
                return CoverData.BotDistance;
            }
        }

        public float PathLength
        {
            get
            {
                return PathData.PathLength;
            }
            set
            {
                PathData.PathLength = value;
            }
        }

        public bool Spotted
        {
            get
            {
                // are we already spotted? check if it has expired
                var hits = _hitsInCover;
                if (hits.Spotted)
                {
                    if (Time.time - hits.TimeSpotted > SpottedCoverPoint.SPOTTED_PERIOD)
                        ResetGetHit();

                    return hits.Spotted;
                }

                // we aren't currently spotted, check to make sure we weren't hit too many times
                hits.Spotted = checkSpotted();

                if (hits.Spotted)
                    hits.TimeSpotted = Time.time;

                return hits.Spotted;
            }
        }

        public CoverStatus StraightDistanceStatus
        {
            get
            {
                float distance = Distance;
                if (CoverData.StraightLengthStatus == CoverStatus.InCover &&
                    distance <= DIST_COVER_INCOVER_STAY)
                {
                    return CoverStatus.InCover;
                }
                CoverData.StraightLengthStatus = checkStatus(distance);
                return CoverData.StraightLengthStatus;
            }
        }

        public CoverStatus PathDistanceStatus
        {
            get
            {
                float pathLength = PathLength;
                if (CoverData.PathLengthStatus == CoverStatus.InCover &&
                    pathLength <= DIST_COVER_INCOVER_STAY)
                {
                    return CoverStatus.InCover;
                }
                CoverData.PathLengthStatus = checkStatus(pathLength);
                return CoverData.PathLengthStatus;
            }
        }
        public NavMeshPath PathToPoint => PathData.Path;
        public float CoverHeight => HardData.Height;
        public Collider Collider => HardColliderData.Collider;
        public SAINHardColliderData HardColliderData { get; }
        public float LastHitInCoverTime { get; private set; }
        public bool IsCurrent => Bot.Cover.CoverInUse == this;

        public bool ShallUpdate(string targetProfileId)
        {
            if (_lastCheckedProfileId != targetProfileId)
            {
                _lastCheckedProfileId = targetProfileId;
                return true;
            }
            float delay = IsCurrent ? 0.2f : 0.5f;
            if (CoverData.TimeSinceUpdated >= delay)
            {
                return true;
            }
            return false;

        }
        private string _lastCheckedProfileId;
        public int RoundedPathLength => PathData.RoundedPathLength;
        public bool BotInThisCover => IsCurrent && (StraightDistanceStatus == CoverStatus.InCover || PathDistanceStatus == CoverStatus.InCover);

        public void GetHit(DamageInfoStruct DamageInfoStruct, EBodyPart partHit, Enemy currentEnemy)
        {
            int hitCount = calcHitCount(DamageInfoStruct);
            bool islegs = partHit.IsLegs();

            var hits = _hitsInCover;
            LastHitInCoverTime = Time.time;
            hits.Total += hitCount;

            IPlayer hitFrom = DamageInfoStruct.Player?.iPlayer;
            if (currentEnemy == null || hitFrom == null)
            {
                hits.Unknown += hitCount;
                return;
            }

            bool sameEnemy = currentEnemy.EnemyPlayer.ProfileId == hitFrom.ProfileId;
            if (!sameEnemy)
            {
                Enemy thirdParty = Bot.EnemyController.GetEnemy(hitFrom.ProfileId, false);
                if (thirdParty == null)
                {
                    hits.Unknown += hitCount;
                    return;
                }

                // Did I get shot in the legs and can't see them?
                if (islegs && thirdParty.IsVisible == false)
                    hits.Legs += hitCount;

                // Did the player who shot me shoot me from a direction that this cover doesn't protect from?
                if (Vector3.Dot(thirdParty.EnemyDirectionNormal, CoverData.ProtectionDirection) < 0.25f)
                    hits.ThirdParty += hitCount;

                return;
            }

            if (!currentEnemy.IsVisible)
            {
                if (islegs)
                    hits.Legs += hitCount;

                hits.CantSee += hitCount;
            }
        }

        public void ResetGetHit()
        {
            _hitsInCover.Reset();
        }

        public CoverPoint(BotComponent bot, SAINHardColliderData colliderData, PathData pathData, Vector3 coverPosition)
        {
            Bot = bot;
            HardColliderData = colliderData;
            PathData = pathData;
            Vector3 size = colliderData.Collider.bounds.size;
            HardData = new SAINHardCoverData
            {
                Id = _count,
                Height = size.y,
                Value = (size.x + size.y + size.z).Round10(),
            };
            updateDirAndPos(coverPosition);
            _count++;
        }

        public SAINHardCoverData HardData { get; }

        public PathData PathData { get; }

        private CoverHitCounts _hitsInCover { get; } = new CoverHitCounts();

        private void updateDirAndPos(Vector3 coverPosition)
        {
            CoverData.Position = coverPosition;
            Vector3 dir = HardColliderData.Position - coverPosition;
            dir.y = 0;
            CoverData.ProtectionDirection = dir.normalized;
            CoverData.TimeLastUpdated = Time.time;
        }

        private CoverStatus checkStatus(float distance)
        {
            if (distance <= DIST_COVER_INCOVER)
                return CoverStatus.InCover;

            if (distance <= DIST_COVER_CLOSE)
                return CoverStatus.CloseToCover;

            if (distance <= DIST_COVER_MID)
                return CoverStatus.MidRangeToCover;

            return CoverStatus.FarFromCover;
        }

        private bool checkSpotted()
        {
            var hits = _hitsInCover;
            int total = hits.Total;
            if (total == 0) return false;

            if (total >= SPOTTED_HITINCOVER_COUNT_TOTAL)
                return true;

            if (hits.CantSee >= SPOTTED_HITINCOVER_COUNT_CANTSEE)
                return true;

            if (hits.Unknown >= SPOTTED_HITINCOVER_COUNT_UNKNOWN)
                return true;

            if (hits.ThirdParty >= SPOTTED_HITINCOVER_COUNT_THIRDPARTY)
                return true;

            if (hits.Legs >= SPOTTED_HITINCOVER_COUNT_LEGS)
                return true;

            return false;
        }

        private int calcHitCount(DamageInfoStruct DamageInfoStruct)
        {
            float received = DamageInfoStruct.Damage;
            float max = HITINCOVER_MAX_DAMAGE;
            float maxCoef = HITINCOVER_DAMAGE_COEF;
            if (received >= max)
            {
                return Mathf.RoundToInt(maxCoef);
            }
            float min = HITINCOVER_MIN_DAMAGE;
            float minCoef = 1f;
            if (received <= min)
            {
                return Mathf.RoundToInt(minCoef);
            }

            float num = max - min;
            float diff = received - min;
            float result = Mathf.Lerp(min, max, diff / num);
            return Mathf.RoundToInt(result);
        }

        private float calcMagnitudeDelay(float dist)
        {
            float maxDelay = CHECKDIST_MAX_DELAY;
            float maxDist = CHECKDIST_MAX_DIST;
            if (dist >= maxDist)
            {
                return maxDelay;
            }
            float minDelay = CHECKDIST_MIN_DELAY;
            float minDist = CHECKDIST_MIN_DIST;
            if (dist <= minDist)
            {
                return minDelay;
            }
            float num = maxDist - minDist;
            float diff = dist - minDist;
            float result = Mathf.Lerp(minDelay, maxDelay, diff / num);
            return result;
        }

        private float _nextGetDistTime;
        private static int _count;
        private readonly BotComponent Bot;
    }
}