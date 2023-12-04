﻿using System.Collections.Generic;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    /// </summary>
    public class HitTestContext
    {
        //set before hit test
        public static Vector3 screenPoint;
        public static Vector3 worldPoint;
        public static Vector3 direction;
        public static bool forTouch;
        public static Camera camera;

        public static int layerMask = -1;
        public static float maxDistance = Mathf.Infinity;

        public static Camera cachedMainCamera;

        private static readonly Dictionary<Camera, RaycastHit?> raycastHits = new();

        /// <summary>
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="hit"></param>
        /// <returns></returns>
        public static bool GetRaycastHitFromCache(Camera camera, out RaycastHit hit)
        {
            RaycastHit? hitRef;
            if (!raycastHits.TryGetValue(camera, out hitRef))
            {
                var ray = camera.ScreenPointToRay(screenPoint);
                if (Physics.Raycast(ray, out hit, maxDistance, layerMask))
                {
                    raycastHits[camera] = hit;
                    return true;
                }

                raycastHits[camera] = null;
                return false;
            }

            if (hitRef == null)
            {
                hit = new RaycastHit();
                return false;
            }

            hit = (RaycastHit)hitRef;
            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="hit"></param>
        public static void CacheRaycastHit(Camera camera, ref RaycastHit hit)
        {
            raycastHits[camera] = hit;
        }

        /// <summary>
        /// </summary>
        public static void ClearRaycastHitCache()
        {
            raycastHits.Clear();
        }
    }
}