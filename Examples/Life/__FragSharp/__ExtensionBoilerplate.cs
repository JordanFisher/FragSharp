// This file was auto-generated by FragSharp. It will be regenerated on the next compilation.
// Manual changes made will not persist and may cause incorrect behavior between compilations.

using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using FragSharpFramework;

namespace Life
{
    [Hlsl("float4")]
    public partial struct cell
    {
        [Hlsl("float4")]
        public cell(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        [Hlsl("x")]
        public float x;

        [Hlsl("y")]
        public float y;

        [Hlsl("z")]
        public float z;

        [Hlsl("w")]
        public float w;

        [Hlsl("xy")]
        public vec2 xy { get { return new vec2(x, y); } set { x = value.x; y = value.y; } }

        [Hlsl("zw")]
        public vec2 zw { get { return new vec2(z, w); } set { z = value.x; w = value.y; } }

        [Hlsl("xyz")]
        public vec3 xyz { get { return new vec3(x, y, z); } set { x = value.x; y = value.y; z = value.z; } }

        [Hlsl("r")]
        public float r { get { return x; } set { x = value; } }

        [Hlsl("g")]
        public float g { get { return y; } set { y = value; } }

        [Hlsl("b")]
        public float b { get { return z; } set { z = value; } }

        [Hlsl("a")]
        public float a { get { return w; } set { w = value; } }

        [Hlsl("rgb")]
        public vec3 rgb { get { return xyz; } set { xyz = value; } }

        [Hlsl("rg")]
        public vec2 rg { get { return xy; } set { xy = value; } }

        [Hlsl("ba")]
        public vec2 ba { get { return zw; } set { zw = value; } }


        public static cell operator *(float a, cell v)
        {
            return new cell(a * v.x, a * v.y, a * v.z, a * v.w);
        }

        public static cell operator *(cell v, float a)
        {
            return new cell(a * v.x, a * v.y, a * v.z, a * v.w);
        }

        public static cell operator /(float a, cell v)
        {
            return new cell(a / v.x, a / v.y, a / v.z, a / v.w);
        }

        public static cell operator /(cell v, float a)
        {
            return new cell(v.x / a, v.y / a, v.z / a, v.w / a);
        }

        public static cell operator +(cell v, cell w)
        {
            return new cell(v.x + w.x, v.y + w.y, v.z + w.z, v.w + w.w);
        }

        public static cell operator -(cell v, cell w)
        {
            return new cell(v.x - w.x, v.y - w.y, v.z - w.z, v.w - w.w);
        }

        public static cell operator *(cell v, cell w)
        {
            return new cell(v.x * w.x, v.y * w.y, v.z * w.z, v.w * w.w);
        }

        public static cell operator /(cell v, cell w)
        {
            return new cell(v.x / w.x, v.y / w.y, v.z / w.z, v.w / w.w);
        }

        public static bool operator ==(cell v, cell w)
        {
            return
                v.x == w.x &&
                v.y == w.y &&
                v.z == w.z &&
                v.w == w.w;
        }

        public static bool operator !=(cell v, cell w)
        {
            return
                v.x != w.x ||
                v.y != w.y ||
                v.z != w.z ||
                v.w != w.w;
        }

        public static cell operator -(cell v)
        {
            return new cell(-v.x, -v.y, -v.z, -v.w);
        }

        public static implicit operator Vector4(cell v)
        {
            return new Vector4(v.x, v.y, v.z, v.w);
        }

        public static implicit operator cell(color v)
        {
            return new cell(v.x, v.y, v.z, v.w);
        }

        public static implicit operator color(cell v)
        {
            return new color(v.x, v.y, v.z, v.w);
        }

        public static explicit operator cell(Vector4 v)
        {
            return new cell(v.X, v.Y, v.Z, v.W);
        }

        public static readonly cell Zero    = new cell(0, 0, 0, 0);
        public static readonly cell Nothing = new cell(0, 0, 0, 0);

        public static explicit operator cell(vec4 v) { return new cell(v.x, v.y, v.z, v.w); }
        public static explicit operator vec4(cell v) { return new vec4(v.x, v.y, v.z, v.w); }
    }
}

