using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

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
        public CStyleWriter(SemanticModel model, Compilation compilation)
            : base(model, compilation)
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
            CompileExpression(method.ReturnType);
            Write(" {0}", method.Identifier.ValueText);
            Write("(");

            var last = method.ParameterList.Parameters.Last();
            foreach (var parameter in method.ParameterList.Parameters)
            {
                CompileMethodParameter(parameter);

                if (parameter != last)
                    Write(", ");
            }

            Write(")");
            Write(LineBreak);
        }

        override protected void CompileMethodParameter(ParameterSyntax parameter)
        {
            CompileExpression(parameter.Type);
            Write(" {0}", parameter.Identifier.ValueText);
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

        override protected void CompileVariableDeclaration(VariableDeclarationSyntax declaration)
        {
            BeginLine();

            CompileExpression(declaration.Type);
            Write(" ");

            var last = declaration.Variables.Last();
            foreach (var variable in declaration.Variables)
            {
                CompileVariableDeclarator(variable);
                Write(variable == last ? ";" : "," + Space);
            }

            Write(LineBreak);
        }

        override protected void CompileVariableDeclarator(VariableDeclaratorSyntax declarator)
        {
            Write(declarator.Identifier);
            CompileEqualsValueClause(declarator.Initializer);
        }

        override protected void CompileEqualsValueClause(EqualsValueClauseSyntax clause)
        {
            Write("{0}={0}", Space);
            CompileExpression(clause.Value);
        }

        override protected void CompileCastExpression(CastExpressionSyntax expression)
        {
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
            var last = list.Arguments.Last();
            foreach (var argument in list.Arguments)
            {
                CompileExpression(argument.Expression);
                Write(argument == last ? "" : ",{0}", Space);
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

        override protected void CompileBinaryExpression(BinaryExpressionSyntax expression)
        {
            CompileExpression(expression.Left);
            Write("{1}{0}{1}", expression.OperatorToken, Space);
            CompileExpression(expression.Right);
        }
    }
}
