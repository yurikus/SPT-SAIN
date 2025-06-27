using Comfort.Common;
using EFT;
using SAIN.Helpers;
using SAIN.Models.Structs;
using SAIN.SAINComponent.Classes;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class CoverAnalyzer : BotBase
    {
        public CoverAnalyzer(BotComponent botOwner, CoverFinderComponent coverFinder) : base(botOwner)
        {
            CoverFinder = coverFinder;
        }

        private readonly CoverFinderComponent CoverFinder;

        public bool CheckCollider(Collider collider, TargetData targetData, out CoverPoint coverPoint, out string reason)
        {
            coverPoint = null;
            SAINHardColliderData hardData = new(collider);
            ColliderData colliderData = new(hardData, targetData);

            if (!GetPlaceToMove(colliderData, hardData, targetData, out Vector3 coverPosition))
            {
                reason = "noPlaceToMove";
                return false;
            }

            if (!checkPositionVsOtherBots(coverPosition))
            {
                reason = "tooCloseToAnotherBot";
                return false;
            }

            if (isPositionSpotted(coverPosition))
            {
                reason = "tooCloseToSpottedPoint";
                return false;
            }

            if (!checkDistToTarget(coverPosition, targetData))
            {
                reason = "tooCloseToTarget";
                return false;
            }

            if (!visibilityCheck(coverPosition, targetData, colliderData, hardData))
            {
                reason = "pointVisibleToTarget";
                return false;
            }

            PathData pathData = new(new NavMeshPath());
            if (!CheckPath(coverPosition, pathData, targetData))
            {
                reason = "badPath";
                return false;
            }

            reason = string.Empty;
            coverPoint = new CoverPoint(Bot, hardData, pathData, coverPosition);
            return true;
        }

        public bool RecheckCoverPoint(CoverPoint coverPoint, TargetData targetData, out string reason)
        {
            var hardData = coverPoint.HardColliderData;
            ColliderData colliderData = new(hardData, targetData);

            if (!GetPlaceToMove(colliderData, hardData, targetData, out Vector3 coverPosition))
            {
                reason = "noPlaceToMove";
                return false;
            }

            if (!checkPositionVsOtherBots(coverPosition))
            {
                reason = "tooCloseToAnotherBot";
                return false;
            }

            if (coverPoint.StraightDistanceStatus == CoverStatus.InCover)
            {
                coverPoint.Position = coverPosition;
                reason = "inCover";
                return true;
            }

            if (isPositionSpotted(coverPosition))
            {
                reason = "tooCloseToSpottedPoint";
                return false;
            }

            if (!checkDistToTarget(coverPosition, targetData))
            {
                reason = "tooCloseToTarget";
                return false;
            }

            if (!visibilityCheck(coverPosition, targetData, colliderData, hardData))
            {
                reason = "pointVisibleToTarget";
                return false;
            }

            if (!CheckPath(coverPosition, coverPoint.PathData, targetData))
            {
                reason = "badPath";
                return false;
            }

            coverPoint.Position = coverPosition;
            reason = string.Empty;
            return true;
        }

        public bool GetPlaceToMove(ColliderData colliderData, SAINHardColliderData hardData, TargetData targetDirections, out Vector3 place)
        {
            if (!checkColliderDirectionvsTargetDirection(colliderData, targetDirections))
            {
                place = Vector3.zero;
                return false;
            }
            if (!findSampledPosition(colliderData, hardData, POSITION_SAMPLE_RANGE, out place))
            {
                place = Vector3.zero;
                return false;
            }

            return true;
        }

        private const float POSITION_FINAL_MIN_DOT = 0.5f;
        private const float POSITION_EDGE_MIN_DOT = 0.5f;
        private const float POSTIION_EDGE_SAMPLE_RANGE = 0.5f;
        private const float POSITION_SAMPLE_RANGE = 1f;

        private bool checkFinalPositionDirection(ColliderData colliderDirs, SAINHardColliderData hardData, TargetData targetDirs, Vector3 place)
        {
            Vector3 dirToPlace = place - hardData.Position;
            Vector3 dirToPlaceNormal = dirToPlace.normalized;
            Vector3 dirToColliderNormal = colliderDirs.dirTargetToColliderNormal;
            float dot = Vector3.Dot(dirToPlaceNormal, dirToColliderNormal);
            return dot > POSITION_FINAL_MIN_DOT;
        }

        private bool findSampledPosition(ColliderData colliderDirs, SAINHardColliderData hardData, float navSampleRange, out Vector3 coverPosition)
        {
            Vector3 samplePos = hardData.Position + colliderDirs.dirTargetToColliderNormal;
            if (!NavMesh.SamplePosition(samplePos, out var hit, navSampleRange, -1))
            {
                coverPosition = Vector3.zero;
                return false;
            }
            coverPosition = findEdge(hit.position, colliderDirs);
            return true;
        }

        private Vector3 findEdge(Vector3 navMeshHit, ColliderData colliderDirs)
        {
            if (NavMesh.FindClosestEdge(navMeshHit, out var edge, -1))
            {
                Vector3 edgeNormal = edge.normal;
                Vector3 targetNormal = colliderDirs.dirTargetToColliderNormal;
                if (Vector3.Dot(edgeNormal, targetNormal) > POSITION_EDGE_MIN_DOT)
                {
                    Vector3 edgeCover = edge.position + colliderDirs.dirTargetToColliderNormal;
                    if (NavMesh.SamplePosition(edgeCover, out var edgeHit, POSTIION_EDGE_SAMPLE_RANGE, -1))
                    {
                        return edgeHit.position;
                    }
                }
            }
            return navMeshHit;
        }

        private bool checkColliderDirectionvsTargetDirection(ColliderData colliderDirs, TargetData targetDirs)
        {
            float dot = Vector3.Dot(targetDirs.DirBotToTargetNormal, colliderDirs.dirBotToColliderNormal);

            if (dot <= 0.33f)
            {
                return true;
            }
            float colliderDist = colliderDirs.ColliderDistanceToBot;
            float targetDist = targetDirs.TargetDistance;
            if (dot <= 0.5f)
            {
                return colliderDist < targetDist * 0.75f;
            }
            if (dot <= 0.66f)
            {
                return colliderDist < targetDist * 0.66f;
            }
            return colliderDist < targetDist * 0.5f;
        }

        private bool CheckPosition(Vector3 coverPosition, TargetData targetData, ColliderData colliderData, SAINHardColliderData hardData)
        {
            return (coverPosition - targetData.TargetPosition).sqrMagnitude > CoverMinEnemyDistSqr &&
                !isPositionSpotted(coverPosition) &&
                checkPositionVsOtherBots(coverPosition) &&
                visibilityCheck(coverPosition, targetData, colliderData, hardData);
        }

        private bool checkDistToTarget(Vector3 coverPosition, TargetData data)
        {
            return (coverPosition - data.TargetPosition).sqrMagnitude > CoverMinEnemyDistSqr;
        }

        private bool isPositionSpotted(Vector3 position)
        {
            foreach (var point in CoverFinder.SpottedCoverPoints)
            {
                Vector3 coverPos = point.CoverPoint.Position;
                if (!point.IsValidAgain &&
                    point.TooClose(coverPos, position))
                {
                    return true;
                }
            }
            return false;
        }

        private bool CheckPath(Vector3 position, PathData pathData, TargetData targetData)
        {
            var path = pathData.Path;
            path.ClearCorners();
            NavMesh.CalculatePath(OriginPoint, position, -1, path);

            if (path.status != NavMeshPathStatus.PathComplete)
            {
                return false;
            }

            float pathLength = path.CalculatePathLength();
            if (pathLength > SAINPlugin.LoadedPreset.GlobalSettings.General.Cover.MaxCoverPathLength)
            {
                return false;
            }
            pathData.PathLength = pathLength;

            if (!checkPathToEnemy(path, targetData))
            {
                return false;
            }
            return true;
        }

        private const float PATH_SAME_DIST_MIN_RATIO = 0.66f;
        private const float PATH_SAME_CHECK_DIST = 0.1f;
        private const float PATH_NODE_MIN_DIST_SQR = 0.25f;
        private const float PATH_NODE_FIRST_DOT_MAX = 0.5f;

        private bool checkPathToEnemy(NavMeshPath path, TargetData targetData)
        {
            if (!SAINBotSpaceAwareness.ArePathsDifferent(path, targetData.TargetEnemy.Path.PathToEnemy, PATH_SAME_DIST_MIN_RATIO, PATH_SAME_CHECK_DIST))
            {
                return false;
            }

            Vector3 botToTargetNormal = targetData.DirBotToTargetNormal;

            for (int i = 1; i < path.corners.Length - 1; i++)
            {
                var corner = path.corners[i];
                Vector3 cornerToTarget = TargetPoint - corner;
                Vector3 botToCorner = corner - OriginPoint;

                if (cornerToTarget.sqrMagnitude < PATH_NODE_MIN_DIST_SQR)
                {
                    if (DebugCoverFinder)
                    {
                        //DrawDebugGizmos.Ray(OriginPoint, corner - OriginPoint, Color.red, (corner - OriginPoint).magnitude, 0.05f, true, 30f);
                    }
                    return false;
                }

                if (i == 1)
                {
                    if (Vector3.Dot(botToCorner.normalized, botToTargetNormal) > PATH_NODE_FIRST_DOT_MAX)
                    {
                        if (DebugCoverFinder)
                        {
                            //DrawDebugGizmos.Ray(corner, cornerToTarget, Color.red, cornerToTarget.magnitude, 0.05f, true, 30f);
                        }
                        return false;
                    }
                }
                else if (i < path.corners.Length - 2)
                {
                    Vector3 cornerB = path.corners[i + 1];
                    Vector3 directionToNextCorner = cornerB - corner;

                    if (Vector3.Dot(cornerToTarget.normalized, directionToNextCorner.normalized) > 0.5f)
                    {
                        if (directionToNextCorner.sqrMagnitude > cornerToTarget.sqrMagnitude)
                        {
                            if (DebugCoverFinder)
                            {
                                //DrawDebugGizmos.Ray(corner, cornerToTarget, Color.red, cornerToTarget.magnitude, 0.05f, true, 30f);
                            }
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private bool checkPositionVsOtherBots(Vector3 position)
        {
            string profileID = Bot.ProfileId;

            if (checkIfPlayerCollidersNear(position, profileID, 0.5f))
            {
                return false;
            }

            var members = Bot.Squad.Members;

            if (members != null)
            {
                foreach (var member in Bot.Squad.Members.Values)
                {
                    if (member == null || member.ProfileId == profileID) continue;
                    var coverPoints = member.Cover.CoverPoints;
                    foreach (var point in coverPoints)
                    {
                        if (isDistanceTooClose(point, position))
                            return false;

                    }
                }
            }

            return true;
        }

        private static bool checkIfPlayerCollidersNear(Vector3 point, string botProfileId, float radius)
        {
            for (int i = 0; i < _playerColliderArray.Length; i++)
            {
                _playerColliderArray[i] = null;
            }

            Physics.OverlapSphereNonAlloc(point, radius, _playerColliderArray, LayerMaskClass.PlayerMask);

            int count = 0;
            Collider foundCollider = null;
            foreach (var collider in _playerColliderArray)
            {
                if (collider == null) continue;
                count++;
                if (count > 1)
                {
                    return true;
                }
                foundCollider = collider;
            }
            if (count == 0)
            {
                return false;
            }

            var player = Singleton<GameWorld>.Instance?.GetPlayerByCollider(foundCollider);
            if (player == null)
            {
                return false;
            }
            if (player.ProfileId == botProfileId)
            {
                return false;
            }
            return true;
        }

        private static Collider[] _playerColliderArray = new Collider[5];

        private bool isDistanceTooClose(CoverPoint point, Vector3 position)
        {
            const float DistanceToBotCoverThresh = 0.5f;
            const float distSqr = DistanceToBotCoverThresh * DistanceToBotCoverThresh;
            return point != null && (position - point.Position).sqrMagnitude < distSqr;
        }

        private static bool visibilityCheck(Vector3 position, TargetData targetData, ColliderData colliderData, SAINHardColliderData hardColliderData)
        {
            const float offset = 0.1f;

            float distanceToCollider = (hardColliderData.Position - position).magnitude * 1.25f;
            //Logger.LogDebug($"visCheck: Dist To Collider: {distanceToCollider}");

            Vector3 target = targetData.TargetPosition;
            if (!checkRaycastToCoverCollider(position, target, out RaycastHit hit, distanceToCollider))
            {
                return false;
            }

            Vector3 enemyDirection = targetData.DirBotToTargetNormal * offset;
            Quaternion right = Quaternion.Euler(0f, 90f, 0f);
            Vector3 rightPoint = right * enemyDirection;

            if (!checkRaycastToCoverCollider(position + rightPoint, target, out hit, distanceToCollider))
            {
                return false;
            }

            if (!checkRaycastToCoverCollider(position - rightPoint, target, out hit, distanceToCollider))
            {
                return false;
            }

            return true;
        }

        private static bool checkRaycastToCoverCollider(Vector3 point, Vector3 target, out RaycastHit hit, float distance)
        {
            point.y += 0.5f;
            target.y += 1.25f;
            Vector3 direction = target - point;
            bool hitObject = Physics.Raycast(point, direction, out hit, distance, LayerMaskClass.HighPolyWithTerrainMask);

            if (DebugCoverFinder)
            {
                if (hitObject)
                {
                    DebugGizmos.Line(point, hit.point, Color.white, 0.1f, true, 10f);
                }
                else
                {
                    Vector3 testPoint = direction.normalized * distance + point;
                    DebugGizmos.Line(point, testPoint, Color.red, 0.1f, true, 10f);
                }
            }
            return hitObject;
        }

        private Vector3 OriginPoint => CoverFinder.OriginPoint;
        private Vector3 TargetPoint => CoverFinder.TargetPoint;
        private float CoverMinEnemyDistSqr => CoverFinderComponent.CoverMinEnemyDistSqr;
        private static bool DebugCoverFinder => CoverFinderComponent.DebugCoverFinder;
        private static float CoverMinHeight => CoverFinderComponent.CoverMinHeight;
    }
}