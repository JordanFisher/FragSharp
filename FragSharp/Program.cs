using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace TransformationCS
{
    public static class StringWriterExtension
    {
        public static void WriteLineIndented(this StringWriter writer, string indent, string str)
        {
            writer.Write(indent);
            writer.Write(str);
            writer.Write(Program.LineBreak);
        }

        public static void WriteLineIndented(this StringWriter writer, string indent, string format, params object[] paramlist)
        {
            writer.Write(indent);
            writer.Write(format, paramlist);
            writer.Write(Program.LineBreak);
        }


        public static void WriteIndented(this StringWriter writer, string indent)
        {
            writer.Write(indent);
        }

        public static void WriteIndented(this StringWriter writer, string indent, string str)
        {
            writer.Write(indent);
            writer.Write(str);
        }

        public static void WriteIndented(this StringWriter writer, string indent, string format, params object[] paramlist)
        {
            writer.Write(indent);
            writer.Write(format, paramlist);
        }
    }

    internal class Program
    {
        static SemanticModel model;
        static Compilation compilation;

        private static void Main()
        {
            var Tree = SyntaxTree.ParseText(Code);
            var Root = Tree.GetRoot();

            compilation = Compilation.Create("MyCompilation",
                                             syntaxTrees: new List<SyntaxTree>() { Tree },
                                             references: new List<MetadataReference>() { MetadataReference.CreateAssemblyReference(typeof(object).Assembly.FullName) });
            model = compilation.GetSemanticModel(Tree);

            var Methods = GetMethods(Root);
            Methods.ForEach(method => Console.WriteLine(method.Identifier.Value));

            var GridMethods = Methods.Where(method => HasAttribute(method, "FragmentShader")).ToList();
            var output = CompileFragmentMethod(GridMethods[0]);

            Console.WriteLine("\nDone!");
        }

        static int SamplerNumber;
        static List<Symbol> ReferencedMethods = new List<Symbol>();
        static void StartMethodCompilation()
        {
            SamplerNumber = 0;
            ReferencedMethods.Clear();
        }

        static string CompileMethod(Symbol symbol)
        {
            string compilation = string.Empty;

            if (SymbolCompilation.ContainsKey(symbol))
            {
                compilation = SymbolCompilation[symbol];
            }
            else
            {
                var method = symbol.DeclaringSyntaxNodes[0] as MethodDeclarationSyntax;

                var output = new StringWriter();

                CompileMethodSignature(method, output);

                output.Write("{");
                output.Write(LineBreak);

                CompileStatement(method.Body, output, Tab);

                output.Write("}");
                output.Write(LineBreak);

                compilation = output.ToString();
                SymbolCompilation.Add(symbol, compilation);
            }

            // Add the method to the look up dictionary once we are done.
            // It's important to do this last, so that any recursively added methods will be added to this list first.
            // Order of decleration may matter in a target language.
            ReferencedMethods.Add(symbol);

            return compilation;
        }

        static void CompileMethodSignature(MethodDeclarationSyntax method, StringWriter output)
        {
            CompileExpression(method.ReturnType, output);
            output.Write(" {0}", method.Identifier.ValueText);
            output.Write("(");
            
            var last = method.ParameterList.Parameters.Last();
            foreach (var parameter in method.ParameterList.Parameters)
            {
                CompileMethodParameter(parameter, output);

                if (parameter != last)
                    output.Write(", ");
            }
            
            output.Write(")");
            output.Write(LineBreak);
        }

        static void CompileMethodParameter(ParameterSyntax parameter, StringWriter output)
        {
            CompileExpression(parameter.Type, output);
            output.Write(" {0}", parameter.Identifier.ValueText);
        }

        static string CompileFragmentMethod(MethodDeclarationSyntax method)
        {
            StartMethodCompilation();

            StringWriter
                header = new StringWriter(),
                shader = new StringWriter(),
                methods = new StringWriter();

            header.Write(FileBegin);

            CompileShaderSignature(method, header);

            shader.Write(FragmentShaderBegin);
            CompileStatement(method.Body, shader, Tab);
            shader.Write(FragmentShaderEnd);

            shader.Write(FileEnd);

            // We must wait until after compiling the shader to know which methods that shader references.
            IncludeReferencedMethods(methods);

            header.Write(LineBreak);
            header.Write(methods);
            header.Write(shader);
            return header.ToString();
        }

        static void IncludeReferencedMethods(StringWriter output)
        {
            var last = ReferencedMethods.Last();
            foreach (var method in ReferencedMethods)
            {
                output.Write(SymbolCompilation[method]);
                
                if (method != last)
                    output.Write(LineBreak);
            }
        }

        static void CompileShaderSignature(MethodDeclarationSyntax method, StringWriter output)
        {
            foreach (var parameter in method.ParameterList.Parameters)
            {
                CompileShaderParameter(parameter, output);
            }
        }

        static void CompileShaderParameter(ParameterSyntax parameter, StringWriter output)
        {
            string type = parameter.Type.ToString();

            if (type == "UnitField")
            {
                CompileSamplerParameter(parameter, output);
            }
        }

        static void CompileSamplerParameter(ParameterSyntax parameter, StringWriter output)
        {
            SamplerNumber++;

            output.WriteLine(@"
// Texture Sampler for UnitField {2}, using register location {1}
Texture {2};
sampler {2}Sampler : register(s{1}) = sampler_state
{{
{0}texture   = <{2}>;
{0}MipFilter = Point;
{0}MagFilter = Point;
{0}MinFilter = Point;
{0}AddressU  = Wrap;
{0}AddressV  = Wrap;
}};", Tab, SamplerNumber, parameter.Identifier.ValueText);

        }

        const string _0 = "0/255.0", _1 = "1/255.0", _2 = "2/255.0", _3 = "3/255.0", _4 = "4/255.0", _5 = "5/255.0", _6 = "6/255.0", _7 = "7/255.0", _8 = "8/255.0", _9 = "9/255.0", _10 = "10/255.0", _11 = "11/255.0", _12 = "12/255.0";

        static Dictionary<Symbol, string> SymbolCompilation = new Dictionary<Symbol, string>();


        static Dictionary<string, string> SymbolMap = new Dictionary<string,string>() {
            { "unit" , "float4" },

            { "vec2" , "float2" },
            { "vec3" , "float3" },
            { "vec4" , "float4" },

            { "RelativeIndex" , "float2" },

            { "cos" , "cos" },
            { "sin" , "sin" },

            { "unit.Nothing" , "float4(0,0,0,0)" },

            { "RightOne" , CreateRelativeIndex( 1,  0) },
            { "LeftOne"  , CreateRelativeIndex(-1,  0) },
            { "UpOne"    , CreateRelativeIndex( 0,  1) },
            { "DownOne"  , CreateRelativeIndex( 0, -1) },
            { "Here"     , CreateRelativeIndex( 0,  0) },
            
            { "Dir.None"  , _0 },
            { "Dir.Right" , _1 },
            { "Dir.Up"    , _2 },
            { "Dir.Left"  , _3 },
            { "Dir.Down"  , _4 },

            { "TurnRight" , "-1/255.0" },
            { "TurnLeft"  ,  "1/255.0" },


            { "Change.Moved"  , _0 },
            { "Change.Stayed" , _1 },
        };

        static Dictionary<string, string> MemberMap = new Dictionary<string, string>() {
            { "x", "x" },
            { "y", "y" },

            { "xy", "xy" },
            
            { "xyz", "xyz" },

            { "r", "r" },
            { "g", "g" },
            { "b", "b" },
            { "a", "a" },

            { "direction", "r" },
            { "change",    "g" },
        };

        public static bool Minify = false;
        
        public static string Tab
        {
            get
            {
                return Minify ? string.Empty : "  ";
            }
        }

        public static string Space
        {
            get
            {
                return Minify ? string.Empty : " ";
            }
        }

        public static string LineBreak
        {
            get
            {
                return Minify ? string.Empty : "\n";
            }
        }

        static readonly string FragmentShaderBegin = string.Format(@"
// Auto-generated fragment shader
PixelToFrame FragmentShader(VertexToPixel psin)
{{
{0}PixelToFrame __FinalOutput = (PixelToFrame)0;
", Tab);

        static readonly string FragmentShaderEnd = "}";

        static readonly string FileBegin = string.Format(
@"// This file was auto-generated by FragSharp. It will be regenerated on the next compilation.
// Manual changes made will not persist and may cause incorrect behavior between compilations.

// Vertex shader data structure definition
struct VertexToPixel
{{
{0}float4 Position   : POSITION0;
{0}float4 Color      : COLOR0;
{0}float2 TexCoords  : TEXCOORD0;
{0}float2 Position2D : TEXCOORD2;
}};

// Fragment shader data structure definition
struct PixelToFrame
{{
{0}float4 Color      : COLOR0;
}};

// Vertex Shader
VertexToPixel StandardVertexShader( float2 inPos : POSITION0, float2 inTexCoords : TEXCOORD0, float4 inColor : COLOR0)
{{
    VertexToPixel Output = (VertexToPixel)0;    

    Output.Position.w = 1;
    
    Output.Position.x = (inPos.x - xCameraPos.x) / xCameraAspect * xCameraPos.z;
    Output.Position.y = (inPos.y - xCameraPos.y) * xCameraPos.w;

	Output.Position2D = Output.Position.xy;

    Output.TexCoords = inTexCoords;
    Output.Color = inColor;
    
    return Output;
}}
", Tab);

        static readonly string FileEnd = string.Format(@"
// Shader compilation
technique Simplest
{{
{0}pass Pass0
{0}{{
{0}{0}VertexShader = compile VERTEX_SHADER StandardVertexShader();
{0}{0}PixelShader = compile PIXEL_SHADER FragmentShader();
{0}}}
}}
", Tab);

        static void CompileStatement(StatementSyntax statement, StringWriter output, string indent)
        {
            if      (statement is IfStatementSyntax)               CompileIfStatement(              (IfStatementSyntax)              statement, output, indent);
            else if (statement is LocalDeclarationStatementSyntax) CompileLocalDeclarationStatement((LocalDeclarationStatementSyntax)statement, output, indent);
            else if (statement is BlockSyntax)                     CompileBlock(                    (BlockSyntax)                    statement, output, indent);
            else if (statement is ExpressionStatementSyntax)       CompileExpressionStatement(      (ExpressionStatementSyntax)      statement, output, indent);
            else if (statement is ReturnStatementSyntax)           CompileReturnStatement(          (ReturnStatementSyntax)          statement, output, indent);
            else if (statement is StatementSyntax)                 output.WriteLine("{0}statement {1}", indent, statement.GetType());
        }

        static void CompileIfStatement(IfStatementSyntax statement, StringWriter output, string indent)
        {
            output.WriteIndented(indent, "if{0}(", Space);
            CompileExpression(statement.Condition, output);
            output.Write(")\n");

            output.WriteLineIndented(indent, "{");

            CompileStatement(statement.Statement, output, indent + Tab);

            output.WriteLineIndented(indent, "}");

            if (statement.Else == null) return;
            
            output.WriteLineIndented(indent, "else");
            output.WriteLineIndented(indent, "{");

            CompileStatement(statement.Else.Statement, output, indent + Tab);

            output.WriteLineIndented(indent, "}");
        }

        static void CompileLocalDeclarationStatement(LocalDeclarationStatementSyntax statement, StringWriter output, string indent)
        {
            CompileVariableDeclaration(statement.Declaration, output, indent);
        }

        static void CompileType(TypeSyntax type, StringWriter output)
        {
            string name = type.ToString();

            if (SymbolMap.ContainsKey(name))
            {
                output.Write(SymbolMap[name]);
            }
            else
            {
                output.Write(name);
            }
        }

        static void CompileLiteralExpression(LiteralExpressionSyntax literal, StringWriter output)
        {
            var get = model.GetConstantValue(literal);

            if (get.HasValue)
            {
                var val = get.Value;

                if      (val is int)    output.Write(val);
                else if (val is float)  output.Write(val);
                else if (val is double) output.Write(val);
                else                    output.Write("ERROR(Unsupported Literal : {0})", val);
            }
            else
            {
                output.Write("ERROR(Improper Literal : {0})", literal);
            }
        }

        static void CompileConditionalExpression(ConditionalExpressionSyntax conditional, StringWriter output)
        {
            CompileExpression(conditional.Condition, output);
            output.Write("{0}?{0}", Space);
            CompileExpression(conditional.WhenTrue, output);
            output.Write("{0}:{0}", Space);
            CompileExpression(conditional.WhenFalse, output);
        }

        static void CompileObjectCreationExpression(ObjectCreationExpressionSyntax creation, StringWriter output)
        {
            CompileExpression(creation.Type, output);

            output.Write("(");
            CompileArgumentList(creation.ArgumentList, output);
            output.Write(")");
        }

        static void CompileVariableDeclaration(VariableDeclarationSyntax declaration, StringWriter output, string indent)
        {
            output.WriteIndented(indent);

            CompileExpression(declaration.Type, output);

            var last = declaration.Variables.Last();
            foreach (var variable in declaration.Variables)
            {
                output.Write(" ");
                CompileVariableDeclarator(variable, output);
                output.Write(variable == last ? ';' : ',');
            }

            output.Write(LineBreak);
        }

        static void CompileVariableDeclarator(VariableDeclaratorSyntax declarator, StringWriter output)
        {
            output.Write(declarator.Identifier);
            CompileEqualsValueClause(declarator.Initializer, output);
        }

        static void CompileEqualsValueClause(EqualsValueClauseSyntax clause, StringWriter output)
        {
            output.Write("{0}={0}", Space);
            CompileExpression(clause.Value, output);
        }

        static void CompileElementAccessExpression(ElementAccessExpressionSyntax expression, StringWriter output)
        {
            //var info = model.GetSymbolInfo(expression.ArgumentList.Arguments[0].Expression);
            //info.Symbol

            output.Write("tex2D(");
            CompileExpression(expression.Expression, output);
            output.Write(",{0}", Space);
            output.Write("PSIn.TexCoords{0}+{0}(float2(.5,.5){0}+{0}(", Space);
            CompileExpression(expression.ArgumentList.Arguments[0].Expression, output);
            output.Write("){0}*{0}float2(dx,{0}dy)))", Space);
        }

        static void CompileInvocationExpression(InvocationExpressionSyntax expression, StringWriter output)
        {
            if (SymbolMap.ContainsKey(expression.Expression.ToString()))
            {
                output.Write(SymbolMap[expression.Expression.ToString()]);
            }
            else
            {
                var info = model.GetSymbolInfo(expression.Expression);
                CompileMethod(info.Symbol);
                CompileExpression(expression.Expression, output);
            }
            
            // Default: compile expression yielding the function
            //CompileExpression(expression.Expression, output);

            output.Write("(");
            CompileArgumentList(expression.ArgumentList, output);
            output.Write(")");
        }

        static void CompileCastExpression(CastExpressionSyntax expression, StringWriter output)
        {
            EncloseInParanthesis(expression.Type, output);

            CompileExpression(expression.Expression, output);
        }

        static void CompileParenthesizedExpression(ParenthesizedExpressionSyntax expression, StringWriter output)
        {
            EncloseInParanthesis(expression.Expression, output);
        }

        static void EncloseInParanthesis(ExpressionSyntax expression, StringWriter output)
        {
            if (expression is ParenthesizedExpressionSyntax)
            {
                CompileExpression(expression, output);
            }
            else
            {
                output.Write("(");
                CompileExpression(expression, output);
                output.Write(")");
            }
        }

        static void CompileArgumentList(ArgumentListSyntax list, StringWriter output)
        {
            var last = list.Arguments.Last();
            foreach (var argument in list.Arguments)
            {
                CompileExpression(argument.Expression, output);
                output.Write(argument == last ? "" : ",{0}", Space);
            }

        }

        static string CreateRelativeIndex(int i, int j)
        {
            return string.Format("float2({0},{2}{1})", i, j, Space);
        }

        static void CompileBlock(BlockSyntax block, StringWriter output, string indent)
        {
            foreach (var statement in block.Statements)
            {
                CompileStatement(statement, output, indent);
            }
        }

        static void CompileExpressionStatement(ExpressionStatementSyntax statement, StringWriter output, string indent)
        {
            output.WriteIndented(indent);
            CompileExpression(statement.Expression, output);
            output.Write(";{0}", LineBreak);
        }

        static void CompileReturnStatement(ReturnStatementSyntax statement, StringWriter output, string indent)
        {
            output.WriteIndented(indent, "__FinalOutput.Color{0}={0}", Space);
            CompileExpression(statement.Expression, output);
            output.Write(";");
            output.Write(LineBreak);
            output.WriteLineIndented(indent, "return __FinalOutput;");

            //output.WriteIndented(indent, "return ");
            //CompileExpression(statement.Expression, output);
            //output.Write(";");
            //output.Write(LineBreak);
        }

        static void CompileExpression(ExpressionSyntax expression, StringWriter output)
        {
            if      (expression is BinaryExpressionSyntax)         CompileBinaryExpression(        (BinaryExpressionSyntax)        expression, output);
            else if (expression is MemberAccessExpressionSyntax)   CompileMemberAccessExpression(  (MemberAccessExpressionSyntax)  expression, output);
            else if (expression is IdentifierNameSyntax)           CompileIdentifierName(          (IdentifierNameSyntax)          expression, output);
            else if (expression is ElementAccessExpressionSyntax)  CompileElementAccessExpression( (ElementAccessExpressionSyntax) expression, output);
            else if (expression is InvocationExpressionSyntax)     CompileInvocationExpression(    (InvocationExpressionSyntax)    expression, output);
            else if (expression is CastExpressionSyntax)           CompileCastExpression(          (CastExpressionSyntax)          expression, output);
            else if (expression is ParenthesizedExpressionSyntax)  CompileParenthesizedExpression( (ParenthesizedExpressionSyntax) expression, output);
            else if (expression is TypeSyntax)                     CompileType(                    (TypeSyntax)                    expression, output);
            else if (expression is LiteralExpressionSyntax)        CompileLiteralExpression(       (LiteralExpressionSyntax)       expression, output);
            else if (expression is ConditionalExpressionSyntax)    CompileConditionalExpression(   (ConditionalExpressionSyntax)   expression, output);
            else if (expression is ObjectCreationExpressionSyntax) CompileObjectCreationExpression((ObjectCreationExpressionSyntax)expression, output);
            else output.Write("expression " + expression.GetType().Name);
        }

        static void CompileBinaryExpression(BinaryExpressionSyntax expression, StringWriter output)
        {
            CompileExpression(expression.Left, output);
            output.Write("{1}{0}{1}", expression.OperatorToken, Space);
            CompileExpression(expression.Right, output);
        }

        static void CompileMemberAccessExpression(MemberAccessExpressionSyntax expression, StringWriter output)
        {
            string member = expression.Name.Identifier.ValueText;
            
            string access = expression.Expression + "." + member;
            if (SymbolMap.ContainsKey(access))
            {
                output.Write(SymbolMap[access]);
                return;
            }

            if (MemberMap.ContainsKey(member))
            {
                CompileExpression(expression.Expression, output);
                output.Write(".");

                var mapped = MemberMap[member];
                output.Write(mapped);
            }
            else
            {
                output.Write("ERROR(MemberAccess: {0})", expression);
            }
        }

        static void CompileIdentifierName(IdentifierNameSyntax syntax, StringWriter output)
        {
            string identifier = syntax.Identifier.ValueText;

            if (SymbolMap.ContainsKey(identifier))
            {
                output.Write(SymbolMap[identifier]);
            }
            else
            {
                output.Write(identifier);
            }
        }


        static void PrintTree(SyntaxNodeOrToken node, StringWriter output, string indent = "")
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

                PrintTree(child, output, indent + "--");
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

        static void Print(SyntaxNodeOrToken node, string indent = "")
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

                Console.WriteLine("{0}{1}  {2}", indent, kind, value);

                Print(child, indent + "  ");
            }
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
            unit here = Current[Here], output = unit.Nothing;
            
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
