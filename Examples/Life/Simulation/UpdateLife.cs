using FragSharpFramework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Life
{
    public partial class UpdateLife : GridComputation
    {
        [FragmentShader]
        cell FragmentShader(VertexOut vertex, Field<cell> Current)
        {
            cell here = Current[Here];

            float neighbors = 
                Current[RightOne].state + Current[UpOne] .state + Current[LeftOne]  .state + Current[DownOne] .state +
                Current[UpRight] .state + Current[UpLeft].state + Current[DownRight].state + Current[DownLeft].state;

            if (neighbors < _2 || neighbors > _3)
                here.state = State.Dead;

            if (neighbors == _3)
                here.state = State.Alive;

            return here;
        }
    }

    public partial class UpdateLife
    {
        public static Effect CompiledEffect;

        public static void Initialize(ContentManager Content, GraphicsDevice GraphicsDevice)
        {
            CompiledEffect = Content.Load<Effect>("FragSharpShaders/UpdateLife");
        }

        public static void Apply(Texture2D Current, RenderTarget2D Output)
        {
            GridHelper.GraphicsDevice.SetRenderTarget(Output);
            GridHelper.GraphicsDevice.Clear(Color.Transparent);
            Using(Current);
            GridHelper.DrawGrid();
        }

        public static void Using(Texture2D Current)
        {
            CompiledEffect.Parameters["fs_param_Current_Texture"].SetValue(FragSharpMarshal.Marshal(Current));
            CompiledEffect.Parameters["fs_param_Current_size"].SetValue(FragSharpMarshal.Marshal(vec(Current.Width, Current.Height)));
            CompiledEffect.Parameters["fs_param_Current_dxdy"].SetValue(FragSharpMarshal.Marshal(1.0f / vec(Current.Width, Current.Height)));
            CompiledEffect.CurrentTechnique.Passes[0].Apply();
        }
    }
}
