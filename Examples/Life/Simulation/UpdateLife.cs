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
}
