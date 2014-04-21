using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FragSharpFramework
{
    public class RectangleQuad
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

        public RectangleQuad()
        {
            SetupVertices(-vec2.Ones, vec2.Ones, vec2.Zero, vec2.Ones);
        }

        public RectangleQuad(vec2 PositionBl, vec2 PositionTr, vec2 UvBl, vec2 UvTr)
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

        static RectangleQuad ScratchQuad = new RectangleQuad();
        public static void Draw(GraphicsDevice GraphicsDevice, vec2 pos, vec2 size)
        {
            ScratchQuad.SetupVertices(pos - size, pos + size, vec2.Zero, vec2.Ones);
            ScratchQuad.Draw(GraphicsDevice);
        }
    }
}