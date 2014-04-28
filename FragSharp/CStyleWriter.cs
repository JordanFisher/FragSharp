using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

using FragSharpFramework;

namespace FragSharp
{
    class _Indent : IDisposable
    {
        AbstractCodeWriter writer;
        string starting_indent;

        public _Indent(AbstractCodeWriter writer)
        {
            this.writer = writer;
            starting_indent = writer.CurrentIndent;
        }

        public void Dispose()
        {
            writer.CurrentIndent = starting_indent;
        }
    }

    internal abstract class CStyleWriter : AbstractCodeWriter
    {
        public CStyleWriter(Dictionary<SyntaxTree, SemanticModel> models, Compilation compilation)
            : base(models, compilation)
        {
        }

        public CStyleWriter(HlslWriter writer)
            : base(writer)
        {
        }

        override protected CompiledMethod CompileMethod(Symbol symbol)
        {
            ClearString();

            if (SymbolCompilation.ContainsKey(symbol))
            {
                return SymbolCompilation[symbol];
            }
            else
            {
                var method = symbol.DeclaringSyntaxNodes[0] as MethodDeclarationSyntax;

                CompileMethodSignature(method);

                WriteLine("{");

                ResetIndent();

                var PrevIndent = Indent();
                
                CompileStatement(method.Body);
                
                RestoreIndent(PrevIndent);

                WriteLine("}");

                var compilation = new CompiledMethod(GetString(), ReferencedMethods);

                SymbolCompilation.Add(symbol, compilation);

                return compilation;
            }
        }

        override protected void CompileMethodSignature(MethodDeclarationSyntax method)
        {
            var symbol = GetSymbol(method) as MethodSymbol;

            CompileExpression(method.ReturnType);

            Write(" ");
            WriteFullMethodName(symbol);
            //Write(" {0}", method.Identifier.ValueText);
            
            Write("(");

            var Params = method.ParameterList.Parameters;

            // If there is a sampler paramter we need to pass in the VertexToPixel variable.
            // We add a paramter spot at the beginning for it.
            if (Params.Count > 0 && Params.Any(param => IsSampler(param)))
            {
                Write(VertexToPixelDecl + Comma);
            }

            // Add each parameter, comma separated
            var last = Params.Last();
            foreach (var parameter in Params)
            {
                CompileMethodParameter(parameter);

                if (parameter != last)
                    Write(Comma);
            }

            Write(")");
            Write(LineBreak);
        }

        override protected void CompileMethodParameter(ParameterSyntax parameter)
        {
            // Check if symbol is a shader variable
            if (IsSampler(parameter))
            {
                Write("sampler {0}, float2 {0}_{1}, float2 {0}_{2}", parameter.Identifier.ValueText, Sampler.SizeSuffix, Sampler.DxDySuffix);
            }
            else
            {
                if (parameter.Modifiers.ToList().Any(modifier => modifier.IsKeyword() && modifier.ToString() == "ref"))
                {
                    Write("inout ");
                }

                CompileExpression(parameter.Type);
                Write(" {0}", parameter.Identifier.ValueText);
            }
        }

        override protected void CompileIfStatement(IfStatementSyntax statement)
        {
            BeginLine("if{0}(", Space);
            CompileExpression(statement.Condition);
            EndLine(")");

            WriteLine("{");

            var PrevIndent = Indent();
            CompileStatement(statement.Statement);
            RestoreIndent(PrevIndent);

            WriteLine("}");

            if (statement.Else == null) return;

            WriteLine("else");
            WriteLine("{");

            Indent();
            CompileStatement(statement.Else.Statement);
            RestoreIndent(PrevIndent);

            WriteLine("}");
        }

        override protected void CompileLocalDeclarationStatement(LocalDeclarationStatementSyntax statement)
        {
            CompileVariableDeclaration(statement.Declaration);
        }

        override protected void CompileConditionalExpression(ConditionalExpressionSyntax conditional)
        {
            CompileExpression(conditional.Condition);
            Write("{0}?{0}", Space);
            CompileExpression(conditional.WhenTrue);
            Write("{0}:{0}", Space);
            CompileExpression(conditional.WhenFalse);
        }

        override protected void CompileObjectCreationExpression(ObjectCreationExpressionSyntax creation)
        {
            CompileExpression(creation.Type);

            Write("(");
            CompileArgumentList(creation.ArgumentList);
            Write(")");
        }

        protected override void CompilePrefixUnaryExpression(PrefixUnaryExpressionSyntax expression)
        {
            string op = expression.OperatorToken.ValueText;
            switch (op)
            {
                case "!":
                case "-":
                    Write(expression.OperatorToken);
                    Write("(");
                    CompileExpression(expression.Operand);
                    Write(")");
                    break;
                
                default:
                    Write("ERROR(Unsupported unary expression: {0})", expression);
                    break;
            }
        }

        override protected void CompileVariableDeclaration(VariableDeclarationSyntax declaration)
        {
            BeginLine();

            CompileExpression(declaration.Type);
            Write(" ");

            var last = declaration.Variables.Last();
            foreach (var variable in declaration.Variables)
            {
                CompileVariableDeclarator(variable, declaration.Type);
                Write(variable == last ? ";" : "," + Space);
            }

            EndLine();
        }

