using SAIN.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Models.Structs
{
    public struct BotPeekPlan
    {
        public BotPeekPlan(Vector3 start, Vector3 end, Vector3 dangerPoint)
        {
            PeekStart = new PeekPosition(start, dangerPoint);
            PeekEnd = new PeekPosition(end, dangerPoint);
            DangerPoint = dangerPoint;
            DebugVectorList = null;
            DebugGameObjectList = null;
        }

        public PeekPosition PeekStart { get; private set; }
        public PeekPosition PeekEnd { get; private set; }
        public Vector3 DangerPoint { get; private set; }

        private Vector3 MidPoint(Vector3 A, Vector3 B)
        {
            return Vector3.Lerp(A, B, 0.5f);
        }

        private bool CheckIfLeanable(float signAngle, float limit = 1f)
        {
            return Mathf.Abs(signAngle) > limit;
        }

        public LeanSetting GetDirectionToLean(float signAngle)
        {
            if (CheckIfLeanable(signAngle))
            {
                return signAngle > 0 ? LeanSetting.Right : LeanSetting.Left;
            }
            return LeanSetting.None;
        }

        private List<Vector3> DebugVectorList;
        private List<GameObject> DebugGameObjectList;

        public void DrawDebug()
        {
            if (SAINPlugin.DebugMode == false || !SAINPlugin.DebugSettings.Gizmos.DebugSearchGizmos)
            {
                DisposeDebug();
                return;
            }
            if (DebugVectorList == null)
            {
                DebugVectorList = new List<Vector3>
                {
                    PeekStart.Point,
                    PeekEnd.Point,
                    DangerPoint,
                };
            }
            if (DebugGameObjectList == null)
            {
                DebugGameObjectList = DebugGizmos.DrawLinesBetweenPoints(0.1f, 0.05f, DebugVectorList.ToArray());
            }
        }

        public void DisposeDebug()
        {
            if (DebugVectorList != null)
            {
                DebugVectorList.Clear();
                DebugVectorList = null;
            }
            if (DebugGameObjectList != null)
            {
                for (int i = 0; i < DebugGameObjectList.Count; i++)
                {
                    UnityEngine.Object.Destroy(DebugGameObjectList[i]);
                }
                DebugGameObjectList.Clear();
                DebugGameObjectList = null;
            }
        }

    }
}