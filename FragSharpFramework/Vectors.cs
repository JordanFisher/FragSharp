using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FragSharpFramework
{
    [Hlsl("float2")]
    public struct RelativeIndex
    {
        [Hlsl("x")]
        public float i;
        [Hlsl("y")]
        public float j;

        [Hlsl("float2")]
        public RelativeIndex(float i, float j) { this.i = i; this.j = j; }

        public static RelativeIndex operator *(float a, RelativeIndex v)
        {
            return new RelativeIndex(a * v.i, a * v.j);
        }

        public static RelativeIndex operator *(RelativeIndex v, float a)
        {
            return new RelativeIndex(a * v.i, a * v.j);
        }

        public static RelativeIndex operator /(float a, RelativeIndex v)
        {
            return new RelativeIndex(a / v.i, a / v.j);
        }

        public static RelativeIndex operator /(RelativeIndex v, float a)
        {
            return new RelativeIndex(v.i / a, v.j / a);
        }

        public static RelativeIndex operator +(RelativeIndex v, RelativeIndex w)
        {
            return new RelativeIndex(v.i + w.i, v.j + w.j);
        }

        public static RelativeIndex operator -(RelativeIndex v, RelativeIndex w)
        {
            return new RelativeIndex(v.i - w.i, v.j - w.j);
        }

        public static RelativeIndex operator *(RelativeIndex v, RelativeIndex w)
        {
            return new RelativeIndex(v.i * w.i, v.j * w.j);
        }

        public static RelativeIndex operator /(RelativeIndex v, RelativeIndex w)
        {
            return new RelativeIndex(v.i / w.i, v.j / w.j);
        }

        public static RelativeIndex operator -(RelativeIndex v)
        {
            return new RelativeIndex(-v.i, -v.j);
        }

        public static implicit operator Vector2(RelativeIndex v)
        {
            return new Vector2(v.i, v.j);
        }

        public static implicit operator vec2(RelativeIndex v)
        {
            return new vec2(v.i, v.j);
        }

        public static explicit operator RelativeIndex(vec2 v)
        {
            return new RelativeIndex(v.x, v.y);
        }

        public static explicit operator RelativeIndex(Vector2 v)
        {
            return new RelativeIndex(v.X, v.Y);
        }
    }

    [Hlsl("float2")]
    public partial struct vec2
    {
        [Hlsl("float2")]
        public vec2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        [Hlsl("x")]
        public float x;

        [Hlsl("y")]
        public float y;

        public override string ToString()
        {
            return string.Format("{0}, {1}", x, y);
        }

        public static vec2 Parse(string s)
        {
            var parts = s.Split(',');
            return new vec2(float.Parse(parts[0]), float.Parse(parts[1]));
        }

        public float LengthSquared()
        {
            return x * x + y * y;
        }

        public float Length()
        {
            return (float)Math.Sqrt(LengthSquared());
        }

        public vec2 FlipX()
        {
            return new vec2(-x, y);
        }

        public vec2 FlipY()
        {
            return new vec2(x, -y);
        }

        public static vec2 operator *(float a, vec2 v)
        {
            return new vec2(a * v.x, a * v.y);
        }

        public static vec2 operator *(vec2 v, float a)
        {
            return new vec2(a * v.x, a * v.y);
        }

        public static vec2 operator /(float a, vec2 v)
        {
            return new vec2(a / v.x, a / v.y);
        }

        public static vec2 operator /(vec2 v, float a)
        {
            return new vec2(v.x / a, v.y / a);
        }

        public static vec2 operator +(vec2 v, vec2 w)
        {
            return new vec2(v.x + w.x, v.y + w.y);
        }

        public static vec2 operator -(vec2 v, vec2 w)
        {
            return new vec2(v.x - w.x, v.y - w.y);
        }

        public static vec2 operator *(vec2 v, vec2 w)
        {
            return new vec2(v.x * w.x, v.y * w.y);
        }

        public static vec2 operator /(vec2 v, vec2 w)
        {
            return new vec2(v.x / w.x, v.y / w.y);
        }

        public static vec2 operator -(vec2 v)
        {
            return new vec2(-v.x, -v.y);
        }

        public static bool operator ==(vec2 v, vec2 w)
        {
            return v.x == w.x && v.y == w.y;
        }

        public static bool operator !=(vec2 v, vec2 w)
        {
            return v.x != w.x || v.y != w.y;
        }

        public static bool operator >(vec2 v, vec2 w)
        {
            return v.x > w.x && v.y > w.y;
        }

        public static bool operator <(vec2 v, vec2 w)
        {
            return v.x < w.x && v.y < w.y;
        }

        public override bool Equals(object o)
        {
            return o is vec2 ? this == (vec2)o : false;
        }

        public bool Equals(vec2 v)
        {
            return this == v;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode();
        }

        public static implicit operator Vector2(vec2 v)
        {
            return new Vector2(v.x, v.y);
        }

        public static explicit operator vec2(Vector2 v)
        {
            return new vec2(v.X, v.Y);
        }

        public static readonly vec2 Zero = new vec2(0, 0);
        public static readonly vec2 Ones = new vec2(1, 1);
    }

    [Hlsl("float3")]
    public partial struct vec3
    {
        [Hlsl("float3")]
        public vec3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        [Hlsl("x")]
        public float x;

        [Hlsl("y")]
        public float y;

        [Hlsl("z")]
        public float z;

        [Hlsl("xy")]
        public vec2 xy { get { return new vec2(x, y); } set { x = value.x; y = value.y; } }

        public static vec3 operator *(float a, vec3 v)
        {
            return new vec3(a * v.x, a * v.y, a * v.z);
        }

        public static vec3 operator *(vec3 v, float a)
        {
            return new vec3(a * v.x, a * v.y, a * v.z);
        }

        public static vec3 operator /(float a, vec3 v)
        {
            return new vec3(a / v.x, a / v.y, a / v.z);
        }

        public static vec3 operator /(vec3 v, float a)
        {
            return new vec3(v.x / a, v.y / a, v.z / a);
        }

        public static vec3 operator +(vec3 v, vec3 w)
        {
            return new vec3(v.x + w.x, v.y + w.y, v.z + w.z);
        }

        public static vec3 operator -(vec3 v, vec3 w)
        {
            return new vec3(v.x - w.x, v.y - w.y, v.z - w.z);
        }

        public static vec3 operator *(vec3 v, vec3 w)
        {
            return new vec3(v.x * w.x, v.y * w.y, v.z * w.z);
        }

        public static vec3 operator /(vec3 v, vec3 w)
        {
            return new vec3(v.x / w.x, v.y / w.y, v.z / w.z);
        }

        public static bool operator ==(vec3 v, vec3 w)
        {
            return
                v.x == w.x &&
                v.y == w.y &&
                v.z == w.z;
        }

        public static bool operator !=(vec3 v, vec3 w)
        {
            return
                v.x != w.x ||
                v.y != w.y ||
                v.z != w.z;
        }

        public override bool Equals(object o)
        {
            return o is vec3 ? this == (vec3)o : false;
        }

        public bool Equals(vec3 v)
        {
            return this == v;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
        }

        public static vec3 operator -(vec3 v)
        {
            return new vec3(-v.x, -v.y, -v.z);
        }

        public static implicit operator Vector3(vec3 v)
        {
            return new Vector3(v.x, v.y, v.z);
        }

        public static explicit operator vec3(Vector3 v)
        {
            return new vec3(v.X, v.Y, v.Z);
        }

        public static readonly vec3 Zero = new vec3(0, 0, 0);
    }

    [Hlsl("float4")]
    public partial struct vec4 : Convertible</*KeepInCopy*/ vec4, vec4>
    {
        public vec4 ConvertFrom(/*KeepInCopy*/ vec4 v)
        {
            return (vec4)v;
        }

        public /*KeepInCopy*/ vec4 ConvertTo()
        {
            return (/*KeepInCopy*/ vec4)this;
        }

        [Hlsl("float4")]
        public vec4(float x, float y, float z, float w)
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

        [Hlsl("yzw")]
        public vec3 yzw { get { return new vec3(y, z, w); } set { y = value.x; z = value.y; w = value.z; } }

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

        [Hlsl("gba")]
        public vec3 gba { get { return yzw; } set { yzw = value; } }

        [Hlsl("rg")]
        public vec2 rg { get { return xy; } set { xy = value; } }

        [Hlsl("ba")]
        public vec2 ba { get { return zw; } set { zw = value; } }

        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                    case 3: return w;
                    default: throw new Exception("Invalid index.");
                }
            }
        }

        public static vec4 operator *(float a, vec4 v)
        {
            return new vec4(a * v.x, a * v.y, a * v.z, a * v.w);
        }

        public static vec4 operator *(vec4 v, float a)
        {
            return new vec4(a * v.x, a * v.y, a * v.z, a * v.w);
        }

        public static vec4 operator /(float a, vec4 v)
        {
            return new vec4(a / v.x, a / v.y, a / v.z, a / v.w);
        }

        public static vec4 operator /(vec4 v, float a)
        {
            return new vec4(v.x / a, v.y / a, v.z / a, v.w / a);
        }

        public static vec4 operator +(vec4 v, vec4 w)
        {
            return new vec4(v.x + w.x, v.y + w.y, v.z + w.z, v.w + w.w);
        }

        public static vec4 operator -(vec4 v, vec4 w)
        {
            return new vec4(v.x - w.x, v.y - w.y, v.z - w.z, v.w - w.w);
        }

        public static vec4 operator *(vec4 v, vec4 w)
        {
            return new vec4(v.x * w.x, v.y * w.y, v.z * w.z, v.w * w.w);
        }

        public static vec4 operator /(vec4 v, vec4 w)
        {
            return new vec4(v.x / w.x, v.y / w.y, v.z / w.z, v.w / w.w);
        }

        public static bool operator ==(vec4 v, vec4 w)
        {
            return
                v.x == w.x &&
                v.y == w.y &&
                v.z == w.z &&
                v.w == w.w;
        }

        public static bool operator !=(vec4 v, vec4 w)
        {
            return
                v.x != w.x ||
                v.y != w.y ||
                v.z != w.z ||
                v.w != w.w;
        }

        public override bool Equals(object o)
        {
            return o is vec4 ? this == (vec4)o : false;
        }

        public bool Equals(vec4 v)
        {
            return this == v;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode() ^ w.GetHashCode();
        }

        public static vec4 operator -(vec4 v)
        {
            return new vec4(-v.x, -v.y, -v.z, -v.w);
        }

        public static implicit operator Vector4(vec4 v)
        {
            return new Vector4(v.x, v.y, v.z, v.w);
        }

        public static implicit operator vec4(color v)
        {
            return new vec4(v.x, v.y, v.z, v.w);
        }

        public static explicit operator color(vec4 v)
        {
            return new color(v.x, v.y, v.z, v.w);
        }

        public static explicit operator vec4(Vector4 v)
        {
            return new vec4(v.X, v.Y, v.Z, v.W);
        }

        public static explicit operator Color(vec4 v)
        {
            return new Color(v.x, v.y, v.z, v.w);
        }        

        public static readonly vec4 Zero    = new vec4(0, 0, 0, 0);
        public static readonly vec4 Nothing = new vec4(0, 0, 0, 0);

        // Extra code gen goes here
    }

    [Hlsl("float4")]
    public partial struct color : Convertible</*KeepInCopy*/ vec4, color>
    {
        public color ConvertFrom(/*KeepInCopy*/ vec4 v)
        {
            return (color)v;
        }

        public /*KeepInCopy*/ vec4 ConvertTo()
        {
            return (/*KeepInCopy*/ vec4)this;
        }

        [Hlsl("float4")]
        public color(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public color(byte x, byte y, byte z, byte w)
        {
            this.x = ((float)x) / 256.0f;
            this.y = ((float)y) / 256.0f;
            this.z = ((float)z) / 256.0f;
            this.w = ((float)w) / 256.0f;
        }

        public color Premultiplied
        {
            get
            {
                return new color(r * a, g * a, b * a, a);
            }
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

        [Hlsl("yzw")]
        public vec3 yzw { get { return new vec3(y, z, w); } set { y = value.x; z = value.y; w = value.z; } }

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

        [Hlsl("gba")]
        public vec3 gba { get { return yzw; } set { yzw = value; } }

        [Hlsl("rg")]
        public vec2 rg { get { return xy; } set { xy = value; } }

        [Hlsl("ba")]
        public vec2 ba { get { return zw; } set { zw = value; } }


        public static color operator *(float a, color v)
        {
            return new color(a * v.x, a * v.y, a * v.z, a * v.w);
        }

        public static color operator *(color v, float a)
        {
            return new color(a * v.x, a * v.y, a * v.z, a * v.w);
        }

        public static color operator /(float a, color v)
        {
            return new color(a / v.x, a / v.y, a / v.z, a / v.w);
        }

        public static color operator /(color v, float a)
        {
            return new color(v.x / a, v.y / a, v.z / a, v.w / a);
        }

        public static color operator +(color v, color w)
        {
            return new color(v.x + w.x, v.y + w.y, v.z + w.z, v.w + w.w);
        }

        public static color operator -(color v, color w)
        {
            return new color(v.x - w.x, v.y - w.y, v.z - w.z, v.w - w.w);
        }

        public static color operator *(color v, color w)
        {
            return new color(v.x * w.x, v.y * w.y, v.z * w.z, v.w * w.w);
        }

        public static color operator /(color v, color w)
        {
            return new color(v.x / w.x, v.y / w.y, v.z / w.z, v.w / w.w);
        }

        public static bool operator ==(color v, color w)
        {
            return
                v.x == w.x &&
                v.y == w.y &&
                v.z == w.z &&
                v.w == w.w;
        }

        public static bool operator !=(color v, color w)
        {
            return
                v.x != w.x ||
                v.y != w.y ||
                v.z != w.z ||
                v.w != w.w;
        }

        public override bool Equals(object o)
        {
            return o is color ? this == (color)o : false;
        }

        public bool Equals(color v)
        {
            return this == v;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode() ^ w.GetHashCode();
        }

        public static implicit operator Vector4(color v)
        {
            return new Vector4(v.x, v.y, v.z, v.w);
        }

        public static explicit operator color(Vector4 v)
        {
            return new color(v.X, v.Y, v.Z, v.W);
        }

        public static explicit operator Color(color v)
        {
            return new Color(v.x, v.y, v.z, v.w);
        }        

        public static readonly color Zero    = new color(0f, 0f, 0f, 0f);
        public static readonly color Nothing = new color(0f, 0f, 0f, 0f);

        // -------------------------------------------------------------------

        public static readonly color TransparentBlack = new color(0f, 0f, 0f, 0f);

        public static explicit operator color(Color v)
        {
            return new color(v.R, v.G, v.B, v.A);
        }

        // Extra code gen goes here
    }
}