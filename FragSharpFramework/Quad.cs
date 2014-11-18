using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FragSharpFramework
{
    public class RectangleQuad : FragSharpStd
    {
        public Texture2D Texture;

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

        public void SetupPosition(vec2 PositionBl, vec2 PositionTr)
        {
            const float Z = 0.0f;

            vec3 _PositionBl = new vec3(PositionBl.x, PositionBl.y, Z);
            vec3 _PositionTr = new vec3(PositionTr.x, PositionTr.y, Z);
            vec3 _PositionBr = new vec3(PositionTr.x, PositionBl.y, Z);
            vec3 _PositionTl = new vec3(PositionBl.x, PositionTr.y, Z);

            vertexData[TOP_LEFT].Position = _PositionTl;
            vertexData[TOP_RIGHT].Position = _PositionTr;
            vertexData[BOTTOM_RIGHT].Position = _PositionBr;
            vertexData[BOTTOM_LEFT].Position = _PositionBl;
        }

        vec2 Rotate(vec2 v, float Angle, vec2 Center)
        {
            v -= Center;

            float c = cos(Angle);
            float s = sin(Angle);

            return Center + vec(c * v.x + -s * v.y, s * v.x + c * v.y);
        }

        public void SetupVertices(vec2 PositionBl, vec2 PositionTr, vec2 UvBl, vec2 UvTr, float Angle = 0, vec2 Center = default(vec2))
        {
            const float Z = 0.0f;

            vec3 _PositionBl = new vec3(PositionBl.x, PositionBl.y, Z);
            vec3 _PositionTr = new vec3(PositionTr.x, PositionTr.y, Z);
            vec3 _PositionBr = new vec3(PositionTr.x, PositionBl.y, Z);
            vec3 _PositionTl = new vec3(PositionBl.x, PositionTr.y, Z);

            if (Angle != 0)
            {
                _PositionBl.xy = Rotate(_PositionBl.xy, Angle, Center);
                _PositionTr.xy = Rotate(_PositionTr.xy, Angle, Center);
                _PositionBr.xy = Rotate(_PositionBr.xy, Angle, Center);
                _PositionTl.xy = Rotate(_PositionTl.xy, Angle, Center);
            }

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

        public void SetupUv(vec2 UvBl, vec2 UvTr)
        {
            const float Z = 0.0f;

            vec2 _UvBl = new vec2(UvBl.x, UvTr.y);
            vec2 _UvTr = new vec2(UvTr.x, UvBl.y);
            vec2 _UvBr = new vec2(UvTr.x, UvTr.y);
            vec2 _UvTl = new vec2(UvBl.x, UvBl.y);

            vertexData[TOP_LEFT].TextureCoordinate = _UvTl;
            vertexData[TOP_RIGHT].TextureCoordinate =_UvTr;
            vertexData[BOTTOM_RIGHT].TextureCoordinate = _UvBr;
            vertexData[BOTTOM_LEFT].TextureCoordinate = _UvBl;
        }

        public void SetColor(color clr)
        {
            Color _clr = new Color(FragSharpMarshal.Marshal(clr));

            vertexData[TOP_LEFT].Color = _clr;
            vertexData[TOP_RIGHT].Color = _clr;
            vertexData[BOTTOM_RIGHT].Color = _clr;
            vertexData[BOTTOM_LEFT].Color = _clr;
        }

        public void Draw(GraphicsDevice GraphicsDevice)
        {
            GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertexData, 0, 4, indexData, 0, 2);
        }

        static RectangleQuad ScratchQuad = new RectangleQuad();
        public static void Draw(GraphicsDevice GraphicsDevice, vec2 pos, vec2 size, float Angle = 0)
        {
            ScratchQuad.SetupVertices(pos - size, pos + size, vec2.Zero, vec2.Ones, Angle, pos);
            ScratchQuad.Draw(GraphicsDevice);
        }

        public vec2 Bl { get { return new vec2(vertexData[BOTTOM_LEFT] .Position.X, vertexData[BOTTOM_LEFT] .Position.Y); } }
        public vec2 Tl { get { return new vec2(vertexData[TOP_LEFT]    .Position.X, vertexData[TOP_LEFT]    .Position.Y); } }
        public vec2 Br { get { return new vec2(vertexData[BOTTOM_RIGHT].Position.X, vertexData[BOTTOM_RIGHT].Position.Y); } }
        public vec2 Tr { get { return new vec2(vertexData[TOP_RIGHT]   .Position.X, vertexData[TOP_RIGHT]   .Position.Y); } }
        public vec2 pos { get { return (Tr + Bl) / 2; } }
        public vec2 size { get { return abs(Tr - Bl) / 2; } }
    }
}