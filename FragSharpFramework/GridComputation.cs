using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FragSharpFramework
{
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
}