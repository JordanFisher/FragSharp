﻿using System;
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
                var predefined = type as PredefinedTypeSyntax;
                if (null != predefined)
                {
                    if (predefined.Keyword.ToString() == "void")
                    {
                        Write("void");
                    }
                }
                else
                {
                    Write("ERROR(Unsupported type : {0})", type);
                }
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
            else
            {
                Write("ERROR(Unsupported Literal : {0})", value);
            }
        }

        override protected void CompileElementAccessExpression(ElementAccessExpressionSyntax expression)
        {
            var argument = expression.ArgumentList.Arguments[0];

            var symbol = GetSymbol(argument);
            var type = GetType(symbol);

            if (type.Name == "vec2")
            {
                Write("tex2D(");
                CompileExpression(expression.Expression);
                Write(Comma);
                CompileExpression(argument.Expression);
                Write(")");
            }
            else if (type.Name == "RelativeIndex")
            {
                // Without .5,.5 shift
                Write("tex2D(");
                CompileExpression(expression.Expression);
                Write(Comma);
                Write("psin.TexCoords{0}+{0}(", Space);
                CompileExpression(argument.Expression);
                Write("){0}*{0}", Space);
                //CompileExpression(expression.Expression);
                //Write("_d)", Space);
                Write("float2(1.0 / 1024.0, 1.0 / 1024.0))");

                // With .5,.5 shift
                //Write("tex2D(");
                //CompileExpression(expression.Expression);
                //Write(Comma);
                //Write("psin.TexCoords{0}+{0}(float2(.5,.5){0}+{0}(", Space);
                //CompileExpression(argument.Expression);
                //Write(")){0}*{0}", Space);
                ////CompileExpression(expression.Expression);
                ////Write("_d)", Space);
                //Write("float2(1.0 / 1024.0, 1.0 / 1024.0))");
            }
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
        
        protected Dictionary<Symbol, string> LocalSymbolMap = new Dictionary<Symbol,string>();
        protected void UseLocalSymbolMap(Dictionary<Symbol, string> map)
        {
            LocalSymbolMap = map;
        }

        override protected void CompileIdentifierName(IdentifierNameSyntax syntax)
        {
            var info = GetModel(syntax).GetSymbolInfo(syntax);

            var symbol = info.Symbol;
            if (symbol != null)
            {
                if (LocalSymbolMap.ContainsKey(symbol))
                {
                    var translation = LocalSymbolMap[symbol];
                    Write(translation);
                }
                else if (symbol is LocalSymbol || symbol is ParameterSymbol)
                {
                    string name = syntax.Identifier.ValueText;
                    if (symbol is ParameterSymbol) name = FunctionParameterPrefix + name;

                    Write(name);
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
            //var identifier = declarator.Identifier;

            Write("=");
            Write(Space);
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

        override protected string VertexToPixelVar { get { return "psin"; } }
        override protected string VertexToPixelType { get { return "VertexToPixel"; } }
        override protected string VertexToPixelDecl { get { return VertexToPixelType + " " + VertexToPixelVar; } }
    }
}
