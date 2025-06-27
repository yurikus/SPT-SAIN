using SAIN.Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes
{
    public sealed class FlankRoute
    {
        public Vector3 FlankPoint;
        public Vector3 FlankPoint2;
        public NavMeshPath FirstPath;
        public NavMeshPath SecondPath;
        public NavMeshPath ThirdPath;
    }

    public class SAINBotSpaceAwareness : BotBase, IBotClass
    {
        public SAINBotSpaceAwareness(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
        }

        public void Update()
        {
            if (Bot.HasEnemy && _findFlankTimer < Time.time && Bot.Enemy?.EnemyPlayer?.IsYourPlayer == true)
            {
                _findFlankTimer = Time.time + 1f;

                if (CurrentFlankRoute != null)
                {
                    Logger.NotifyDebug("Found Flank Route");
                    DrawDebug(CurrentFlankRoute);
                }
            }
        }

        public FlankRoute CurrentFlankRoute { get; private set; }

        private float _findFlankTimer;

        public void Dispose()
        {
        }

        private void DrawDebug(FlankRoute route)
        {
            if (SAINPlugin.DebugMode && SAINPlugin.DrawDebugGizmos)
            {
                if (_timer < Time.time)
                {
                    _timer = Time.time + 60f;
                    list1.DrawTempPath(route.FirstPath, true, RandomColor, RandomColor, 0.1f, 60f);
                    list2.DrawTempPath(route.SecondPath, true, RandomColor, RandomColor, 0.1f, 60f);
                    list3.DrawTempPath(route.ThirdPath, true, RandomColor, RandomColor, 0.1f, 60f);
                    DebugGizmos.Ray(route.FlankPoint, Vector3.up, RandomColor, 3f, 0.2f, true, 60f);
                    DebugGizmos.Ray(route.FlankPoint2, Vector3.up, RandomColor, 3f, 0.2f, true, 60f);
                    DebugGizmos.Line(route.FirstPath.corners[0] + Vector3.up, route.ThirdPath.corners[route.SecondPath.corners.Length - 1] + Vector3.up, Color.white, 0.1f, true, 60f);
                }
            }
            else
            {
            }
        }

        private Color RandomColor = DebugGizmos.RandomColor;

        float _timer;

        DebugGizmos.DrawLists list1 = new(Color.red, Color.red, "flankroute1");
        DebugGizmos.DrawLists list2 = new(Color.blue, Color.blue, "flankroute2");
        DebugGizmos.DrawLists list3 = new(Color.blue, Color.blue, "flankroute3");

        public static bool ArePathsDifferent(NavMeshPath path1, NavMeshPath path2, float minRatio = 0.5f, float sqrDistCheck = 0.05f)
        {
            Vector3[] path1Corners = path1.corners;
            int path1Length = path1Corners.Length;
            Vector3[] path2Corners = path2.corners;
            int path2Length = path2Corners.Length;

            int sameCount = 0;
            for (int i = 0; i < path1Length; i++)
            {
                Vector3 node = path1Corners[i];

                if (i < path2Length)
                {
                    Vector3 node2 = path2Corners[i];
                    if (node.IsEqual(node2, sqrDistCheck))
                    {
                        sameCount++;
                    }
                }
            }
            float ratio = sameCount / path1Length;
            //Logger.LogDebug($"Result = [{ratio <= minRatio}]Path 1 length: {path1.corners.Length} Path2 length: {path2.corners.Length} Same Node Count: {sameCount} ratio: {ratio}");
            return ratio <= minRatio;
        }


        public static float GetSegmentLength(int segmentCount, Vector3 direction, float minLength, float maxLength, out float dirMagnitude, out int countResult, int maxIterations = 10)
        {
            dirMagnitude = direction.magnitude;
            countResult = 0;
            if (dirMagnitude < minLength)
            {
                return 0f;
            }

            float segmentLength = 0f;
            for (int i = 0; i < maxIterations; i++)
            {
                if (segmentCount > 0)
                {
                    segmentLength = dirMagnitude / segmentCount;
                }
                if (segmentLength > maxLength)
                {
                    segmentCount++;
                }
                if (segmentLength < minLength)
                {
                    segmentCount--;
                }
                if (segmentLength <= maxLength && segmentLength >= minLength)
                {
                    break;
                }
                if (segmentCount <= 0)
                {
                    break;
                }
            }
            countResult = segmentCount;
            return segmentLength;
        }
    }
}
