// This file was auto-generated by FragSharp. It will be regenerated on the next compilation.
// Manual changes made will not persist and may cause incorrect behavior between compilations.

using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using FragSharpFramework;

namespace FragSharpFramework
{
    public class FragSharp
    {
        public static ContentManager Content;
        public static GraphicsDevice GraphicsDevice;
        public static void Initialize(ContentManager Content, GraphicsDevice GraphicsDevice)
        {
            FragSharp.Content = Content;
            FragSharp.GraphicsDevice = GraphicsDevice;
        }
    }
}

