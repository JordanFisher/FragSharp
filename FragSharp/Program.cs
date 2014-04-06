using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace FragSharp
{
    internal class Program
    {
        private static void Main()
        {
            // Get and compile the code
            SemanticModel model;
            Compilation compilation;

            var Tree = SyntaxTree.ParseText(Code);
            var Root = Tree.GetRoot();

            compilation = Compilation.Create("MyCompilation",
                                             syntaxTrees: new List<SyntaxTree>() { Tree },
                                             references: new List<MetadataReference>() { MetadataReference.CreateAssemblyReference(typeof(object).Assembly.FullName) });
            model = compilation.GetSemanticModel(Tree);

            // Find all methods
            var Methods = GetMethods(Root);
            Methods.ForEach(method => Console.WriteLine(method.Identifier.Value));

            // Find all FragmentShader methods
            var GridMethods = Methods.Where(method => HasAttribute(method, "FragmentShader")).ToList();

            // Compile a FragementShader
            var writer = new HlslFragmentWriter(model, compilation);
            string output = writer.CompileFragmentMethod(GridMethods[1]);

            Console.WriteLine("\nDone!");
        }

        static void PrintTree(SyntaxNodeOrToken node, string indent = "")
        {
            using (var writer = new StringWriter())
            {
                WriteTree(node, writer, indent);
                Console.Write(writer);
            }
        }

        static void WriteTree(SyntaxNodeOrToken node, StringWriter output, string indent = "")
        {
            var nodes = node.ChildNodesAndTokens();
            foreach (var child in nodes)
            {
                string kind = string.Empty;
                string value = string.Empty;

                if (child.IsNode)
                {
                    kind = child.AsNode().Kind.ToString();
                }
                else
                {
                    kind = child.AsToken().Kind.ToString();
                    value = child.AsToken().ValueText;
                }

                output.WriteLine("{0}{1}  {2}", indent, kind, value);

                WriteTree(child, output, indent + "--");
            }
        }

        static bool HasAttribute(MethodDeclarationSyntax method, string AttributeName)
        {
            return method.AttributeLists.Any(
              list => list.Attributes.Any(
                attribute => attribute.Name.ToString() == AttributeName));
        }

        static bool IsGridComputtion(MethodDeclarationSyntax method)
        {
            return true;
        }

        static List<MethodDeclarationSyntax> GetMethods(SyntaxNodeOrToken node, List<MethodDeclarationSyntax> methods = null)
        {
            if (methods == null)
            {
                methods = new List<MethodDeclarationSyntax>();
            }

            var nodes = node.ChildNodesAndTokens();
            foreach (var child in nodes)
            {
                var method = child.AsNode() as MethodDeclarationSyntax;
                if (null != method)
                {
                    methods.Add(method);
                }

                GetMethods(child, methods);
            }

            return methods;
        }

        static string Code = @"
namespace GpuSim
{
    public class vec2
    {
        public vec2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public float x, y;

        public static readonly vec2 Zero = new vec2(0, 0);
    }

    public class vec3
    {
        public vec3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public float x, y, z;

        public vec2 xy { get { return new vec2(x, y); } set { x = value.x; y = value.y; } }

        public static readonly vec3 Zero = new vec3(0, 0, 0);
    }

    public class vec4
    {
        public vec4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public float x, y, z, w;

        public vec2 xy { get { return new vec2(x, y); } set { x = value.x; y = value.y; } }
        public vec3 xyz { get { return new vec3(x, y, z); } set { x = value.x; y = value.y; z = value.z; } }

        public static readonly vec4 Zero = new vec4(0, 0, 0, 0);
    }

    public class unit : color
    {
        public unit() : base(0, 0, 0, 0) { }

        public float direction { get { return r; } set { r = value; } }
        public float change { get; set; }

        public static readonly unit Nothing = new unit(0, 0, 0, 0);
    }

    public class color
    {
        public color(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        byte r_byte, g_byte, b_byte, a_byte;

        public float r { get { return r_byte / 255f; } set { r_byte = (byte)(value * 255f); } }
        public float g { get { return g_byte / 255f; } set { g_byte = (byte)(value * 255f); } }
        public float b { get { return b_byte / 255f; } set { b_byte = (byte)(value * 255f); } }
        public float a { get { return a_byte / 255f; } set { a_byte = (byte)(value * 255f); } }

        public float x { get { return r; } set { r = value; } }
        public float y { get { return g; } set { g = value; } }
        public float z { get { return b; } set { b = value; } }
        public float w { get { return a; } set { a = value; } }

        public static readonly color TransparentBlack = new color(0,0,0,0);
    }

	public static class RndExtension
	{
		public static float RndBit(this System.Random rnd)
		{
			return rnd.NextDouble() > .5 ? 1 : 0;
		}
	}

    
    public class Variable
    {
        protected EffectParameter MyParameter;
    }

    public class UnitField : Variable
    {
        public int Width, Height;
        public RenderTarget2D RenderTarget;

        public unit this[RelativeIndex index]
        {
            get
            {
                return unit.Nothing;
            }
            set
            {

            }
        }

        public unit this[int i, int j]
        {
            get
            {
                return unit.Nothing;
            }
            set
            {

            }
        }
    }

    public class Single : Variable
    {
        public void Set(float value)
        {
            MyParameter.SetValue(value);
        }
    }

    public class Quad
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

        void SetupVertices(Vector2 PositionBl, Vector2 PositionTr, Vector2 UvBl, Vector2 UvTr)
        {
            const float Z = 0.0f;

            Vector3 _PositionBl = new Vector3(PositionBl.X, PositionTr.Y, Z);
            Vector3 _PositionTr = new Vector3(PositionTr.X, PositionTr.Y, Z);
            Vector3 _PositionBr = new Vector3(PositionTr.X, PositionBl.Y, Z);
            Vector3 _PositionTl = new Vector3(PositionBl.X, PositionTr.Y, Z);

            Vector2 _UvBl = new Vector2(UvBl.X, UvTr.Y);
            Vector2 _UvTr = new Vector2(UvTr.X, UvBl.Y);
            Vector2 _UvBr = new Vector2(UvTr.X, UvTr.Y);
            Vector2 _UvTl = new Vector2(UvBl.X, UvBl.Y);

            vertexData = new VertexPositionColorTexture[4];
            vertexData[TOP_LEFT]     = new VertexPositionColorTexture(_PositionTl, Color.White, _UvTl);
            vertexData[TOP_RIGHT]    = new VertexPositionColorTexture(_PositionTr, Color.White, _UvTr);
            vertexData[BOTTOM_RIGHT] = new VertexPositionColorTexture(_PositionBr, Color.White, _UvBr);
            vertexData[BOTTOM_LEFT]  = new VertexPositionColorTexture(_PositionBl, Color.White, _UvBl);
        }

        public void Draw(GraphicsDevice GraphicsDevice)
        {
            GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertexData, 0, 4, indexData, 0, 2);
        }
    }

    public class GridHelper
    {
        public static GraphicsDevice GraphicsDevice;

        static Quad UnitSquare = new Quad();

        public static void Initialize(GraphicsDevice GraphicsDevice)
        {
            GridHelper.GraphicsDevice = GraphicsDevice;
        }

        public static void DrawGrid()
        {
            UnitSquare.Draw(GraphicsDevice);
        }
    }

    public class RelativeIndex
    {
        public float i, j;
        public RelativeIndex(float i, float j) { this.i = i; this.j = j; }
    }

    public class POSITION0Attribute : Attribute { }
    public class COLOR0Attribute    : Attribute { }
    public class TEXCOORD0Attribute : Attribute { }

    public class PreambleAttribute : Attribute { }

    public class VertexShaderAttribute   : Attribute { }
    public class FragmentShaderAttribute : Attribute { }

    public class FragmentShader
    {
        protected const float _0 = 0 / 255f, _1 = 1 / 255f, _2 = 2 / 255f, _3 = 3 / 255f, _4 = 4 / 255f, _5 = 5 / 255f, _6 = 6 / 255f, _7 = 7 / 255f, _8 = 8 / 255f, _9 = 9 / 255f, _10 = 10 / 255f, _11 = 11 / 255f, _12 = 12 / 255f;

        protected static class Dir
        {
            public const float
                None  = _0, 
                Right = _1,
                Up    = _2,
                Left  = _3,
                Down  = _4,

                TurnRight = -_1,
                TurnLeft  =  _1;
        }

        protected static class Change
        {
            public const float
                Moved  = _0,
                Stayed = _1;
        }

        [Preamble]
        protected static readonly RelativeIndex
            RightOne = new RelativeIndex( 1,  0),
            LeftOne  = new RelativeIndex(-1,  0),
            UpOne    = new RelativeIndex( 0,  1),
            DownOne  = new RelativeIndex( 0, -1),
            Here     = new RelativeIndex( 0,  0);


        protected static void Run(UnitField Output)
        {
            GridHelper.GraphicsDevice.SetRenderTarget(Output.RenderTarget);
            GridHelper.GraphicsDevice.Clear(Color.Transparent);
            GridHelper.DrawGrid();
        }

        static bool Something(unit u)
        {
            return u.direction > 0;
        }

        static bool IsValid(float direction)
        {
            return direction > 0;
        }

        static unit output;

        static void TurnLeft(ref unit u)
        {
            u.direction += Dir.TurnLeft;
            if (u.direction > Dir.Down)
                u.direction = Dir.Right;
        }

        static void TurnRight(ref unit u)
        {
            u.direction += Dir.TurnRight;
            if (u.direction < Dir.Right)
                u.direction = Dir.Down;
        }

        static float cos(float angle)
        {
            return (float)Math.Cos((float)angle);
        }

        static float sin(float angle)
        {
            return (float)Math.Sin((float)angle);
        }

        static RelativeIndex dir_to_vec(float direction)
        {
            float angle = (float)((direction * 255 - 1) * (3.1415926 / 2.0));
            return IsValid(direction) ? new RelativeIndex(cos(angle), sin(angle)) : new RelativeIndex(0, 0);
        }

        public struct VertexOut
        {
	        [POSITION0] public vec4 Position;
            [COLOR0]    public vec4 Color;
            [TEXCOORD0] public vec2 TexCoords;

            public VertexOut() { Position = vec4.Zero; Color = vec4.Zero; TexCoords = vec2.Zero; }
            public static VertexOut Zero = new VertexOut();
        }

        public struct PixelToFrame
        {
            [COLOR0] public vec4 Color;
        }

        public class Vertex
        {
            public vec3 Position;
            public vec4 Color;
            public vec2 TextureCoordinate;

            public Vertex(vec3 Position, vec4 Color, vec2 TextureCoordinate)
            {
                this.Position = Position;
                this.Color = Color;
                this.TextureCoordinate = TextureCoordinate;
            }
        }

        [VertexShader]
        public static VertexOut SimpleVertexShader(Vertex data, vec4 cameraPos, float cameraAspect)
        {
            VertexOut Output = VertexOut.Zero;

            Output.Position.w = 1;
    
            Output.Position.x = (data.Position.x - cameraPos.x) / cameraAspect * cameraPos.z;
            Output.Position.y = (data.Position.y - cameraPos.y) * cameraPos.w;

            Output.TexCoords = data.TextureCoordinate;
            Output.Color = data.Color;
    
            return Output;
        }

        [FragmentShader]
        public static unit Movement_Phase1(VertexOut vertex, UnitField Current)
        {
            unit here = Current[Here] = unit.Nothing;
            
            // Check if something is here already
            if (Something(here))
            {
                // If so, they have the right to stay so keep them here
                output = here;
                output.change = Change.Stayed;
                return output;
            }
            else
            {
                // Otherwise, check each direction to see if something is incoming
                unit
                    right = Current[RightOne],
                    up    = Current[UpOne],
                    left  = Current[LeftOne],
                    down  = Current[DownOne];

                if (right.direction == Dir.Left)  output = right;
                if (up.   direction == Dir.Down)  output = up;
                if (left. direction == Dir.Right) output = left;
                if (down. direction == Dir.Up)    output = down;

                output.change = Change.Moved;

                return output;
            }
        }

        [FragmentShader]
        public static unit Movement_Phase2(UnitField Current, UnitField Previous)
        {
	        unit result = Current[Here];
	        unit prior = Previous[Here];
	
	        unit ahead = Current[dir_to_vec(prior.direction)];
	        if (ahead.change == Change.Moved && ahead.direction == prior.direction)
		        result = unit.Nothing;

	        // If unit hasn't moved, change direction
	        if (result.a == prior.a && Something(result))
                TurnLeft(ref result);
	
	        return result;
        }
    }
}";

    }
}
