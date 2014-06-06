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

    public interface Convertible<TargetType, BaseType>
    {
        BaseType ConvertFrom(TargetType v);
        TargetType ConvertTo();
    }

    public class AddressType { }
    public class Wrap : AddressType { }
    public class Clamp : AddressType { }

    public class SampleType { }
    public class Point  : SampleType { }
    public class Linear : SampleType { }

    public class Field<Type> : Sampler<Type, Clamp, Point>
        where Type : Convertible<vec4, Type>
            { public Field(Texture2D Texture) : base(Texture) { } }

    public class PeriodicField<Type> : Sampler<Type, Wrap, Point>
        where Type : Convertible<vec4, Type>
            { public PeriodicField(Texture2D Texture) : base(Texture) { } }

    public class PointSampler : TextureSampler<Wrap, Point>
            { public PointSampler(Texture2D Texture) : base(Texture) { } }
    public class PointSampler<Address> : TextureSampler<Address, Point>
        where Address : AddressType
            { public PointSampler(Texture2D Texture) : base(Texture) { } }
    public class PointSampler<AddressU, AddressV> : TextureSampler<AddressU, AddressV, Point, Point, Point>
        where AddressU : AddressType
        where AddressV : AddressType
            { public PointSampler(Texture2D Texture) : base(Texture) { } }

    public class TextureSampler : TextureSampler<Wrap, Linear>
            { public TextureSampler(Texture2D Texture) : base(Texture) { } }
    public class TextureSampler<Address, Filter> : TextureSampler<Address, Address, Filter, Filter, Filter>
        where Address : AddressType
        where Filter : SampleType    
            { public TextureSampler(Texture2D Texture) : base(Texture) { } }
    public class TextureSampler<AddressU, AddressV, MinFilter, MagFilter, MipFilter> : Sampler<color, AddressU, AddressV, MinFilter, MagFilter, MipFilter>
        where AddressU : AddressType
        where AddressV : AddressType
        where MinFilter : SampleType
        where MagFilter : SampleType
        where MipFilter : SampleType
            { public TextureSampler(Texture2D Texture) : base(Texture) { } }

    public class Sampler<Type, Address, Filter> : Sampler<Type, Address, Address, Filter, Filter, Filter>
        where Type : Convertible<vec4, Type>
        where Address : AddressType
        where Filter : SampleType
            { public Sampler(Texture2D Texture) : base(Texture) { } }

    public class SamplerBaseAttribute : Attribute { }
    [SamplerBase]
    public class Sampler<Type, AddressU, AddressV, MinFilter, MagFilter, MipFilter> : Sampler
        where Type : Convertible<vec4, Type>
        where AddressU : AddressType
        where AddressV : AddressType
        where MinFilter : SampleType
        where MagFilter : SampleType
        where MipFilter : SampleType
    {
        public Sampler(Texture2D Texture)
            : base(Texture)
        {
        }

        new public Type this[RelativeIndex index]
        {
            get
            {
                Type t = default(Type);
                return t.ConvertFrom(base[index]);
            }
        }
    }

    [Hlsl("sampler")]
    public abstract class Sampler : FragSharpStd
    {
        public Sampler() { }

        public Sampler(Texture2D Texture)
        {
            SetTexture(Texture);   
        }

        public void SetTexture(Texture2D Texture)
        {
            this.Texture = Texture;

            Width = Texture.Width;
            Height = Texture.Height;
            Size = vec(Width, Height);
            DxDy = 1 / Size;
        }

        public const string SizeSuffix = "size", DxDySuffix = "dxdy";
        [Hlsl(SizeSuffix, TranslationType.UnderscoreAppend)] public vec2 Size;
        [Hlsl(DxDySuffix, TranslationType.UnderscoreAppend)] public vec2 DxDy;

        public int Width, Height;
        public Texture2D Texture;

        public Color[] clr;

        public void GetDataFromTexture()
        {
            if (clr == null)
            {
                clr = new Color[Width * Height];
            }

            Texture.GetData<Color>(clr);
        }

        public void CopyDataToTexture()
        {
            Texture.SetData(clr);
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
                return texture_lookup(this, __SamplerHelper.TextureCoord + DxDy * (vec2)index);
            }
        }
    }

    public static class __SamplerHelper
    {
        public static bool SoftwareEmulation = false;
        public static vec2 TextureCoord = vec2.Zero;
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

        [Special(Special.rgba_hex)]
        protected static color rgba(int hex, float a)
        {
            return rgba(0, 0, 0, a);
        }

        [Special(Special.rgb_hex)]
        protected static color rgb(int hex)
        {
            return rgba(0, 0, 0, 1);
        }

        public const float _0 = 0 / 255f, _1 = 1 / 255f, _2 = 2 / 255f, _3 = 3 / 255f, _4 = 4 / 255f, _5 = 5 / 255f, _6 = 6 / 255f, _7 = 7 / 255f, _8 = 8 / 255f, _9 = 9 / 255f, _10 = 10 / 255f, _11 = 11 / 255f, _12 = 12 / 255f, _13 = 13 / 255f, _14 = 14 / 255f, _15 = 15 / 255f, _16 = 16 / 255f, _17 = 17 / 255f, _18 = 18 / 255f, _19 = 19 / 255f, _20 = 20 / 255f, _21 = 21 / 255f, _22 = 22 / 255f, _23 = 23 / 255f, _24 = 24 / 255f, _25 = 25 / 255f, _26 = 26 / 255f, _27 = 27 / 255f, _28 = 28 / 255f, _29 = 29 / 255f, _30 = 30 / 255f, _31 = 31 / 255f, _32 = 32 / 255f, _33 = 33 / 255f, _34 = 34 / 255f, _35 = 35 / 255f, _36 = 36 / 255f, _37 = 37 / 255f, _38 = 38 / 255f, _39 = 39 / 255f, _40 = 40 / 255f, _41 = 41 / 255f, _42 = 42 / 255f, _43 = 43 / 255f, _44 = 44 / 255f, _45 = 45 / 255f, _46 = 46 / 255f, _47 = 47 / 255f, _48 = 48 / 255f, _49 = 49 / 255f, _50 = 50 / 255f, _51 = 51 / 255f, _52 = 52 / 255f, _53 = 53 / 255f, _54 = 54 / 255f, _55 = 55 / 255f, _56 = 56 / 255f, _57 = 57 / 255f, _58 = 58 / 255f, _59 = 59 / 255f, _60 = 60 / 255f, _61 = 61 / 255f, _62 = 62 / 255f, _63 = 63 / 255f, _64 = 64 / 255f, _65 = 65 / 255f, _66 = 66 / 255f, _67 = 67 / 255f, _68 = 68 / 255f, _69 = 69 / 255f, _70 = 70 / 255f, _71 = 71 / 255f, _72 = 72 / 255f, _73 = 73 / 255f, _74 = 74 / 255f, _75 = 75 / 255f, _76 = 76 / 255f, _77 = 77 / 255f, _78 = 78 / 255f, _79 = 79 / 255f, _80 = 80 / 255f, _81 = 81 / 255f, _82 = 82 / 255f, _83 = 83 / 255f, _84 = 84 / 255f, _85 = 85 / 255f, _86 = 86 / 255f, _87 = 87 / 255f, _88 = 88 / 255f, _89 = 89 / 255f, _90 = 90 / 255f, _91 = 91 / 255f, _92 = 92 / 255f, _93 = 93 / 255f, _94 = 94 / 255f, _95 = 95 / 255f, _96 = 96 / 255f, _97 = 97 / 255f, _98 = 98 / 255f, _99 = 99 / 255f, _100 = 100 / 255f, _101 = 101 / 255f, _102 = 102 / 255f, _103 = 103 / 255f, _104 = 104 / 255f, _105 = 105 / 255f, _106 = 106 / 255f, _107 = 107 / 255f, _108 = 108 / 255f, _109 = 109 / 255f, _110 = 110 / 255f, _111 = 111 / 255f, _112 = 112 / 255f, _113 = 113 / 255f, _114 = 114 / 255f, _115 = 115 / 255f, _116 = 116 / 255f, _117 = 117 / 255f, _118 = 118 / 255f, _119 = 119 / 255f, _120 = 120 / 255f, _121 = 121 / 255f, _122 = 122 / 255f, _123 = 123 / 255f, _124 = 124 / 255f, _125 = 125 / 255f, _126 = 126 / 255f, _127 = 127 / 255f, _128 = 128 / 255f, _129 = 129 / 255f, _130 = 130 / 255f, _131 = 131 / 255f, _132 = 132 / 255f, _133 = 133 / 255f, _134 = 134 / 255f, _135 = 135 / 255f, _136 = 136 / 255f, _137 = 137 / 255f, _138 = 138 / 255f, _139 = 139 / 255f, _140 = 140 / 255f, _141 = 141 / 255f, _142 = 142 / 255f, _143 = 143 / 255f, _144 = 144 / 255f, _145 = 145 / 255f, _146 = 146 / 255f, _147 = 147 / 255f, _148 = 148 / 255f, _149 = 149 / 255f, _150 = 150 / 255f, _151 = 151 / 255f, _152 = 152 / 255f, _153 = 153 / 255f, _154 = 154 / 255f, _155 = 155 / 255f, _156 = 156 / 255f, _157 = 157 / 255f, _158 = 158 / 255f, _159 = 159 / 255f, _160 = 160 / 255f, _161 = 161 / 255f, _162 = 162 / 255f, _163 = 163 / 255f, _164 = 164 / 255f, _165 = 165 / 255f, _166 = 166 / 255f, _167 = 167 / 255f, _168 = 168 / 255f, _169 = 169 / 255f, _170 = 170 / 255f, _171 = 171 / 255f, _172 = 172 / 255f, _173 = 173 / 255f, _174 = 174 / 255f, _175 = 175 / 255f, _176 = 176 / 255f, _177 = 177 / 255f, _178 = 178 / 255f, _179 = 179 / 255f, _180 = 180 / 255f, _181 = 181 / 255f, _182 = 182 / 255f, _183 = 183 / 255f, _184 = 184 / 255f, _185 = 185 / 255f, _186 = 186 / 255f, _187 = 187 / 255f, _188 = 188 / 255f, _189 = 189 / 255f, _190 = 190 / 255f, _191 = 191 / 255f, _192 = 192 / 255f, _193 = 193 / 255f, _194 = 194 / 255f, _195 = 195 / 255f, _196 = 196 / 255f, _197 = 197 / 255f, _198 = 198 / 255f, _199 = 199 / 255f, _200 = 200 / 255f, _201 = 201 / 255f, _202 = 202 / 255f, _203 = 203 / 255f, _204 = 204 / 255f, _205 = 205 / 255f, _206 = 206 / 255f, _207 = 207 / 255f, _208 = 208 / 255f, _209 = 209 / 255f, _210 = 210 / 255f, _211 = 211 / 255f, _212 = 212 / 255f, _213 = 213 / 255f, _214 = 214 / 255f, _215 = 215 / 255f, _216 = 216 / 255f, _217 = 217 / 255f, _218 = 218 / 255f, _219 = 219 / 255f, _220 = 220 / 255f, _221 = 221 / 255f, _222 = 222 / 255f, _223 = 223 / 255f, _224 = 224 / 255f, _225 = 225 / 255f, _226 = 226 / 255f, _227 = 227 / 255f, _228 = 228 / 255f, _229 = 229 / 255f, _230 = 230 / 255f, _231 = 231 / 255f, _232 = 232 / 255f, _233 = 233 / 255f, _234 = 234 / 255f, _235 = 235 / 255f, _236 = 236 / 255f, _237 = 237 / 255f, _238 = 238 / 255f, _239 = 239 / 255f, _240 = 240 / 255f, _241 = 241 / 255f, _242 = 242 / 255f, _243 = 243 / 255f, _244 = 244 / 255f, _245 = 245 / 255f, _246 = 246 / 255f, _247 = 247 / 255f, _248 = 248 / 255f, _249 = 249 / 255f, _250 = 250 / 255f, _251 = 251 / 255f, _252 = 252 / 255f, _253 = 253 / 255f, _254 = 254 / 255f, _255 = 255 / 255f;
        public static readonly float[] _ = new float[] { _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15, _16, _17, _18, _19, _20, _21, _22, _23, _24, _25, _26, _27, _28, _29, _30, _31, _32, _33, _34, _35, _36, _37, _38, _39, _40, _41, _42, _43, _44, _45, _46, _47, _48, _49, _50, _51, _52, _53, _54, _55, _56, _57, _58, _59, _60, _61, _62, _63, _64, _65, _66, _67, _68, _69, _70, _71, _72, _73, _74, _75, _76, _77, _78, _79, _80, _81, _82, _83, _84, _85, _86, _87, _88, _89, _90, _91, _92, _93, _94, _95, _96, _97, _98, _99, _100, _101, _102, _103, _104, _105, _106, _107, _108, _109, _110, _111, _112, _113, _114, _115, _116, _117, _118, _119, _120, _121, _122, _123, _124, _125, _126, _127, _128, _129, _130, _131, _132, _133, _134, _135, _136, _137, _138, _139, _140, _141, _142, _143, _144, _145, _146, _147, _148, _149, _150, _151, _152, _153, _154, _155, _156, _157, _158, _159, _160, _161, _162, _163, _164, _165, _166, _167, _168, _169, _170, _171, _172, _173, _174, _175, _176, _177, _178, _179, _180, _181, _182, _183, _184, _185, _186, _187, _188, _189, _190, _191, _192, _193, _194, _195, _196, _197, _198, _199, _200, _201, _202, _203, _204, _205, _206, _207, _208, _209, _210, _211, _212, _213, _214, _215, _216, _217, _218, _219, _220, _221, _222, _223, _224, _225, _226, _227, _228, _229, _230, _231, _232, _233, _234, _235, _236, _237, _238, _239, _240, _241, _242, _243, _244, _245, _246, _247, _248, _249, _250, _251, _252, _253, _254, _255 };

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
            coordinates *= s.Size;

            int i = (int)coordinates.x;
            int j = (int)coordinates.y;

            if (i < 0) i = 0;
            if (j < 0) j = 0;
            if (i >= s.Width)  i = s.Width  - 1;
            if (j >= s.Height) j = s.Height - 1;

            Color C = s.clr[i * s.Height + j];
            color c = Convert(C);
            return c;

            //return (color)s.clr[i * s.Height + j];
        }

        static color Convert(Color c)
        {
            return rgba(Convert(c.R), Convert(c.G), Convert(c.B), Convert(c.A));
        }

        static float Convert(byte b)
        {
            return _[b];
        }

        /// <summary>
        /// Converts a "float int" value such as _0, _1, ... to its corresponding integer value, such as 0, 1, ...
        /// </summary>
        /// <param name="v">The "float int" value to convert.</param>
        /// <returns></returns>
        public static int Int(float v)
        {
            return (int)floor(255 * v + .5f);
        }

        [Hlsl("floor")]
        protected static float floor(float value)
        {
            return (float)Math.Floor(value);
        }

        [Hlsl("floor")] protected static vec2 floor(vec2 v) { return vec(floor(v.x), floor(v.y)); }
        [Hlsl("floor")] protected static vec3 floor(vec3 v) { return vec(floor(v.x), floor(v.y), floor(v.z)); }
        [Hlsl("floor")] protected static vec4 floor(vec4 v) { return vec(floor(v.x), floor(v.y), floor(v.z), floor(v.w)); }


        [Hlsl("length")]
        protected static float length(vec2 v)
        {
            return (float)Math.Sqrt(v.x * v.x + v.y * v.y);
        }

        [Hlsl("length")]
        protected static float length(vec3 v)
        {
            return (float)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        }

        [Hlsl("length")]
        protected static float length(vec4 v)
        {
            return (float)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z + v.w * v.w);
        }


        [Hlsl("cos")]
        protected static float cos(float angle)
        {
            return (float)Math.Cos((float)angle);
        }

        [Hlsl("cos")] protected static vec2 cos(vec2 v) { return vec(cos(v.x), cos(v.y)); }
        [Hlsl("cos")] protected static vec3 cos(vec3 v) { return vec(cos(v.x), cos(v.y), cos(v.z)); }
        [Hlsl("cos")] protected static vec4 cos(vec4 v) { return vec(cos(v.x), cos(v.y), cos(v.z), cos(v.w)); }


        [Hlsl("sin")]
        protected static float sin(float angle)
        {
            return (float)Math.Sin((float)angle);
        }

        [Hlsl("sin")] protected static vec2 sin(vec2 v) { return vec(sin(v.x), sin(v.y)); }
        [Hlsl("sin")] protected static vec3 sin(vec3 v) { return vec(sin(v.x), sin(v.y), sin(v.z)); }
        [Hlsl("sin")] protected static vec4 sin(vec4 v) { return vec(sin(v.x), sin(v.y), sin(v.z), sin(v.w)); }


        [Hlsl("abs")]
        protected static float abs(float angle)
        {
            return (float)Math.Abs((float)angle);
        }

        [Hlsl("abs")] protected static vec2 abs(vec2 v) { return vec(abs(v.x), abs(v.y)); }
        [Hlsl("abs")] protected static vec3 abs(vec3 v) { return vec(abs(v.x), abs(v.y), abs(v.z)); }
        [Hlsl("abs")] protected static vec4 abs(vec4 v) { return vec(abs(v.x), abs(v.y), abs(v.z), abs(v.w)); }


        [Hlsl("atan2")]
        protected static float atan(float y, float x)
        {
            return (float)Math.Atan2(y, x);
        }

        [Hlsl("atan2")] protected static vec2 atan2(vec2 y, vec2 x) { return vec(atan(y.x, x.x), atan(y.y, x.y)); }
        [Hlsl("atan2")] protected static vec3 atan2(vec3 y, vec3 x) { return vec(atan(y.x, x.x), atan(y.y, x.y), atan(y.z, x.z)); }
        [Hlsl("atan2")] protected static vec4 atan2(vec4 y, vec4 x) { return vec(atan(y.x, x.x), atan(y.y, x.y), atan(y.z, x.z), atan(y.w, x.w)); }


        [Hlsl("max")]
        protected static float max(float a, float b)
        {
            return (float)Math.Max(a, b);
        }

        [Hlsl("max")] protected static vec2 max(vec2 a, vec2 b) { return vec(max(a.x, b.x), max(a.y, b.y)); }
        [Hlsl("max")] protected static vec3 max(vec3 a, vec3 b) { return vec(max(a.x, b.x), max(a.y, b.y), max(a.z, b.z)); }
        [Hlsl("max")] protected static vec4 max(vec4 a, vec4 b) { return vec(max(a.x, b.x), max(a.y, b.y), max(a.z, b.z), max(a.w, b.w)); }

        protected static float max(float a, float b, float c) { return max(max(a, b), c); }
        protected static vec2  max(vec2  a, vec2  b, vec2  c) { return max(max(a, b), c); }
        protected static vec3  max(vec3  a, vec3  b, vec3  c) { return max(max(a, b), c); }
        protected static vec4  max(vec4  a, vec4  b, vec4  c) { return max(max(a, b), c); }

        protected static float max(float a, float b, float c, float d) { return max(max(a,b), max(c,d)); }
        protected static vec2  max(vec2  a, vec2  b, vec2  c, vec2  d) { return max(max(a,b), max(c,d)); }
        protected static vec3  max(vec3  a, vec3  b, vec3  c, vec3  d) { return max(max(a,b), max(c,d)); }
        protected static vec4  max(vec4  a, vec4  b, vec4  c, vec4  d) { return max(max(a,b), max(c,d)); }


        [Hlsl("min")]
        protected static float min(float a, float b)
        {
            return (float)Math.Min(a, b);
        }

        [Hlsl("min")] protected static vec2 min(vec2 a, vec2 b) { return vec(min(a.x, b.x), min(a.y, b.y)); }
        [Hlsl("min")] protected static vec3 min(vec3 a, vec3 b) { return vec(min(a.x, b.x), min(a.y, b.y), min(a.z, b.z)); }
        [Hlsl("min")] protected static vec4 min(vec4 a, vec4 b) { return vec(min(a.x, b.x), min(a.y, b.y), min(a.z, b.z), min(a.w, b.w)); }

        protected static float min(float a, float b, float c) { return min(min(a, b), c); }
        protected static vec2  min(vec2  a, vec2  b, vec2  c) { return min(min(a, b), c); }
        protected static vec3  min(vec3  a, vec3  b, vec3  c) { return min(min(a, b), c); }
        protected static vec4  min(vec4  a, vec4  b, vec4  c) { return min(min(a, b), c); }

        protected static float min(float a, float b, float c, float d) { return min(min(a,b), min(c,d)); }
        protected static vec2  min(vec2  a, vec2  b, vec2  c, vec2  d) { return min(min(a,b), min(c,d)); }
        protected static vec3  min(vec3  a, vec3  b, vec3  c, vec3  d) { return min(min(a,b), min(c,d)); }
        protected static vec4  min(vec4  a, vec4  b, vec4  c, vec4  d) { return min(min(a,b), min(c,d)); }
    }
}