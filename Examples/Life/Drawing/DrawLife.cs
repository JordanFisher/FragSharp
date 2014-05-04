using FragSharpFramework;

namespace Life
{
    public partial class DrawLife : BaseShader
    {
        [FragmentShader]
        color FragmentShader(VertexOut vertex, Field<cell> Current)
        {
            color output = color.TransparentBlack;

            cell here  = Current[Here];

            return here.state == State.Alive ? rgba(1,1,1,1) : rgba(0,0,0,1);
        }
    }
}
