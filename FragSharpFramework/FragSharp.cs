using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using FragSharpFramework;

namespace FragSharpFramework
{
    abstract class __TypeMaps
    {
        [Hlsl("float")] void 
            _( float _) {}

        [Hlsl("int")] void 
            _( int _) {}

        [Hlsl("bool")] void 
            _( bool _) {}
    }

    [Hlsl("sampler")]
    public class Sampler : FragSharpStd
    {
        public const string SizeSuffix = "size", DxDySuffix = "dxdy";
        [Hlsl(SizeSuffix, TranslationType.UnderscoreAppend)] public vec2 Size;
        [Hlsl(DxDySuffix, TranslationType.UnderscoreAppend)] public vec2 DxDy;

        public int Width, Height;
        public RenderTarget2D RenderTarget;

        public Color[] clr;

        void GetDataFromRenderTarget()
        {
            RenderTarget.GetData<Color>(clr);
        }

        void CopyDataToRenderTarget()
        {
            RenderTarget.SetData(clr);
        }

        public color this[vec2 uv]
        {
            get
            {
                return texture_lookup(this, uv);
            }
        }

        public color this[RelativeIndex index]
        {
            get
            {
                return texture_lookup(this, vec2.Zero);
            }
        }
    }

    /// <summary>
    /// This is the base shader class. All shader classes must inherit from this class or one of its descendants,
    /// in order to be eligible for compilation into shader code. In addition, any class wishing to be compiled must
    /// implement or inherit both a vertex shader and a fragment shader.
    /// </summary>
    public abstract class Shader : FragSharpStd { }

    [Hlsl("VertexToPixel")]
    public struct VertexOut
    {
        [POSITION0, Hlsl]
        public vec4 Position;
        [COLOR0, Hlsl]
        public color Color;
        [TEXCOORD0, Hlsl]
        public vec2 TexCoords;

        VertexOut(vec4 Position, color Color, vec2 TexCoords)
        {
            this.Position = Position;
            this.Color = Color;
            this.TexCoords = TexCoords;
        }

        [Hlsl("(VertexToPixel)0", TranslationType.ReplaceExpression)]
        public static readonly VertexOut Zero = new VertexOut(vec4.Zero, color.TransparentBlack, vec2.Zero);
    }

    public struct PixelToFrame
    {
        [COLOR0]
        public vec4 Color;
    }

    public struct Vertex
    {
        [POSITION0, Hlsl("inPos", TranslationType.ReplaceExpression)]
        public vec3 Position;

        [COLOR0, Hlsl("inColor", TranslationType.ReplaceExpression)]
        public color Color;

        [TEXCOORD0, Hlsl("inTexCoords", TranslationType.ReplaceExpression)]
        public vec2 TextureCoordinate;

        Vertex(vec3 Position, color Color, vec2 TextureCoordinate)
        {
            this.Position = Position;
            this.Color = Color;
            this.TextureCoordinate = TextureCoordinate;
        }
    }

    /// <summary>
    /// This class has a collection of static methods and fields that 'implement' the C# DSL.
    /// Any C# that wants to be FragSharp code should be inside a class that inherits from this class to have access to the DSL language features.
    /// </summary>
    public class FragSharpStd
    {
        [Hlsl("float2")]
        protected static vec2 vec(float x, float y)                   { return new vec2(x, y);       }

        [Hlsl("float3")]
        protected static vec3 vec(float x, float y, float z)          { return new vec3(x, y, z);    }

        [Hlsl("float4")]
        protected static vec4 vec(float x, float y, float z, float w) { return new vec4(x, y, z, w); }

        [Hlsl("float4")]
        protected static color rgba(float x, float y, float z, float w) { return new color(x, y, z, w); }

        protected const float _0 = 0 / 255f, _1 = 1 / 255f, _2 = 2 / 255f, _3 = 3 / 255f, _4 = 4 / 255f, _5 = 5 / 255f, _6 = 6 / 255f, _7 = 7 / 255f, _8 = 8 / 255f, _9 = 9 / 255f, _10 = 10 / 255f, _11 = 11 / 255f, _12 = 12 / 255f;

        protected static readonly RelativeIndex
            RightOne  = new RelativeIndex( 1, 0),
            LeftOne   = new RelativeIndex(-1, 0),
            UpOne     = new RelativeIndex( 0, 1),
            DownOne   = new RelativeIndex( 0,-1),
            UpRight   = new RelativeIndex( 1, 1),
            UpLeft    = new RelativeIndex(-1, 1),
            DownRight = new RelativeIndex( 1,-1),
            DownLeft  = new RelativeIndex(-1,-1),
            Here      = new RelativeIndex( 0, 0);

        [Hlsl("tex2D")]
        protected static color texture_lookup(Sampler s, vec2 coordinates)
        {
            int i = (int)coordinates.x;
            int j = (int)coordinates.y;

            return (color)s.clr[i * s.Height + j];
        }

        [Hlsl("floor")]
        protected static float floor(float value)
        {
            return (float)Math.Floor(value);
        }

        [Hlsl("length")]
        protected static float length(vec2 v)
        {
            return (float)Math.Sqrt(v.x * v.x + v.y * v.y);
        }

        [Hlsl("cos")]
        protected static float cos(float angle)
        {
            return (float)Math.Cos((float)angle);
        }

        [Hlsl("sin")]
        protected static float sin(float angle)
        {
            return (float)Math.Sin((float)angle);
        }

        [Hlsl("abs")]
        protected static float abs(float angle)
        {
            return (float)Math.Abs((float)angle);
        }
    }
}