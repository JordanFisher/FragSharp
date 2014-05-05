using FragSharpFramework;

namespace Life
{
    public class State : GridComputation
    {
        public const float
            Dead = _0,
            Alive = _1;
    }

    [Copy(typeof(vec4))]
    public partial struct cell
    {
        [Hlsl("r")]
        public float state { get { return r; } set { r = value; } }
    }
}