using SAIN.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class ColliderFinder
    {
        static ColliderFinder()
        {
            _orientation = Quaternion.identity;
        }

        public ColliderFinder(CoverFinderComponent component)
        {
            CoverFinderComponent = component;
        }

        private CoverFinderComponent CoverFinderComponent;
        private Vector3 OriginPoint => CoverFinderComponent.OriginPoint;

        public IEnumerator GetNewColliders(Collider[] array, int iterationMax = 10, float startBoxWidth = 2f, int hitThreshold = 100)
        {
            const float StartBoxHeight = 0.25f;
            const float HeightIncreasePerIncrement = 0.5f;
            const float HeightDecreasePerIncrement = 0.5f;
            const float LengthIncreasePerIncrement = 2f;

            clearColliders(array);
            //destroyDebug();

            float boxLength = startBoxWidth;
            float boxHeight = StartBoxHeight;
            Vector3 boxOrigin = OriginPoint + Vector3.up * StartBoxHeight;

            HitCount = 0;
            int hits = 0;
            int totalIterations = 0;
            bool foundEnough = false;
            int layerCount = _layersToCheck.Count;
            for (int l = 0; l < layerCount; l++)
            {
                var layer = _layersToCheck[l];

                for (int i = 0; i < iterationMax; i++)
                {
                    totalIterations++;
                    hits = getCollidersInBox(boxLength, boxHeight, boxLength, boxOrigin, array, layer);
                    foundEnough = hits >= hitThreshold;
                    if (foundEnough)
                    {
                        break;
                    }

                    boxOrigin += Vector3.down * HeightDecreasePerIncrement;
                    boxHeight += HeightIncreasePerIncrement + HeightDecreasePerIncrement;
                    boxLength += LengthIncreasePerIncrement;
                    yield return null;
                }
                if (foundEnough)
                {
                    if (_nextLogTime < Time.time)
                    {
                        _nextLogTime = Time.time + 1f;
                        //Logger.LogInfo($"Found enough colliders in Layer: [{layer.MaskToString()}] after [{totalIterations}] total iterations");
                    }
                    break;
                }
            }

            HitCount = hits;
        }

        private static float _nextLogTime;

        private static List<LayerMask> _layersToCheck = new()
        {
            //LayerMaskClass.HighPolyCollider,
            //LayerMaskClass.TerrainLayer,
            LayerMaskClass.HighPolyWithTerrainMask,
            LayerMaskClass.LowPolyColliderLayerMask,
        };

        private int getCollidersInBox(float x, float y, float z, Vector3 boxOrigin, Collider[] array, LayerMask colliderMask)
        {
            Vector3 box = new(x, y, z);
            int rawHits = Physics.OverlapBoxNonAlloc(boxOrigin, box, array, _orientation, colliderMask);
            return FilterColliders(array, rawHits);
        }

        private static Quaternion _orientation;

        private void destroyDebug()
        {
            for (int i = 0; i < debugObjects.Count; i++)
            {
                GameObject.Destroy(debugObjects[i]);
            }
            debugObjects.Clear();
        }

        public int HitCount;

        private List<GameObject> debugObjects = new();

        private void clearColliders(Collider[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = null;
            }
        }

        /// <summary>
        /// Sorts an array of Colliders based on their Distance from bot's Position. 
        /// </summary>
        /// <param value="array">The array of Colliders to be sorted.</param>
        public void SortArrayBotDist(Collider[] array)
        {
            Array.Sort(array, ColliderArrayBotDistComparer);
        }

        private int FilterColliders(Collider[] array, int hits)
        {
            float minHeight = CoverFinderComponent.CoverMinHeight;
            float minY = CoverFinderComponent.CoverMinHeight;
            const float minX = 0.25f;
            const float minZ = 0.25f;

            int hitReduction = 0;
            for (int i = 0; i < hits; i++)
            {
                Vector3 size = array[i].bounds.size;
                if (size.y < minY || (size.x < minX && size.z < minZ))
                {
                    array[i] = null;
                    hitReduction++;
                }
            }
            //UpdateDebugColliders(array);
            return hits - hitReduction;
        }

        public void UpdateDebugColliders(Collider[] array)
        {
            if (SAINPlugin.DebugMode
                && SAINPlugin.LoadedPreset.GlobalSettings.General.Cover.DebugCoverFinder
                && CoverFinderComponent.Bot.Cover.CurrentCoverFinderState == Classes.CoverFinderState.on)
            {
                foreach (Collider collider in array)
                {
                    if (collider == null) continue;

                    if (!debugGUIObjects.ContainsKey(collider))
                    {
                        var obj = DebugGizmos.CreateLabel(collider.transform.position, collider.name);
                        if (obj != null)
                        {
                            debugGUIObjects.Add(collider, obj);
                        }
                    }
                    if (!debugColliders.ContainsKey(collider))
                    {
                        var marker = DebugGizmos.Sphere(collider.transform.position, 0f);
                        if (marker != null)
                        {
                            debugColliders.Add(collider, marker);
                        }
                    }
                }
            }
            else if (debugGUIObjects.Count > 0
                || debugColliders.Count > 0)
            {
                foreach (var obj in debugGUIObjects)
                {
                    DebugGizmos.DestroyLabel(obj.Value);
                }
                foreach (var obj in debugColliders)
                {
                    GameObject.Destroy(obj.Value);
                }
                debugGUIObjects.Clear();
                debugColliders.Clear();
            }
        }

        private static Dictionary<Collider, GUIObject> debugGUIObjects = new();
        private static Dictionary<Collider, GameObject> debugColliders = new();

        public int ColliderArrayBotDistComparer(Collider A, Collider B)
        {
            if (A == null && B != null)
            {
                return 1;
            }
            else if (A != null && B == null)
            {
                return -1;
            }
            else if (A == null && B == null)
            {
                return 0;
            }
            else
            {
                float AMag = (OriginPoint - A.transform.position).sqrMagnitude;
                float BMag = (OriginPoint - B.transform.position).sqrMagnitude;
                return AMag.CompareTo(BMag);
            }
        }
    }
}