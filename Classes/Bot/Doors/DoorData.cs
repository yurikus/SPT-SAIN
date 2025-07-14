using EFT.Interactive;
using SAIN.Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Mover
{
    public readonly struct DoorLocations
    {
        private static readonly Vector3 handleUpOffset = Vector3.up * 0.25f;
        private static readonly Vector3 floorDownOffset = Vector3.down;

        public DoorLocations(NavMeshDoorLink link)
        {
            Vector3 linkPos = link.transform.position;
            DoorPivotPoint = linkPos + floorDownOffset;
            DoorHandleOpenFloorPoint = link.Open2 + floorDownOffset;
            DoorHandleCloseFloorPoint = link.Close2_Normal + floorDownOffset;

            DoorHandleOpenLookPoint = link.Open2 + handleUpOffset;
            DoorHandleCloseLookPoint = link.Close2_Normal + handleUpOffset;
            NeutralDoorPoint = link.MidClose + handleUpOffset;

            const float backupPointDistCoef = 1.25f;
            Vector3 openDir = DoorHandleOpenFloorPoint - DoorPivotPoint;
            Vector3 closeDir = DoorHandleCloseFloorPoint - DoorPivotPoint;
            Vector3 backupPoint = DoorPivotPoint + ((openDir + closeDir)  * backupPointDistCoef);
            if (NavMesh.SamplePosition(backupPoint, out NavMeshHit hit, 0.5f, -1))
            {
                backupPoint = hit.position;
            }
            DoorBackupPoint = backupPoint;
        }

        public readonly Vector3 DoorPivotPoint;
        public readonly Vector3 DoorHandleOpenLookPoint;
        public readonly Vector3 DoorHandleOpenFloorPoint;
        public readonly Vector3 DoorHandleCloseLookPoint;
        public readonly Vector3 DoorHandleCloseFloorPoint;
        public readonly Vector3 DoorBackupPoint;
        public readonly Vector3 NeutralDoorPoint;
    }
    public class DoorData
    {
        private const float DOOR_INTERACTION_INTERVAL = 2.5f;
        private const float DOOR_OPEN_INTERVAL = 5f;
        private const float DOOR_CLOSE_INTERVAL = 5f;

        public DoorData(NavMeshDoorLink link)
        {
            Link = link;
            LinkPosition = link.transform.position;
            Door = link.Door;
            MoveLocations = new(link);
            //DebugGizmos.DrawSphere(MoveLocations.NeutralDoorPoint, 0.15f, Color.gray);
            //
            //DebugGizmos.DrawSphere(MoveLocations.DoorHandleCloseLookPoint, 0.1f, Color.blue);
            //DebugGizmos.DrawSphere(MoveLocations.DoorHandleCloseFloorPoint, 0.1f, Color.blue);
            //
            //DebugGizmos.DrawSphere(MoveLocations.DoorHandleOpenLookPoint, 0.1f, Color.green);
            //DebugGizmos.DrawSphere(MoveLocations.DoorHandleOpenFloorPoint, 0.1f, Color.green);
            //
            //DebugGizmos.DrawSphere(MoveLocations.DoorPivotPoint, 0.15f, Color.white);
            //DebugGizmos.DrawLine(MoveLocations.DoorPivotPoint, MoveLocations.DoorBackupPoint, Color.red, 0.1f);
            //DebugGizmos.DrawSphere(MoveLocations.DoorBackupPoint, 0.15f, Color.red);
        }

        public DoorLocations MoveLocations { get; }

        public bool CanInteractByTime()
        {
            float time = Time.time;
            if (time - LastInteractTime < DOOR_INTERACTION_INTERVAL)
            {
                return false;
            }
            if (time - LastOpenTime < DOOR_OPEN_INTERVAL)
            {
                return false;
            }
            if (time - LastCloseTime < DOOR_CLOSE_INTERVAL)
            {
                return false;
            }
            return true;
        }

        public float LastInteractTime { get; set; }
        public float LastOpenTime { get; set; }
        public float LastCloseTime { get; set; }

        public NavMeshDoorLink Link { get; }
        public Vector3 LinkPosition { get; }
        public Door Door { get; }

        public float CurrentSqrMagnitude { get; private set; }

        public void ManualUpdate(Vector3 botPosition)
        {
            Direction = LinkPosition - botPosition;
            //Direction = CenterPoint - from;
            DirectionNormal = Direction.normalized;
            CurrentSqrMagnitude = Direction.sqrMagnitude;
        }

        //public Vector3 CenterPoint {
        //    get
        //    {
        //        switch (Door.DoorState)
        //        {
        //            case EDoorState.Open:
        //                return Link.MidOpen;
        //
        //            default:
        //                return Link.MidClose;
        //        }
        //    }
        //}

        public Vector3 Direction { get; private set; }
        public Vector3 DirectionNormal { get; private set; }
        public float DotProduct { get; set; }
    }
}