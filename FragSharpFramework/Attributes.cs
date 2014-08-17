using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FragSharpFramework
{
    public enum TranslationType { Substitute, ReplaceExpression, UnderscoreAppend };

    public class Vals
    {
        [Vals(true, false)] public class BoolAttribute : Attribute { }
        public static readonly bool[] Bool = new bool[] { true, false };
    }

    public class ValsAttribute : Attribute
    {
        public ValsAttribute(params int[] vals) { }
        public ValsAttribute(params float[] vals) { }
        public ValsAttribute(params bool[] vals) { }
    }

    public enum CastStyle { ExplicitCasts, ImplicitCast, NoCasts };
    public class CopyAttribute : Attribute
    {
        public CopyAttribute(Type type) { }
        public CopyAttribute(Type type, CastStyle style) { }
    }

    public class KeepInCopy : Attribute { }

    public enum Special { rgba_hex, rgb_hex }
    public class SpecialAttribute : Attribute
    {
        public SpecialAttribute(Special name) { }
    }

    public class HlslAttribute : Attribute
    {
        public HlslAttribute() { }
        public HlslAttribute(string translation) { }
        public HlslAttribute(string translation, TranslationType translation_type) { }
    }

    public class POSITION0Attribute : Attribute { }
    public class COLOR0Attribute : Attribute { }
    public class TEXCOORD0Attribute : Attribute { }

    public class VertexShaderAttribute : Attribute { }
    public class FragmentShaderAttribute : Attribute { }
}