using SAIN.Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Classes.Transform
{
    public struct PlayerNavData
    {
        private const float ON_NAVMESH_BUFFER_INTERVAL = 0.25F;
        private const float NAV_SAMPLE_RANGE = 0.5f;
        private const float NAVMESH_CHECK_FREQUENCY = 0.5f;
        private const float NAVMESH_CHECK_FREQUENCY_AI = 1f / 60f;
        private const float NAVMESH_DISTANCE_ON = 0.5f;
        private const float NAVMESH_DISTANCE_CLOSE = 1f;
        private const float NAVMESH_DISTANCE_FAR = 2f;

        public EPlayerNavMeshDistance PlayerNavMeshStatus;
        public Vector3 NavMeshPosition;
        public float TimeLastOnNavMesh;
        public bool IsOnNavMesh;

        public float NextCheckTime;

        public static PlayerNavData Update(PlayerNavData navData, Vector3 playerPosition, bool isAI)
        {
            float time = Time.time;
            if (navData.NextCheckTime < time)
            {
                float delay = isAI ? NAVMESH_CHECK_FREQUENCY_AI : NAVMESH_CHECK_FREQUENCY;
                navData.NextCheckTime = time + delay;

                if (NavMesh.SamplePosition(playerPosition, out var hit, NAV_SAMPLE_RANGE, -1))
                {
                    navData.NavMeshPosition = hit.position;
                    navData.PlayerNavMeshStatus = EPlayerNavMeshDistance.OnNavMesh;
                    navData.TimeLastOnNavMesh = time;
                }
                else
                {
                    Vector3 offset = navData.NavMeshPosition - playerPosition;
                    navData.PlayerNavMeshStatus = CalcStatus(offset);
                    if (navData.PlayerNavMeshStatus == EPlayerNavMeshDistance.OnNavMesh)
                        navData.TimeLastOnNavMesh = time;
                }
                //if (DebugSettings.Instance.Gizmos.DrawNavMeshSamplingGizmos)
                //{
                DrawDebugGizmos(navData, playerPosition, delay);
                //}
            }
            navData.IsOnNavMesh = time - navData.TimeLastOnNavMesh < ON_NAVMESH_BUFFER_INTERVAL;
            return navData;
        }

        private static void DrawDebugGizmos(PlayerNavData navData, Vector3 playerPosition, float expireTime)
        {
            Color color;
            if (navData.IsOnNavMesh)
            {
                color = Color.blue;
            }
            else
            {
                color = navData.PlayerNavMeshStatus switch {
                    EPlayerNavMeshDistance.OnNavMesh => Color.green,
                    EPlayerNavMeshDistance.CloseToNavMesh => Color.magenta,
                    EPlayerNavMeshDistance.FarFromNavMesh => Color.yellow,
                    _ => Color.red,
                };
            }
            DebugGizmos.DrawSphere(navData.NavMeshPosition, 0.1f, color, expireTime);
            DebugGizmos.DrawSphere(playerPosition, 0.1f, Color.white, expireTime);
            DebugGizmos.DrawLine(navData.NavMeshPosition, playerPosition, color, 0.1f, expireTime);
        }

        private static EPlayerNavMeshDistance CalcStatus(Vector3 offset)
        {
            float sqrMag = offset.sqrMagnitude;
            if (sqrMag < NAVMESH_DISTANCE_ON * NAVMESH_DISTANCE_ON)
            {
                return EPlayerNavMeshDistance.OnNavMesh;
            }
            if (sqrMag < NAVMESH_DISTANCE_CLOSE * NAVMESH_DISTANCE_CLOSE)
            {
                return EPlayerNavMeshDistance.CloseToNavMesh;
            }
            if (sqrMag < NAVMESH_DISTANCE_FAR * NAVMESH_DISTANCE_FAR)
            {
                return EPlayerNavMeshDistance.FarFromNavMesh;
            }
            return EPlayerNavMeshDistance.OffNavMesh;
        }
    }
}