        override protected void CompileVariableDeclarator(VariableDeclaratorSyntax declarator, TypeSyntax type)
        {
            Write(declarator.Identifier);

            if (declarator.Initializer == null)
            {
                CompileDefaultInitialization(declarator, type);
            }
            else
            {
                CompileEqualsValueClause(declarator.Initializer);
            }
        }

        virtual protected void CompileDefaultInitialization(VariableDeclaratorSyntax declarator, TypeSyntax type)
        {
        }

        override protected void CompileEqualsValueClause(EqualsValueClauseSyntax clause)
        {
            Write("{0}={0}", Space);
            CompileExpression(clause.Value);
        }

        override protected void CompileCastExpression(CastExpressionSyntax expression)
        {
            try
            {
                // If we are typecasting to what the type is already being mapped to, then we can discard the cast.
                if (TranslationLookup.SymbolMap[GetSymbol(expression.Type)].Translation == TranslationLookup.SymbolMap[GetType(GetSymbol(expression.Expression))].Translation)
                {
                    CompileExpression(expression.Expression);
                    return;
                }
            }
            catch
            {
            }

            EncloseInParanthesis(expression.Type);

            CompileExpression(expression.Expression);
        }

        override protected void CompileParenthesizedExpression(ParenthesizedExpressionSyntax expression)
        {
            EncloseInParanthesis(expression.Expression);
        }

        virtual protected void EncloseInParanthesis(ExpressionSyntax expression)
        {
            if (expression is ParenthesizedExpressionSyntax)
            {
                CompileExpression(expression);
            }
            else
            {
                Write("(");
                CompileExpression(expression);
                Write(")");
            }
        }

        override protected void CompileArgumentList(ArgumentListSyntax list)
        {
            var args = list.Arguments;

            // If there is a sampler paramter we need to pass in the VertexToPixel variable.
            if (args.Count > 0 && args.Any(arg => IsSampler(arg)))
            {
                Write(VertexToPixelVar + Comma);
            }

            // Write each argument
            foreach (var argument in args)
            {
                // If an argument is a sampler, we need to pass in the size and dxdy vectors.
                if (IsSampler(argument))
                {
                    // We can only pass in the extra information if the sampler is a variable, and not an expression.
                    var identifier = argument.Expression as IdentifierNameSyntax;
                    if (null != identifier)
                    {
                        CompileIdentifierName(identifier);
                        Write(Comma);

                        CompileIdentifierName(identifier);
                        Write("_");
                        Write(Sampler.SizeSuffix);
                        Write(Comma);

                        CompileIdentifierName(identifier);
                        Write("_");
                        Write(Sampler.DxDySuffix);
                    }
                    else
                    {
                        throw new Exception("Sampler variables cannot be passed as expressions! It must be passed as a variable name only!");
                    }
                }
                else
                {
                    CompileExpression(argument.Expression);
                }
                
                Write(argument == args.Last() ? string.Empty : Comma);
            }

        }

        override protected void CompileBlock(BlockSyntax block)
        {
            foreach (var statement in block.Statements)
            {
                CompileStatement(statement);
            }
        }

        override protected void CompileExpressionStatement(ExpressionStatementSyntax statement)
        {
            BeginLine();
            CompileExpression(statement.Expression);
            EndLine(";");
        }

        override protected void CompileReturnStatement(ReturnStatementSyntax statement)
        {
            BeginLine("return ");
            CompileExpression(statement.Expression);
            EndLine(";");
        }

        protected static bool IsAssignment(BinaryExpressionSyntax expression)
        {
            var kind = expression.Kind;
            switch (kind)
            {
                case SyntaxKind.AssignExpression:
                case SyntaxKind.AddAssignExpression:
                case SyntaxKind.AndAssignExpression:
                case SyntaxKind.DivideAssignExpression:
                case SyntaxKind.ExclusiveOrAssignExpression:
                case SyntaxKind.LeftShiftAssignExpression:
                case SyntaxKind.ModuloAssignExpression:
                case SyntaxKind.MultiplyAssignExpression:
                case SyntaxKind.OrAssignExpression:
                case SyntaxKind.RightShiftAssignExpression:
                case SyntaxKind.SubtractAssignExpression:
                    return true;
                default: return false;
            }
        }

        protected bool CompilingLeftSideOfAssignment = false;

        override protected void CompileBinaryExpression(BinaryExpressionSyntax expression)
        {
            if (expression.OperatorToken.ValueText == "==")
            {
                Write("abs(");
                if (IsAssignment(expression)) CompilingLeftSideOfAssignment = true;
                CompileExpression(expression.Left);
                if (IsAssignment(expression)) CompilingLeftSideOfAssignment = false;
                Write("{0}-{0}", Space);
                CompileExpression(expression.Right);
                Write(") < .001");
            }
            else
            {
                if (IsAssignment(expression)) CompilingLeftSideOfAssignment = true;
                CompileExpression(expression.Left);
                if (IsAssignment(expression)) CompilingLeftSideOfAssignment = false;

                Write("{1}{0}{1}", expression.OperatorToken, Space);

                CompileExpression(expression.Right);
            }
        }
    }
}
