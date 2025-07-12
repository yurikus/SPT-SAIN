using UnityEngine;
using UnityEngine.AI;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;

namespace SAIN.Classes.Transform
{
    public struct PlayerNavData
    {
        private const float NAV_SAMPLE_RANGE = 0.5f;
        private const float NAVMESH_CHECK_FREQUENCY = 0.5f;
        private const float NAVMESH_CHECK_FREQUENCY_AI = 1f / 60f;
        private const float NAVMESH_DISTANCE_ON = 0.25f;
        private const float NAVMESH_DISTANCE_CLOSE = 1f;
        private const float NAVMESH_DISTANCE_FAR = 2f;

        public EPlayerNavMeshDistance PlayerNavMeshStatus;
        public Vector3 NavMeshPosition;

        /// <summary>
        /// Direction from the Player's Real position, to their NavMeshPosition
        /// </summary>
        public Vector3 NavMeshOffset;

        public float NextCheckTime;

        public static PlayerNavData Update(PlayerNavData navData, Vector3 playerPosition, bool isAI)
        {
            float delay = isAI ? NAVMESH_CHECK_FREQUENCY_AI : NAVMESH_CHECK_FREQUENCY;
            navData.NextCheckTime = Time.time + delay;

            if (NavMesh.SamplePosition(playerPosition, out var hit, NAV_SAMPLE_RANGE, -1))
                navData.NavMeshPosition = hit.position;

            navData.NavMeshOffset = navData.NavMeshPosition - playerPosition;
            navData.PlayerNavMeshStatus = CalcStatus(navData.NavMeshOffset);
            if (DebugSettings.Instance.Gizmos.DrawNavMeshSamplingGizmos)
            {
                DrawDebugGizmos(navData, playerPosition, delay);
            }
            return navData;
        }

        private static void DrawDebugGizmos(PlayerNavData navData, Vector3 playerPosition, float expireTime)
        {
            Color color = navData.PlayerNavMeshStatus switch {
                EPlayerNavMeshDistance.OnNavMesh => Color.blue,
                EPlayerNavMeshDistance.CloseToNavMesh => Color.cyan,
                EPlayerNavMeshDistance.FarFromNavMesh => Color.yellow,
                _ => Color.red,
            };
            DebugGizmos.DrawSphere(navData.NavMeshPosition, 0.25f, color, expireTime);
            DebugGizmos.DrawLine(navData.NavMeshPosition, playerPosition, color, 0.05f, expireTime);
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