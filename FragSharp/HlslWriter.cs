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
            else if (value is bool)
            {
                Write((bool)value ? "true" : "false");
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

            //if (expression.ToFullString().Contains("Far")) Console.Write("!");

            if (type != null && type.Name == "vec2")
            {
                Write("tex2D(");
                CompileExpression(expression.Expression);
                Write(Comma);
                CompileExpression(argument.Expression);
                Write(")");
            }
            else if (type != null && type.Name == "RelativeIndex")
            {
                // Without .5,.5 shift
                Write("tex2D(");
                CompileExpression(expression.Expression);
                Write(Comma);
                Write("psin.TexCoords{0}+{0}(", Space);
                CompileExpression(argument.Expression);
                Write("){0}*{0}", Space);
                CompileExpression(expression.Expression);
                Write("_{0})", Sampler.DxDySuffix);

                // With .5,.5 shift. This may be needed on some architectures due to rounding/interpolation issues.
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
            else
            {
                //Write("tex2D(");
                //CompileExpression(expression.Expression);
                //Write(Comma);
                //Write("float2(2,1) * float2(1,1)/16.0)");

                // Assume form [i, j]
                Write("tex2D(");
                CompileExpression(expression.Expression);
                Write(Comma);
                Write("float2(", Space);

                var arg1 = expression.ArgumentList.Arguments[0];
                CompileExpression(arg1.Expression);
                Write("+.5,.5+" + Space);
                var arg2 = expression.ArgumentList.Arguments[1];
                CompileExpression(arg2.Expression);

                //Write(") * float2(1,1)/16.0)");
                Write("){0}*{0}", Space);
                CompileExpression(expression.Expression);
                Write("_{0})", Sampler.DxDySuffix);
            }
        }

        int hex(char c)
        {
            int dec = (int)c - (int)'0';
            if (dec <= 9 && dec >= 0) return dec;
            int hex = (int)c - (int)'a';
            if (hex < 6  && hex >= 0) return hex + 10;
            
            hex = (int)c - (int)'A';
            if (hex < 6  && hex >= 0) return hex + 10;

            throw new Exception("Improper hexadecimal literal. Should be of form 0x8a7b81 or 0x8A7B81.");
        }


        string HexToVec4(string s)
        {
            float r = (16 * hex(s[2]) + hex(s[3])) / 255f;
            float g = (16 * hex(s[4]) + hex(s[5])) / 255f;
            float b = (16 * hex(s[6]) + hex(s[7])) / 255f;

            return string.Format("{0}, {1}, {2}", r, g, b);
        }

        override protected void CompileInvocationExpression(InvocationExpressionSyntax expression)
        {
            var symbol = GetSymbol(expression.Expression);

            if (symbol != null)
            {
                var special = symbol.GetAttribute("Special");

                if (special != null)
                {
                    var name = (Special)special.ConstructorArguments.First().Value;

                    switch (name)
                    {
                        case Special.rgba_hex:
                            // If the funciton has a special compilation, do that special compilation.
                            var hex_literal = expression.ArgumentList.Arguments[0].Expression as LiteralExpressionSyntax;
                            var float_literal = expression.ArgumentList.Arguments[1].Expression as LiteralExpressionSyntax;
                            //Write("float4({0}, {1})", HexToVec4(hex_literal.ToString()), float_literal.ToString());
                            
                            Write("float4(");
                            Write(HexToVec4(hex_literal.ToString()));
                            Write(", ");
                            CompileLiteralExpression(float_literal);
                            Write(")");
                            
                            break;

                        case Special.rgb_hex:
                            // If the funciton has a special compilation, do that special compilation.
                            var _hex_literal = expression.ArgumentList.Arguments[0].Expression as LiteralExpressionSyntax;
                            Write("float4({0}, 1.0)", HexToVec4(_hex_literal.ToString()));
                            break;
                    }
                }
                else if (TranslationLookup.SymbolMap.ContainsKey(symbol))
                {
                    // If the function has a tranlsation, use that tranlsation
                    var translation_info = TranslationLookup.SymbolMap[symbol];
                    Write(translation_info.Translation);

                    Write("(");
                    CompileArgumentList(expression.ArgumentList, false);
                    Write(")");
                }
                else
                {
                    // Otherwise compile the function and note that we are using it.
                    var writer = new HlslWriter(this);
                    var result = writer.CompileMethod(symbol);

                    ReferencedMethods.AddUnique(SymbolCompilation[symbol].ReferencedMethods);
                    ReferencedMethods.AddUnique(symbol);

                    ReferencedForeignVars.AddUnique(writer.ReferencedForeignVars);

                    CompileExpression(expression.Expression);

                    Write("(");
                    CompileArgumentList(expression.ArgumentList, result.UsesSampler);
                    Write(")");
                }
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
                        var method = symbol as MethodSymbol;
                        if (null != method)
                            WriteFullMethodName(method);
                        else
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
                                //Write("ERROR(Non-local symbol : {0})", syntax);

                                string name = syntax.Identifier.ValueText;
                                if (IsSamplerType(GetType(symbol)))
                                    name = "fs_param_" + name;
                                else
                                    name = "foreign_" + name;

                                Write(name);
                                ReferencedForeignVars.AddUnique(symbol);
                            }
                        }
                    }
                }
            }
        }

        override protected void CompileDefaultInitialization(VariableDeclaratorSyntax declarator, TypeSyntax type)
        {
            Write("=");
            Write(Space);
            Write("(");
            CompileExpression(type);
            Write(")0");
        }

        override protected string VertexToPixelVar { get { return "psin"; } }
        override protected string VertexToPixelType { get { return "VertexToPixel"; } }
        override protected string VertexToPixelDecl { get { return VertexToPixelType + " " + VertexToPixelVar; } }
    }
}
