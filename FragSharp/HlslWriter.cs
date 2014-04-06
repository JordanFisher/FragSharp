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
        public HlslWriter(SemanticModel model, Compilation compilation)
            : base(model, compilation)
        {
        }

        public HlslWriter(HlslWriter writer)
            : base(writer)
        {
        }

        const string _0 = "0/255.0", _1 = "1/255.0", _2 = "2/255.0", _3 = "3/255.0", _4 = "4/255.0", _5 = "5/255.0", _6 = "6/255.0", _7 = "7/255.0", _8 = "8/255.0", _9 = "9/255.0", _10 = "10/255.0", _11 = "11/255.0", _12 = "12/255.0";

        Dictionary<string, string> SymbolMap = new Dictionary<string, string>() {
            { "unit" , "float4" },

            { "vec2" , "float2" },
            { "vec3" , "float3" },
            { "vec4" , "float4" },

            { "RelativeIndex" , "float2" },

            { "cos" , "cos" },
            { "sin" , "sin" },

            { "unit.Nothing" , "float4(0,0,0,0)" },

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

            { "TurnRight" , "-1/255.0" },
            { "TurnLeft"  ,  "1/255.0" },


            { "Change.Moved"  , _0 },
            { "Change.Stayed" , _1 },
        };

        Dictionary<string, string> MemberMap = new Dictionary<string, string>() {
            { "x", "x" },
            { "y", "y" },

            { "xy", "xy" },
            
            { "xyz", "xyz" },

            { "r", "r" },
            { "g", "g" },
            { "b", "b" },
            { "a", "a" },

            { "direction", "r" },
            { "change",    "g" },
        };

        override protected void CompileType(TypeSyntax type)
        {
            string name = type.ToString();

            if (SymbolMap.ContainsKey(name))
            {
                Write(SymbolMap[name]);
            }
            else
            {
                Write(name);
            }
        }

        override protected void CompileLiteralExpression(LiteralExpressionSyntax literal)
        {
            var get = model.GetConstantValue(literal);

            if (get.HasValue)
            {
                var val = get.Value;

                if (val is int) Write(val);
                else if (val is float) Write(val);
                else if (val is double) Write(val);
                else Write("ERROR(Unsupported Literal : {0})", val);
            }
            else
            {
                Write("ERROR(Improper Literal : {0})", literal);
            }
        }

        override protected void CompileElementAccessExpression(ElementAccessExpressionSyntax expression)
        {
            //var info = model.GetSymbolInfo(expression.ArgumentList.Arguments[0].Expression);
            //info.Symbol

            Write("tex2D(");
            CompileExpression(expression.Expression);
            Write(",{0}", Space);
            Write("PSIn.TexCoords{0}+{0}(float2(.5,.5){0}+{0}(", Space);
            CompileExpression(expression.ArgumentList.Arguments[0].Expression);
            Write("){0}*{0}float2(dx,{0}dy)))", Space);
        }

        override protected void CompileInvocationExpression(InvocationExpressionSyntax expression)
        {
            if (SymbolMap.ContainsKey(expression.Expression.ToString()))
            {
                Write(SymbolMap[expression.Expression.ToString()]);
            }
            else
            {
                var info = model.GetSymbolInfo(expression.Expression);

                var writer = new HlslWriter(this);
                writer.CompileMethod(info.Symbol);

                ReferencedMethods.AddRange(SymbolCompilation[info.Symbol].ReferencedMethods);
                ReferencedMethods.Add(info.Symbol);

                CompileExpression(expression.Expression);
            }

            Write("(");
            CompileArgumentList(expression.ArgumentList);
            Write(")");
        }

        protected static string CreateRelativeIndex(int i, int j)
        {
            return string.Format("float2({0},{1})", i, j);
        }

        override protected void CompileMemberAccessExpression(MemberAccessExpressionSyntax expression)
        {
            string member = expression.Name.Identifier.ValueText;

            string access = expression.Expression + "." + member;
            if (SymbolMap.ContainsKey(access))
            {
                Write(SymbolMap[access]);
                return;
            }

            if (MemberMap.ContainsKey(member))
            {
                CompileExpression(expression.Expression);
                Write(".");

                var mapped = MemberMap[member];
                Write(mapped);
            }
            else
            {
                Write("ERROR(MemberAccess: {0})", expression);
            }
        }

        override protected void CompileIdentifierName(IdentifierNameSyntax syntax)
        {
            string identifier = syntax.Identifier.ValueText;

            if (SymbolMap.ContainsKey(identifier))
            {
                Write(SymbolMap[identifier]);
            }
            else
            {
                Write(identifier);
            }
        }
    }
}
