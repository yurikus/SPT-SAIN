using EFT;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Models.Enums;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyCornerDictionary : Dictionary<ECornerType, EnemyCorner>
    {
        public EnemyCornerDictionary(PersonTransformClass transform, BifacialTransform weaponRoot)
        {
            _transform = transform;
            _weaponRoot = weaponRoot;
        }

        private BifacialTransform _weaponRoot;

        public Vector3? GroundPosition(ECornerType type)
        {
            var corner = GetCorner(type);
            return corner?.GroundPosition;
        }

        public Vector3? EyeLevelPosition(ECornerType type)
        {
            var corner = GetCorner(type);
            return corner?.EyeLevelCorner(_weaponRoot.position + Vector3.down * 0.15f, _transform.Position);
        }

        public Vector3? PointPastCorner(ECornerType type)
        {
            var corner = GetCorner(type);
            return corner?.PointPastCorner(_weaponRoot.position, _transform.Position);
        }

        private PersonTransformClass _transform;

        public EnemyCorner GetCorner(ECornerType cornerType)
        {
            if (this.TryGetValue(cornerType, out EnemyCorner corner))
            {
                return corner;
            }
            return null;
        }

        public void AddOrReplace(ECornerType type, EnemyCorner corner)
        {
            if (corner == null)
            {
                this.Remove(type);
                return;
            }
            if (!this.ContainsKey(type))
            {
                this.Add(type, corner);
                return;
            }
            this[type] = corner;
        }
    }
}