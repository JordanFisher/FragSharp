using FragSharpFramework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Life
{
    public partial class UpdateLife : GridComputation
    {
        public static void _Apply(Texture2D Current, RenderTarget2D Output)
        {
            GridHelper.GraphicsDevice.SetRenderTarget(null);
            for (int i = 0; i < 10; i++)
                GridHelper.GraphicsDevice.Textures[i] = null;

            vec2 OutputSize = new vec2(Output.Width, Output.Height);
            
            var instance = new UpdateLife();
            var _Current = new Field<cell>(Current); _Current.GetDataFromTexture();
            var _Output  = new Field<cell>(Output);  _Output.GetDataFromTexture();

            for (int i = 0; i < Output.Width ; i++) {
            for (int j = 0; j < Output.Height; j++) {
                VertexOut v = VertexOut.Zero;
                v.TexCoords = new vec2(i, j) / OutputSize;
                __SamplerHelper.TextureCoord = v.TexCoords;
                var color = (color)instance.FragmentShader(v, _Current).ConvertTo();
                //color = rgba(State.Alive, 0,0,0);
                _Output.clr[i * _Output.Height + j] = new Color(FragSharpMarshal.Marshal(color));
            }}

            _Output.CopyDataToTexture();
        }
   }
}
