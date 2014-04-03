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

            //Compilation compilation = Compilation.Create("MyCompilation",
            //                                             syntaxTrees: new List<SyntaxTree>() { Tree },
            //                                             references: new List<MetadataReference>() { MetadataReference.CreateAssemblyReference(typeof(object).Assembly.FullName) });
            //model = compilation.GetSemanticModel(Tree);




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

            CompileStatement(method.Body, output, "");

            return output.ToString();
        }

        static Dictionary<string, string> TypeMap = new Dictionary<string, string>() {
            { "unit", "float4" },
        };

        static Dictionary<string, string> MemberMap = new Dictionary<string, string>() {
            { "direction", "r" },
            { "change",    "g" },
        };

        static string TabSpace = "  ";

        static void CompileStatement(StatementSyntax statement, StringWriter output, string indent)
        {
            if      (statement is IfStatementSyntax)               CompileIfStatement(              (IfStatementSyntax)              statement, output, indent);
            else if (statement is LocalDeclarationStatementSyntax) CompileLocalDeclarationStatement((LocalDeclarationStatementSyntax)statement, output, indent);
            else if (statement is BlockSyntax)                     CompileBlock(                    (BlockSyntax)                    statement, output, indent);
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
            output.WriteIndented(indent, declaration.Type.ToString());

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
            output.Write("tex2D(");
            CompileExpression(expression.Expression, output);
            output.Write(", ");
            output.Write("PSIn.TexCoords + float2(({0}+.5) * dx, ({1}+.5) * dy)", -1, 1);
            output.Write(")");

            //expression.ArgumentList
        }

        static void CompileBlock(BlockSyntax block, StringWriter output, string indent)
        {
            foreach (var statement in block.Statements)
            {
                CompileStatement(statement, output, indent);
            }
        }

        static void CompileExpression(ExpressionSyntax expression, StringWriter output)
        {
            if      (expression is BinaryExpressionSyntax)        CompileBinaryExpression(       (BinaryExpressionSyntax)       expression, output);
            else if (expression is MemberAccessExpressionSyntax)  CompileMemberAccessExpression( (MemberAccessExpressionSyntax) expression, output);
            else if (expression is IdentifierNameSyntax)          CompileIdentifierName(         (IdentifierNameSyntax)         expression, output);
            else if (expression is ElementAccessExpressionSyntax) CompileElementAccessExpression((ElementAccessExpressionSyntax)expression, output);
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
            CompileExpression(expression.Expression, output);
            output.Write(".");

            try
            {
                var member = expression.Name.Identifier.ValueText;
                var mapped = MemberMap[member];
                output.Write(mapped);
            }
            catch
            {
                output.Write("ERROR {0}", expression.Name);
            }
        }

        static void CompileIdentifierName(IdentifierNameSyntax syntax, StringWriter output)
        {
            output.Write(syntax.Identifier.ValueText);
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
    static RelativeIndex RightOne = new RelativeIndex( 1 ,  0 ),
                            LeftOne  = new RelativeIndex(-1 ,  0 ),
                            UpOne    = new RelativeIndex( 0 ,  1 ),
                            DownOne  = new RelativeIndex( 0 , -1 ),
                            Here     = new RelativeIndex( 0 ,  0 );

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
	    if (right.direction == Dir.Left)  output = right;

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
		    output = here;
		    output.change = Change.Stayed;
	    }
    }
}";

    }
}
