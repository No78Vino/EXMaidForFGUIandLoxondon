using System;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    /// </summary>
    public class TweenValue
    {
        /// <summary>
        /// </summary>
        public double d;

        /// <summary>
        /// </summary>
        public float w;

        /// <summary>
        /// </summary>
        public float x;

        /// <summary>
        /// </summary>
        public float y;

        /// <summary>
        /// </summary>
        public float z;

        /// <summary>
        /// </summary>
        public Vector2 vec2
        {
            get => new(x, y);
            set
            {
                x = value.x;
                y = value.y;
            }
        }

        /// <summary>
        /// </summary>
        public Vector3 vec3
        {
            get => new(x, y, z);
            set
            {
                x = value.x;
                y = value.y;
                z = value.z;
            }
        }

        /// <summary>
        /// </summary>
        public Vector4 vec4
        {
            get => new(x, y, z, w);
            set
            {
                x = value.x;
                y = value.y;
                z = value.z;
                w = value.w;
            }
        }

        /// <summary>
        /// </summary>
        public Color color
        {
            get => new(x, y, z, w);
            set
            {
                x = value.r;
                y = value.g;
                z = value.b;
                w = value.a;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return x;
                    case 1:
                        return y;
                    case 2:
                        return z;
                    case 3:
                        return w;
                    default:
                        throw new Exception("Index out of bounds: " + index);
                }
            }

            set
            {
                switch (index)
                {
                    case 0:
                        x = value;
                        break;
                    case 1:
                        y = value;
                        break;
                    case 2:
                        z = value;
                        break;
                    case 3:
                        w = value;
                        break;
                    default:
                        throw new Exception("Index out of bounds: " + index);
                }
            }
        }

        /// <summary>
        /// </summary>
        public void SetZero()
        {
            x = y = z = w = 0;
            d = 0;
        }
    }
}