using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace FragSharp
{
    internal class RoslynHelper
    {
        protected string LastError = "";

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

        protected SemanticModel GetModel(SyntaxNode expression, Dictionary<SyntaxTree, SemanticModel> models)
        {
            return models[expression.SyntaxTree];
        }

        protected Symbol          GetSymbol(ArgumentSyntax  syntax, Dictionary<SyntaxTree, SemanticModel> models) { return syntax == null ? null : GetModel(syntax, models).GetSymbolInfo(syntax.Expression).Symbol; }
        protected ParameterSymbol GetSymbol(ParameterSyntax syntax, Dictionary<SyntaxTree, SemanticModel> models) { return syntax == null ? null : GetModel(syntax, models).GetDeclaredSymbol(syntax); }

        protected Symbol GetSymbol(SyntaxNode syntax, Dictionary<SyntaxTree, SemanticModel> models)
        {
            if (syntax == null) return null;

            var m = GetModel(syntax, models);

            if      (syntax is ArgumentSyntax)               return m.GetSymbolInfo    (((ArgumentSyntax)syntax).Expression).GetSymbolOrSetError(ref LastError);
            else if (syntax is ParameterSyntax)              return m.GetDeclaredSymbol((ParameterSyntax)syntax);
            else if (syntax is ExpressionSyntax)             return m.GetSymbolInfo    ((ExpressionSyntax)syntax).GetSymbolOrSetError(ref LastError);
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
    }
}
