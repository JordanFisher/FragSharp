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
            writer.WriteLine(str);
        }

        public static void WriteLineIndented(this StringWriter writer, string indent, string format, params object[] paramlist)
        {
            writer.Write(indent);
            writer.WriteLine(format, paramlist);
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

            Compilation compilation = Compilation.Create("MyCompilation",
                                                         syntaxTrees: new List<SyntaxTree>() { Tree },
                                                         references: new List<MetadataReference>() { MetadataReference.CreateAssemblyReference(typeof(object).Assembly.FullName) });
            model = compilation.GetSemanticModel(Tree);




            var Methods = GetMethods(Root);
            Methods.ForEach(method => Console.WriteLine(method.Identifier.Value));

            var GridMethods = Methods.Where(method => HasAttribute(method, "GridComputation")).ToList();
            var output = CompileMethod(GridMethods[0]);

            //Print(Root);
            
            Console.WriteLine("\nDone!");
        }

        static string CompileMethod(MethodDeclarationSyntax method)
        {
            var output = new StringWriter();

            output.Write(FragmentShaderBegin);

            CompileStatement(method.Body, output, TabSpace);

            output.Write(FragmentShaderEnd);

            output.Write(FileEnd);

            return output.ToString();
        }

        const string _0 = "0/255.0", _1 = "1/255.0", _2 = "2/255.0", _3 = "3/255.0", _4 = "4/255.0", _5 = "5/255.0", _6 = "6/255.0", _7 = "7/255.0", _8 = "8/255.0", _9 = "9/255.0", _10 = "10/255.0", _11 = "11/255.0", _12 = "12/255.0";

        static Dictionary<string, string> TypeMap = new Dictionary<string, string>() {
            { "unit", "float4" },
        };

        static Dictionary<string, string> SymbolMap = new Dictionary<string,string>() {
            { "output" , "Output.Color" },

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

            { "Change.Moved"  , _0 },
            { "Change.Stayed" , _1 },
        };

        static Dictionary<string, string> MemberMap = new Dictionary<string, string>() {
            { "direction", "r" },
            { "change",    "g" },
        };

        static readonly string TabSpace = "  ";

        static readonly string FragmentShaderBegin = string.Format(@"
PixelToFrame FragmentShader(VertexToPixel psin)
{{
{0}PixelToFrame Output = (PixelToFrame)0;
", TabSpace);

        static readonly string FragmentShaderEnd = string.Format(@"
{0}return Output;
}}
", TabSpace);

        static readonly string FileEnd = string.Format(@"
technique Simplest
{{
{0}pass Pass0
{0}{{
{0}{0}VertexShader = compile VERTEX_SHADER GridComputationVertexShader();
{0}{0}PixelShader = compile PIXEL_SHADER FragmentShader();
{0}}}
}}
", TabSpace);

        static void CompileStatement(StatementSyntax statement, StringWriter output, string indent)
        {
            if      (statement is IfStatementSyntax)               CompileIfStatement(              (IfStatementSyntax)              statement, output, indent);
            else if (statement is LocalDeclarationStatementSyntax) CompileLocalDeclarationStatement((LocalDeclarationStatementSyntax)statement, output, indent);
            else if (statement is BlockSyntax)                     CompileBlock(                    (BlockSyntax)                    statement, output, indent);
            else if (statement is ExpressionStatementSyntax)       CompileExpressionStatement(      (ExpressionStatementSyntax)      statement, output, indent);
            else if (statement is StatementSyntax)                 output.WriteLine("{0}statement {1}", indent, statement.GetType());
        }

        static void CompileIfStatement(IfStatementSyntax statement, StringWriter output, string indent)
        {
            output.WriteIndented(indent, "if (");
            CompileExpression(statement.Condition, output);
            output.Write(")\n");

            output.WriteLineIndented(indent, "{");

            CompileStatement(statement.Statement, output, indent + TabSpace);

            output.WriteLineIndented(indent, "}");

            if (statement.Else == null) return;
            
            output.WriteLineIndented(indent, "else");
            output.WriteLineIndented(indent, "{");

            CompileStatement(statement.Else.Statement, output, indent + TabSpace);

            output.WriteLineIndented(indent, "}");
        }

        static void CompileLocalDeclarationStatement(LocalDeclarationStatementSyntax statement, StringWriter output, string indent)
        {
            CompileVariableDeclaration(statement.Declaration, output, indent);
        }

        static void CompileVariableDeclaration(VariableDeclarationSyntax declaration, StringWriter output, string indent)
        {
            string type = declaration.Type.ToString();

            if (TypeMap.ContainsKey(type))
            {
                output.WriteIndented(indent, TypeMap[type]);
            }
            else
            {
                output.WriteIndented(indent, type);
            }

            var last = declaration.Variables.Last();
            foreach (var variable in declaration.Variables)
            {
                output.Write(" ");
                CompileVariableDeclarator(variable, output);
                output.Write(variable == last ? ';' : ',');
            }

            output.Write("\n");
        }

        static void CompileVariableDeclarator(VariableDeclaratorSyntax declarator, StringWriter output)
        {
            output.Write(declarator.Identifier);
            CompileEqualsValueClause(declarator.Initializer, output);
        }

        static void CompileEqualsValueClause(EqualsValueClauseSyntax clause, StringWriter output)
        {
            output.Write(" = ");
            CompileExpression(clause.Value, output);
        }

        static void CompileElementAccessExpression(ElementAccessExpressionSyntax expression, StringWriter output)
        {
            //var info = model.GetSymbolInfo(expression.ArgumentList.Arguments[0].Expression);
            //info.Symbol

            output.Write("tex2D(");
            CompileExpression(expression.Expression, output);
            output.Write(", ");
            output.Write("PSIn.TexCoords + ");
            CompileExpression(expression.ArgumentList.Arguments[0].Expression, output);
            output.Write(")");
        }

        static void CompileInvocationExpression(InvocationExpressionSyntax expression, StringWriter output)
        {
            CompileExpression(expression.Expression, output);
            output.Write("(");
            CompileArgumentList(expression.ArgumentList, output);
            output.Write(")");
        }

        static void CompileArgumentList(ArgumentListSyntax list, StringWriter output)
        {
            var last = list.Arguments.Last();
            foreach (var argument in list.Arguments)
            {
                CompileExpression(argument.Expression, output);
                output.Write(argument == last ? "" : ", ");
            }

        }

        static string CreateRelativeIndex(int i, int j)
        {
            return string.Format("float2(({0}+.5) * dx, ({1}+.5) * dy)", i, j);
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
            output.Write(";\n");
        }

        static void CompileExpression(ExpressionSyntax expression, StringWriter output)
        {
            if      (expression is BinaryExpressionSyntax)        CompileBinaryExpression(       (BinaryExpressionSyntax)       expression, output);
            else if (expression is MemberAccessExpressionSyntax)  CompileMemberAccessExpression( (MemberAccessExpressionSyntax) expression, output);
            else if (expression is IdentifierNameSyntax)          CompileIdentifierName(         (IdentifierNameSyntax)         expression, output);
            else if (expression is ElementAccessExpressionSyntax) CompileElementAccessExpression((ElementAccessExpressionSyntax)expression, output);
            else if (expression is InvocationExpressionSyntax)    CompileInvocationExpression(   (InvocationExpressionSyntax)   expression, output);
            else output.Write("expression " + expression.GetType().Name);
        }

        static void CompileBinaryExpression(BinaryExpressionSyntax expression, StringWriter output)
        {
            CompileExpression(expression.Left, output);
            output.Write(" {0} ", expression.OperatorToken);
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
public class GridComputation
{
        protected const float _0 = 0 / 255f, _1 = 1 / 255f, _2 = 2 / 255f, _3 = 3 / 255f, _4 = 4 / 255f, _5 = 5 / 255f, _6 = 6 / 255f, _7 = 7 / 255f, _8 = 8 / 255f, _9 = 9 / 255f, _10 = 10 / 255f, _11 = 11 / 255f, _12 = 12 / 255f;

        protected static class Dir
        {
            public const float
                None  = _0, 
                Right = _1,
                Up    = _2,
                Left  = _3,
                Down  = _4;
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

    [Inline]
    static bool IsValid(Dir direction)
    {
        return direction > 0;
    }

    static unit output;

    [GridComputation]
    public static void Movement_Phase1(UnitField Current, UnitField Output)
    {
	    // Check four directions to see if something is incoming
	    unit right = Current[RightOne];
	    if (right.direction == Dir. Left)  output = right;

	    unit up = Current[UpOne];
	    if (up.direction    == Dir.Down)  output = up;

	    unit left = Current[LeftOne];
	    if (left.direction  == Dir.Right) output = left;

	    unit down = Current[DownOne];
	    if (down.direction  == Dir.Up)    output = down;
        else
output.change = Change.Moved;

	    output.change = Change.Moved;

	    // If something is here already, they have the right to stay here
	    unit here = Current[Here];
	    if (IsValid(here.direction))
	    {
            IsValid(here.direction, here.change);
		    output = here;
		    output.change = Change.Stayed;
	    }
    }
}";

    }
}
