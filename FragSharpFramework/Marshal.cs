using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FragSharpFramework
{
    public static class FragSharpMarshal
    {
        public static float Marshal(float val)
        {
            return val;
        }

        public static Vector2 Marshal(vec2 v)
        {
            return (Vector2)v;
        }

        public static Vector3 Marshal(vec3 v)
        {
            return (Vector3)v;
        }

        public static Vector4 Marshal(vec4 v)
        {
            return (Vector4)v;
        }

        public static Texture Marshal(Texture2D field)
        {
            return field;
        }
    }
}