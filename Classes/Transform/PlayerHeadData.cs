using UnityEngine;

namespace SAIN.Classes.Transform
{
    public struct PlayerHeadData
    {
        public Vector3 HeadLookDirection;
        public Vector3 HeadPosition;

        public static PlayerHeadData Update(PlayerHeadData headData, Vector3 headPosition, Quaternion headRotation, Vector3 headForward)
        {
            //HeadLookDirection = Quaternion.Euler(_myHead.localRotation.y, _myHead.localRotation.x, 0) * _myHead.forward;
            headData.HeadPosition = headPosition;
            //Vector3 euler = headRotation.eulerAngles;
            //Vector3 headLookDir = Quaternion.Euler(0, euler.y + 90, 0) * headForward;
            Vector3 rotatedLook = headRotation * headForward;
            headData.HeadLookDirection = rotatedLook;
            return headData;
        }
    }
}