﻿using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

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

    public class Quad
    {
        VertexPositionColorTexture[] vertexData;

        const int TOP_LEFT = 0;
        const int TOP_RIGHT = 1;
        const int BOTTOM_RIGHT = 2;
        const int BOTTOM_LEFT = 3;

        static int[] indexData = new int[] { 
            TOP_LEFT, BOTTOM_RIGHT, BOTTOM_LEFT,
            TOP_LEFT, TOP_RIGHT,    BOTTOM_RIGHT,
        };

        public Quad(vec2 PositionBl, vec2 PositionTr, vec2 UvBl, vec2 UvTr)
        {
            SetupVertices(PositionBl, PositionTr, UvBl, UvTr);
        }

        public void SetupVertices(vec2 PositionBl, vec2 PositionTr, vec2 UvBl, vec2 UvTr)
        {
            const float Z = 0.0f;

            vec3 _PositionBl = new vec3(PositionBl.x, PositionBl.y, Z);
            vec3 _PositionTr = new vec3(PositionTr.x, PositionTr.y, Z);
            vec3 _PositionBr = new vec3(PositionTr.x, PositionBl.y, Z);
            vec3 _PositionTl = new vec3(PositionBl.x, PositionTr.y, Z);

            vec2 _UvBl = new vec2(UvBl.x, UvTr.y);
            vec2 _UvTr = new vec2(UvTr.x, UvBl.y);
            vec2 _UvBr = new vec2(UvTr.x, UvTr.y);
            vec2 _UvTl = new vec2(UvBl.x, UvBl.y);

            vertexData = new VertexPositionColorTexture[4];
            vertexData[TOP_LEFT] = new VertexPositionColorTexture(_PositionTl, Color.White, _UvTl);
            vertexData[TOP_RIGHT] = new VertexPositionColorTexture(_PositionTr, Color.White, _UvTr);
            vertexData[BOTTOM_RIGHT] = new VertexPositionColorTexture(_PositionBr, Color.White, _UvBr);
            vertexData[BOTTOM_LEFT] = new VertexPositionColorTexture(_PositionBl, Color.White, _UvBl);
        }

        public void Draw(GraphicsDevice GraphicsDevice)
        {
            GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertexData, 0, 4, indexData, 0, 2);
        }
    }

    public class GridHelper
    {
        public static GraphicsDevice GraphicsDevice;

        static Quad UnitSquare = new Quad(new vec2(-1, -1), new vec2(1, 1), new vec2(0, 0), new vec2(1, 1));

        public static void Initialize(GraphicsDevice GraphicsDevice)
        {
            GridHelper.GraphicsDevice = GraphicsDevice;
        }

        public static void DrawGrid()
        {
            UnitSquare.Draw(GraphicsDevice);
        }
    }

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

    [Hlsl("sampler")]
    public class Sampler : FragSharpCode
    {
        [Hlsl("size", TranslationType.UnderscoreAppend)] public vec2 Size;
        [Hlsl("d",    TranslationType.UnderscoreAppend)] public vec2 DxDy;

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

    [Hlsl("float2")]
    public struct RelativeIndex
    {
        [Hlsl("x")] public float i;
        [Hlsl("y")] public float j;

        [Hlsl("float2")]
        public RelativeIndex(float i, float j) { this.i = i; this.j = j; }
    }

    public class CopyAttribute : Attribute
    {
        public CopyAttribute(Type type) { }
    }

    public enum TranslationType { Substitute, ReplaceExpression, UnderscoreAppend };

    public class HlslAttribute : Attribute
    {
        public HlslAttribute() { }
        public HlslAttribute(string translation) { }
        public HlslAttribute(string translation, TranslationType translation_type) { }
    }

    public class POSITION0Attribute : Attribute { }
    public class COLOR0Attribute : Attribute { }
    public class TEXCOORD0Attribute : Attribute { }

    public class PreambleAttribute : Attribute { }

    public class VertexShaderAttribute : Attribute { }
    public class FragmentShaderAttribute : Attribute { }

    /// <summary>
    /// This is the base shader class. All shader classes must inherit from this class or one of its descendants,
    /// in order to be eligible for compilation into shader code. In addition, any class wishing to be compiled must
    /// implement or inherit both a vertex shader and a fragment shader.
    /// </summary>
    public abstract class Shader : FragSharpCode { }

    public partial class GridComputation : Shader
    {
        [VertexShader]
        VertexOut GridVertexShader(Vertex data)
        {
            VertexOut Output = VertexOut.Zero;

            Output.Position.w = 1;
            Output.Position.xy = data.Position.xy;
            Output.TexCoords = data.TextureCoordinate;

            return Output;
        }
    }

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
    public class FragSharpCode
    {
        [Hlsl("float2")]
        protected static vec2 vec(float x, float y)                   { return new vec2(x, y);       }

        [Hlsl("float3")]
        protected static vec3 vec(float x, float y, float z)          { return new vec3(x, y, z);    }

        [Hlsl("float4")]
        protected static vec4 vec(float x, float y, float z, float w) { return new vec4(x, y, z, w); }

        [Hlsl("float4")]
        protected color rgba(float x, float y, float z, float w) { return new color(x, y, z, w); }

        protected const float _0 = 0 / 255f, _1 = 1 / 255f, _2 = 2 / 255f, _3 = 3 / 255f, _4 = 4 / 255f, _5 = 5 / 255f, _6 = 6 / 255f, _7 = 7 / 255f, _8 = 8 / 255f, _9 = 9 / 255f, _10 = 10 / 255f, _11 = 11 / 255f, _12 = 12 / 255f;

        protected static readonly RelativeIndex
            RightOne = new RelativeIndex(1, 0),
            LeftOne = new RelativeIndex(-1, 0),
            UpOne = new RelativeIndex(0, 1),
            DownOne = new RelativeIndex(0, -1),
            Here = new RelativeIndex(0, 0);

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