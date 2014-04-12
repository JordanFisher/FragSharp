using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace FragSharp
{
    internal class HlslWriter : CStyleWriter
    {
        public HlslWriter(Dictionary<SyntaxTree, SemanticModel> models, Compilation compilation)
            : base(models, compilation)
        {
        }

        public HlslWriter(HlslWriter writer)
            : base(writer)
        {
        }

        //const string _0 = "0/255.0", _1 = "1/255.0", _2 = "2/255.0", _3 = "3/255.0", _4 = "4/255.0", _5 = "5/255.0", _6 = "6/255.0", _7 = "7/255.0", _8 = "8/255.0", _9 = "9/255.0", _10 = "10/255.0", _11 = "11/255.0", _12 = "12/255.0";

        override protected void CompileType(TypeSyntax type)
        {
            var info = GetModel(type).GetSymbolInfo(type);

            var symbol = info.Symbol;
            if (symbol != null && TranslationLookup.SymbolMap.ContainsKey(symbol))
            {
                Write(TranslationLookup.SymbolMap[symbol].Translation);
            }
            else
            {
                Write("ERROR(Unsupported type : {0})", type);
            }
        }

        override protected void CompileLiteral(object value)
        {
            if (value is int)
            {
                Write(value);
            }
            else if (value is float || value is double)
            {
                string val = value.ToString();
                if (!val.Contains('.')) val += ".0";
                Write(val);
            }
            else Write("ERROR(Unsupported Literal : {0})", value);
        }

        override protected void CompileElementAccessExpression(ElementAccessExpressionSyntax expression)
        {
            Write("tex2D(");
            CompileExpression(expression.Expression);
            Write(",{0}", Space);
            Write("PSIn.TexCoords{0}+{0}(float2(.5,.5){0}+{0}(", Space);
            CompileExpression(expression.ArgumentList.Arguments[0].Expression);
            Write("){0}*{0}float2(dx,{0}dy)))", Space);
        }

        override protected void CompileInvocationExpression(InvocationExpressionSyntax expression)
        {
            var info = GetModel(expression.Expression).GetSymbolInfo(expression.Expression);

            var symbol = info.Symbol;
            if (symbol != null)
            {
                // If the function has a tranlsation, use that tranlsation
                if (TranslationLookup.SymbolMap.ContainsKey(symbol))
                {
                    var translation_info = TranslationLookup.SymbolMap[symbol];
                    Write(translation_info.Translation);
                }
                else
                {
                    // Otherwise compile the function and note that we are using it.
                    var writer = new HlslWriter(this);
                    writer.CompileMethod(info.Symbol);

                    ReferencedMethods.AddUnique(SymbolCompilation[info.Symbol].ReferencedMethods);
                    ReferencedMethods.AddUnique(info.Symbol);

                    CompileExpression(expression.Expression);
                }

                Write("(");
                CompileArgumentList(expression.ArgumentList);
                Write(")");
            }
            else
            {
                Write("ERROR(Unknown function : {0})", expression);
            }
        }

        protected string FunctionParameterPrefix = string.Empty;
        override protected void CompileIdentifierName(IdentifierNameSyntax syntax)
        {
            var info = GetModel(syntax).GetSymbolInfo(syntax);

            var symbol = info.Symbol;
            if (symbol != null)
            {
                if (symbol is LocalSymbol)
                {
                    Write(syntax.Identifier.ValueText);
                }
                else if (symbol is ParameterSymbol)
                {
                    Write(FunctionParameterPrefix + syntax.Identifier.ValueText);
                }
                else if (TranslationLookup.SymbolMap.ContainsKey(symbol))
                {
                    var translation_info = TranslationLookup.SymbolMap[symbol];

                    Write(translation_info.Translation);
                }
                else
                {
                    if (ReferencedMethods.Contains(symbol))
                    {
                        Write(symbol.Name);
                    }
                    else
                    {
                        var const_val = GetModel(syntax).GetConstantValue(syntax);
                        if (const_val.HasValue)
                        {
                            CompileLiteral(const_val.Value);
                        }
                        else
                        {
                            if (CompilingLeftSideOfAssignment)
                            {
                                Write("ERROR(Non-local assignment : {0})", syntax);
                            }
                            else
                            {
                                Write("ERROR(Non-local symbol : {0})", syntax);
                            }
                        }
                    }
                }
            }
        }

        override protected void CompileDefaultInitialization(VariableDeclaratorSyntax declarator, TypeSyntax type)
        {
            var identifier = declarator.Identifier;
            Write("(");
            CompileExpression(type);
            Write(")0");

            //var info = models[identifier.SyntaxTree].GetDeclaredSymbol(type);

            //var symbol = info.Symbol;

            //if (TranslationLookup.SymbolMap.ContainsKey(symbol))
            //{
            //    Write("({0})0", TranslationLookup.SymbolMap[symbol]);
            //}
            //else
            //{
            //    Write("ERROR(Can't given a default value to this unintialized variable : {0})", declarator);
            //}
        }
    }
}
