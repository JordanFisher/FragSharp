using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FragSharpFramework
{
    public enum TranslationType { Substitute, ReplaceExpression, UnderscoreAppend };
    public class CopyAttribute : Attribute
    {
        public CopyAttribute(Type type) { }
    }

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