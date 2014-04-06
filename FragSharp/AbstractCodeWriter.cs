using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace FragSharp
{
    internal abstract class AbstractCodeWriter
    {
        public AbstractCodeWriter(SemanticModel model, Compilation compilation)
        {
            this.model             = model;
            this.compilation       = compilation;

            this.SymbolCompilation = new Dictionary<Symbol, CompiledMethod>();
        }

        public AbstractCodeWriter(AbstractCodeWriter writer)
        {
            this.model             = writer.model;
            this.compilation       = writer.compilation;
            this.SymbolCompilation = writer.SymbolCompilation;
        }

        protected SemanticModel model;
        protected Compilation compilation;

        protected struct CompiledMethod
        {
            public string Compilation;
            public List<Symbol> ReferencedMethods;

            public CompiledMethod(string Compilation, List<Symbol> ReferencedMethods)
            {
                this.Compilation = Compilation;
                this.ReferencedMethods = ReferencedMethods;
            }
        }

        protected Dictionary<Symbol, CompiledMethod> SymbolCompilation;

        protected List<Symbol> ReferencedMethods = new List<Symbol>();

        protected StringWriter writer = new StringWriter();

        abstract protected CompiledMethod CompileMethod(Symbol symbol);
        abstract protected void CompileMethodSignature(MethodDeclarationSyntax method);
        abstract protected void CompileMethodParameter(ParameterSyntax parameter);

        virtual protected string GetReferencedMethods()
        {
            string methods = string.Empty;

            if (ReferencedMethods.Count == 0)
                return methods;

            var last = ReferencedMethods.Last();
            foreach (var method in ReferencedMethods)
            {
                methods += SymbolCompilation[method].Compilation;

                if (method != last)
                    methods += LineBreak;
            }

            return methods;
        }

        public bool Minify = false;
        
        public string Tab
        {
            get
            {
                return Minify ? string.Empty : "  ";
            }
        }

        public string Space
        {
            get
            {
                return Minify ? string.Empty : " ";
            }
        }

        public string LineBreak
        {
            get
            {
                return Minify ? string.Empty : "\n";
            }
        }

        public string GetString()
        {
            return writer.ToString();
        }

        protected void ClearString()
        {
            writer.Flush();
        }

        protected void Write(object obj)
        {
            writer.Write(obj);
        }

        protected void Write(string str)
        {
            writer.Write(str);
        }

        protected void Write(string str, params object[] arguments)
        {
            writer.Write(str, arguments);
        }

        public string CurrentIndent = string.Empty;

        protected string Indent()
        {
            string hold = CurrentIndent;
            
            CurrentIndent += Tab;
            
            return hold;
        }

        protected void RestoreIndent(string indent)
        {
            CurrentIndent = indent;
        }

        protected void ResetIndent()
        {
            CurrentIndent = string.Empty;
        }

        protected void BeginLine()
        {
            Write(CurrentIndent);
        }

        protected void BeginLine(string str)
        {
            Write(CurrentIndent);
            Write(str);
        }
        
        protected void BeginLine(string str, params object[] arguments)
        {
            Write(CurrentIndent);
            Write(str, arguments);
        }

        protected void EndLine()
        {
            Write(LineBreak);
        }

        protected void EndLine(string str)
        {
            Write(str);
            Write(LineBreak);
        }
        
        protected void EndLine(string str, params object[] arguments)
        {
            Write(str, arguments);
            Write(LineBreak);
        }

        protected void WriteLine()
        {
            Write(LineBreak);
        }

        protected void WriteLine(string str)
        {
            Write(CurrentIndent);
            Write(str);
            Write(LineBreak);
        }

        protected void WriteLine(string str, params object[] arguments)
        {
            Write(CurrentIndent);
            Write(str, arguments);
            Write(LineBreak);
        }

        virtual protected void CompileStatement(StatementSyntax statement)
        {
            if      (statement is IfStatementSyntax)               CompileIfStatement(              (IfStatementSyntax)              statement);
            else if (statement is LocalDeclarationStatementSyntax) CompileLocalDeclarationStatement((LocalDeclarationStatementSyntax)statement);
            else if (statement is BlockSyntax)                     CompileBlock(                    (BlockSyntax)                    statement);
            else if (statement is ExpressionStatementSyntax)       CompileExpressionStatement(      (ExpressionStatementSyntax)      statement);
            else if (statement is ReturnStatementSyntax)           CompileReturnStatement(          (ReturnStatementSyntax)          statement);
            else if (statement is StatementSyntax)                 WriteLine("statement {0}", statement.GetType());
        }

        abstract protected void CompileIfStatement(IfStatementSyntax statement);
        abstract protected void CompileLocalDeclarationStatement(LocalDeclarationStatementSyntax statement);
        abstract protected void CompileBlock(BlockSyntax block);
        abstract protected void CompileExpressionStatement(ExpressionStatementSyntax statement);
        abstract protected void CompileReturnStatement(ReturnStatementSyntax statement);

        virtual protected void CompileExpression(ExpressionSyntax expression)
        {
            if      (expression is BinaryExpressionSyntax)         CompileBinaryExpression(        (BinaryExpressionSyntax)        expression);
            else if (expression is MemberAccessExpressionSyntax)   CompileMemberAccessExpression(  (MemberAccessExpressionSyntax)  expression);
            else if (expression is IdentifierNameSyntax)           CompileIdentifierName(          (IdentifierNameSyntax)          expression);
            else if (expression is ElementAccessExpressionSyntax)  CompileElementAccessExpression( (ElementAccessExpressionSyntax) expression);
            else if (expression is InvocationExpressionSyntax)     CompileInvocationExpression(    (InvocationExpressionSyntax)    expression);
            else if (expression is CastExpressionSyntax)           CompileCastExpression(          (CastExpressionSyntax)          expression);
            else if (expression is ParenthesizedExpressionSyntax)  CompileParenthesizedExpression( (ParenthesizedExpressionSyntax) expression);
            else if (expression is TypeSyntax)                     CompileType(                    (TypeSyntax)                    expression);
            else if (expression is LiteralExpressionSyntax)        CompileLiteralExpression(       (LiteralExpressionSyntax)       expression);
            else if (expression is ConditionalExpressionSyntax)    CompileConditionalExpression(   (ConditionalExpressionSyntax)   expression);
            else if (expression is ObjectCreationExpressionSyntax) CompileObjectCreationExpression((ObjectCreationExpressionSyntax)expression);
            else Write("expression " + expression.GetType().Name);
        }

        abstract protected void CompileBinaryExpression(BinaryExpressionSyntax expression);
        abstract protected void CompileMemberAccessExpression(MemberAccessExpressionSyntax expression);
        abstract protected void CompileIdentifierName(IdentifierNameSyntax syntax);
        abstract protected void CompileElementAccessExpression(ElementAccessExpressionSyntax expression);
        abstract protected void CompileInvocationExpression(InvocationExpressionSyntax expression);
        abstract protected void CompileCastExpression(CastExpressionSyntax expression);
        abstract protected void CompileParenthesizedExpression(ParenthesizedExpressionSyntax expression);
        abstract protected void CompileType(TypeSyntax type);
        abstract protected void CompileLiteralExpression(LiteralExpressionSyntax literal);
        abstract protected void CompileConditionalExpression(ConditionalExpressionSyntax conditional);
        abstract protected void CompileObjectCreationExpression(ObjectCreationExpressionSyntax creation);

        abstract protected void CompileVariableDeclaration(VariableDeclarationSyntax declaration);
        abstract protected void CompileVariableDeclarator(VariableDeclaratorSyntax declarator);
        abstract protected void CompileEqualsValueClause(EqualsValueClauseSyntax clause);
        abstract protected void CompileArgumentList(ArgumentListSyntax list);

        /// <summary>
        /// This is a special string formatting helper function to assist in formatting strings with curly braces inside.
        /// Use <$0$>, <$1$>, <$2$>, ... instead of {0}, {1}, {2}.
        /// </summary>
        /// <param name="format">The string to be formatted.</param>
        /// <param name="arguments">The arguments to interpolate into the string.</param>
        /// <returns></returns>
        protected static string SpecialFormat(string format, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string InterpolationSymbol = "<$" + i.ToString() + "$>";
                format = format.Replace(InterpolationSymbol, args[i].ToString());
            }

            return format;
        }
    }
}
