using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace FragSharp
{
    internal abstract class AbstractCodeWriter : IDisposable
    {
        public AbstractCodeWriter(Dictionary<SyntaxTree, SemanticModel> models, Compilation compilation)
        {
            this.models            = models;
            this.compilation       = compilation;

            this.SymbolCompilation = new Dictionary<Symbol, CompiledMethod>();
        }

        public AbstractCodeWriter(AbstractCodeWriter writer)
        {
            this.models            = writer.models;
            this.compilation       = writer.compilation;
            this.SymbolCompilation = writer.SymbolCompilation;
        }

        protected Dictionary<SyntaxTree, SemanticModel> models;
        protected Compilation compilation;

        protected SemanticModel GetModel(SyntaxNode expression)
        {
            return models[expression.SyntaxTree];
        }

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

        abstract protected void CompileLiteral(object literal);
        virtual protected void CompileLiteralExpression(LiteralExpressionSyntax literal)
        {
            var get = GetModel(literal).GetConstantValue(literal);

            if (get.HasValue)
            {
                CompileLiteral(get.Value);
            }
            else
            {
                Write("ERROR(Improper Literal : {0})", literal);
            }
        }

        protected void CompileMemberAccessExpression(MemberAccessExpressionSyntax expression)
        {
            var member = GetSymbol(expression.Name);

            // If this member has a translation
            if (TranslationLookup.SymbolMap.ContainsKey(member))
            {
                var translation_info = TranslationLookup.SymbolMap[member];

                if (translation_info.TranslationType == TranslationType.ReplaceMember)
                {
                    CompileExpression(expression.Expression);
                    Write(".");

                    Write(translation_info.Translation);
                }
                else if (translation_info.TranslationType == TranslationType.ReplaceExpression)
                {
                    Write(translation_info.Translation);
                }
                else if (translation_info.TranslationType == TranslationType.UnderscoreAppend)
                {
                    CompileExpression(expression.Expression);
                    Write("_");

                    Write(translation_info.Translation);
                }
            }
            else
            {
                var const_val = GetModel(expression.Name).GetConstantValue(expression.Name);
                if (const_val.HasValue)
                {
                    CompileLiteral(const_val.Value);
                }
                else if (ReferencedMethods.Contains(member) && member is MethodSymbol)
                {
                    WriteFullMethodName((MethodSymbol)member);
                }
                else
                {
                    Write("ERROR(MemberAccess: {0})", expression);
                }
            }
        }

        protected void WriteFullMethodName(MethodSymbol method)
        {
            string name = method.ContainingNamespace.Name + "__" + method.ContainingType.Name + "__" + method.Name;
            Write(name.Replace(".", "__"));
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
                return Minify ? string.Empty : "    ";
            }
        }

        public string Comma
        {
            get
            {
                return ',' + Space;
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
            writer = new StringWriter();
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

        virtual public void CompileExpression(ExpressionSyntax expression)
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
            else if (expression is PrefixUnaryExpressionSyntax)    CompilePrefixUnaryExpression(   (PrefixUnaryExpressionSyntax)   expression);
            else Write("expression " + expression.GetType().Name);
        }

        abstract protected void CompileBinaryExpression(BinaryExpressionSyntax expression);
        abstract protected void CompileIdentifierName(IdentifierNameSyntax syntax);
        abstract protected void CompileElementAccessExpression(ElementAccessExpressionSyntax expression);
        abstract protected void CompileInvocationExpression(InvocationExpressionSyntax expression);
        abstract protected void CompileCastExpression(CastExpressionSyntax expression);
        abstract protected void CompileParenthesizedExpression(ParenthesizedExpressionSyntax expression);
        abstract protected void CompileType(TypeSyntax type);
        abstract protected void CompileConditionalExpression(ConditionalExpressionSyntax conditional);
        abstract protected void CompileObjectCreationExpression(ObjectCreationExpressionSyntax creation);
        abstract protected void CompilePrefixUnaryExpression(PrefixUnaryExpressionSyntax expression);

        abstract protected void CompileVariableDeclaration(VariableDeclarationSyntax declaration);
        abstract protected void CompileVariableDeclarator(VariableDeclaratorSyntax declarator, TypeSyntax type);
        abstract protected void CompileEqualsValueClause(EqualsValueClauseSyntax clause);
        abstract protected void CompileArgumentList(ArgumentListSyntax list);

        abstract protected string VertexToPixelVar { get; }
        abstract protected string VertexToPixelType { get; }
        abstract protected string VertexToPixelDecl { get; }

        protected bool IsSampler(SyntaxNode param) { return IsSamplerType(GetType(GetSymbol(param))); }

        protected bool IsSamplerType(TypeSymbol symbol)
        {
            if (symbol == null) return false;

            var info = TranslationLookup.RecursiveLookup(symbol);
            if (info.Translation == "sampler") return true;

            return false;
            //return symbol != null && TranslationLookup.SymbolMap.ContainsKey(symbol) && TranslationLookup.SymbolMap[symbol].Translation == "sampler";
        }

        protected TypeSymbol GetType(Symbol symbol)
        {
            if (symbol == null) return null;

            if      (symbol is LocalSymbol)     return ((LocalSymbol)    symbol).Type;
            else if (symbol is ParameterSymbol) return ((ParameterSymbol)symbol).Type;
            else if (symbol is FieldSymbol)     return ((FieldSymbol)    symbol).Type;
            else if (symbol is PropertySymbol)  return ((PropertySymbol) symbol).Type;
            else if (symbol is MethodSymbol)    return ((MethodSymbol)   symbol).ReturnType;
            
            else throw new Exception("Symbol has no type!");
        }

        protected Symbol          GetSymbol(ArgumentSyntax  syntax) { return syntax == null ? null : GetModel(syntax).GetSymbolInfo(syntax.Expression).Symbol; }
        protected ParameterSymbol GetSymbol(ParameterSyntax syntax) { return syntax == null ? null : GetModel(syntax).GetDeclaredSymbol(syntax); }

        protected Symbol GetSymbol(SyntaxNode syntax)
        {
            if (syntax == null) return null;

            var m = GetModel(syntax);

            if      (syntax is ArgumentSyntax)               return m.GetSymbolInfo    (((ArgumentSyntax)syntax).Expression).Symbol;
            else if (syntax is ParameterSyntax)              return m.GetDeclaredSymbol((ParameterSyntax)syntax);
            else if (syntax is ExpressionSyntax)             return m.GetSymbolInfo((ExpressionSyntax)syntax).Symbol;
            else if (syntax is BaseMethodDeclarationSyntax)  return m.GetDeclaredSymbol((BaseMethodDeclarationSyntax)syntax);
            else throw new Exception("Code stub! Fix me please!");
        }

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

        public void Dispose()
        {
            if (writer != null)
                writer.Dispose();
        }
    }
}
