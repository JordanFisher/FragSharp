﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace FragSharp
{
    public class ShaderCompilation
    {
        public string Code, Boilerplate;
        public ShaderCompilation(string Code, string Boilerplate) { this.Code = Code; this.Boilerplate = Boilerplate; }
    }

    internal class HlslShaderWriter : HlslWriter
    {
        public HlslShaderWriter(Dictionary<SyntaxTree, SemanticModel> models, Compilation compilation)
            : base(models, compilation)
        {
        }

        int SamplerNumber = 0;

        enum Compiling { None, VertexMethod, FragmentMethod };
        Compiling CurrentMethod = Compiling.None;

        public ShaderCompilation CompileShader(NamedTypeSymbol Symbol, MethodDeclarationSyntax vertex_method, MethodDeclarationSyntax fragment_method)
        {
            ClearString();
            
            // Declare samplers and other relevant structures needed for the Fragment Shader
            Write(SpaceFormat(FileBegin));
            EndLine();
            
            WriteLine();

            CompileVertexSignature(vertex_method);
            EndLine();

            CompileFragmentSignature(fragment_method);
            EndLine();

            // Referenced methods
            Write(SpaceFormat(ReferencedMethodsPreamble));
            EndLine();
            Write("<$0$>"); // This is where we will insert referenced methods.
            
            WriteLine();

            // Vertex Shader method
            CurrentMethod = Compiling.VertexMethod;
            Write(SpaceFormat(VertexShaderBegin));
            EndLine();
            var PrevIndent = Indent();
            FunctionParameterPrefix = VertexShaderParameterPrefix;
            CompileStatement(vertex_method.Body);
            RestoreIndent(PrevIndent);
            Write(SpaceFormat(VertexShaderEnd));
            EndLine();

            WriteLine();

            // Fragment Shader method
            CurrentMethod = Compiling.FragmentMethod;
            Write(SpaceFormat(FragmentShaderBegin));
            EndLine();
            PrevIndent = Indent();
            FunctionParameterPrefix = FragmentShaderParameterPrefix;
            CompileStatement(fragment_method.Body);
            RestoreIndent(PrevIndent);
            Write(SpaceFormat(FragmentShaderEnd));
            EndLine();

            WriteLine();

            Write(SpaceFormat(FileEnd));

            // We must wait until after compiling the shader to know which methods that shader references.
            string methods = GetReferencedMethods();
            
            // Now get the full string written so far and insert the referenced methods.
            string fragment = GetString();
            fragment = SpecialFormat(fragment, methods);

            // Create the C# boilerplate that provides links
            string boilerplate = CreateBoilerplate(Symbol);

            return new ShaderCompilation(fragment, boilerplate);
        }

        string CreateBoilerplate(NamedTypeSymbol symbol)
        {
            ClearString();

            WriteLine("namespace {0}", symbol.ContainingNamespace);
            WriteLine("{");

            var PrevIndent1 = Indent();

            WriteBoilerplateClass(symbol);

            RestoreIndent(PrevIndent1);

            WriteLine("}");
            WriteLine();

            return GetString();
        }

        void WriteBoilerplateClass(NamedTypeSymbol symbol)
        {
            WriteLine("public partial class {0}", symbol.Name);
            WriteLine("{");

            var PrevIndent = Indent();

            WriteLine("public static Effect CompiledEffect;");
            WriteLine();

            WriteBoilerplateUseFuncSignature(true);
            WriteBoilerplateUseFuncOverload();
            WriteBoilerplateUseFuncSignature(false);
            WriteBoilerplateUseFunc();

            RestoreIndent(PrevIndent);

            WriteLine("}");    
        }

        void WriteBoilerplateUseFuncSignature(bool WithOutputParam)
        {
            BeginLine("public static void Use(");

            foreach (var param in Params)
            {
                Write("{0} {1}", param.TypeName, param.Name);

                if (WithOutputParam || param != Params.Last())
                {
                    Write(",{0}", Space);
                }
            }

            if (WithOutputParam)
            {
                Write("RenderTarget2D Output");
            }

            EndLine(")");
        }

        void WriteBoilerplateUseFuncOverload()
        {
            WriteLine("{");
            var PrevIndent = Indent();

            BeginLine("Use(");

            foreach (var param in Params)
            {
                Write(param.Name);

                if (param != Params.Last())
                {
                    Write(",{0}", Space);
                }
            }

            EndLine(");");

            RestoreIndent(PrevIndent);
            WriteLine("}");    
        }

        void WriteBoilerplateUseFunc()
        {
            WriteLine("{");
            var PrevIndent = Indent();

            foreach (var param in Params)
            {
                WriteLine("CompiledEffect.Parameters[\"{0}\"].SetValue(FragSharp.Marshal({0}));", param.Name);
            }

            WriteLine("CompiledEffect.CurrentTechnique.Passes[0].Apply();");

            RestoreIndent(PrevIndent);
            WriteLine("}");
        }

        override protected void CompileReturnStatement(ReturnStatementSyntax statement)
        {
            if (CurrentMethod == Compiling.FragmentMethod)
            {
                BeginLine("__FinalOutput.Color{0}={0}", Space);
                CompileExpression(statement.Expression);
                EndLine(";");

                WriteLine("return __FinalOutput;");
            }
            else
            {
                base.CompileReturnStatement(statement);
            }
        }

        string SpaceFormat(string s)
        {
            return string.Format(s, Tab, LineBreak);
        }


        class Param
        {
            public enum ParamType { VertexParam, FragmentParam };

            public string TypeName, Name, MappedName;
            public ParamType Type;

            public Param(string TypeName, string Name, string MappedName, ParamType Type)
            {
                this.TypeName = TypeName;
                this.Name = Name;
                this.MappedName = MappedName;
                this.Type = Type;
            }
        }

        List<Param> Params = new List<Param>();

        void CompileVertexSignature(MethodDeclarationSyntax method)
        {
            var ParameterList = method.ParameterList.Parameters;
            if (ParameterList.Count == 0) return;

            var first = ParameterList.First();
            var last = ParameterList.Last();
            foreach (var parameter in ParameterList)
            {
                if (parameter == first) continue;

                CompileVertexParameter(parameter);
                EndLine();
            }
        }

        void CompileFragmentSignature(MethodDeclarationSyntax method)
        {
            var ParameterList = method.ParameterList.Parameters;
            if (ParameterList.Count == 0) return;

            var first = ParameterList.First();
            var last  = ParameterList.Last();
            foreach (var parameter in ParameterList)
            {
                if (parameter == first) continue;

                CompileFragmentParameter(parameter);
                EndLine();

                if (parameter != last)
                {
                    WriteLine();
                }
            }
        }

        const string VertexShaderParameterPrefix = "vs_param_";
        const string FragmentShaderParameterPrefix = "fs_param_";

        void CompileVertexParameter(ParameterSyntax parameter)
        {
            var info = GetModel(parameter).GetSymbolInfo(parameter.Type);

            var symbol = info.Symbol as TypeSymbol;
            if (symbol != null)
            {
                var translation_info = TranslationLookup.RecursiveLookup(symbol);
                if (translation_info.Translation != null)
                {
                    if (translation_info.Translation == "sampler")
                    {
                        Write("ERROR(Samplers not suported in vertex shaders : {0})", parameter);
                    }
                    else
                    {
                        string type = translation_info.Translation;
                        string name = parameter.Identifier.ValueText;
                        string mapped_name = VertexShaderParameterPrefix + name;

                        Write(type);
                        Write(" ");
                        Write(mapped_name);
                        Write(";");

                        Params.Add(new Param(parameter.Type.ToString(), name, mapped_name, Param.ParamType.VertexParam));
                    }
                }
            }
        }

        void CompileFragmentParameter(ParameterSyntax parameter)
        {
            var info = GetModel(parameter).GetSymbolInfo(parameter.Type);

            var symbol = info.Symbol as TypeSymbol;
            if (symbol != null)
            {
                var translation_info = TranslationLookup.RecursiveLookup(symbol);
                if (translation_info.Translation != null)
                {
                    if (translation_info.Translation == "sampler")
                    {
                        CompileSamplerParameter(parameter);
                    }
                    else
                    {
                        string type = translation_info.Translation;
                        string name = parameter.Identifier.ValueText;
                        string mapped_name = FragmentShaderParameterPrefix + name;

                        Write(type);
                        Write(" ");
                        Write(name);
                        Write(";");

                        Params.Add(new Param(parameter.Type.ToString(), name, mapped_name, Param.ParamType.FragmentParam));
                    }
                }
            }
        }

        void CompileSamplerParameter(ParameterSyntax parameter)
        {
            SamplerNumber++;

            string name = parameter.Identifier.ValueText;
            Write(SamplerTemplate, Tab, SamplerNumber, name);

            Params.Add(new Param("Texture2D", name, name, Param.ParamType.FragmentParam));
        }

const string SamplerTemplate =
@"// Texture Sampler for {2}, using register location {1}
Texture {2}_Texture;
sampler {2} : register(s{1}) = sampler_state
{{
{0}texture   = <{2}_Texture>;
{0}MipFilter = Point;
{0}MagFilter = Point;
{0}MinFilter = Point;
{0}AddressU  = Wrap;
{0}AddressV  = Wrap;
}};";

const string ReferencedMethodsPreamble =
@"// The following methods are included because they are referenced by the fragment shader.";

const string VertexShaderBegin =
@"// Compiled vertex shader
VertexToPixel StandardVertexShader( float2 inPos : POSITION0, float2 inTexCoords : TEXCOORD0, float4 inColor : COLOR0)
{{";

const string VertexShaderEnd =
@"}}";

const string FragmentShaderBegin = 
@"// Compiled fragment shader
PixelToFrame FragmentShader(VertexToPixel psin)
{{
{0}PixelToFrame __FinalOutput = (PixelToFrame)0;";

const string FragmentShaderEnd =
@"}}";

const string FileBegin =
@"// This file was auto-generated by FragSharp. It will be regenerated on the next compilation.
// Manual changes made will not persist and may cause incorrect behavior between compilations.

// Vertex shader data structure definition
struct VertexToPixel
{{
{0}float4 Position   : POSITION0;
{0}float4 Color      : COLOR0;
{0}float2 TexCoords  : TEXCOORD0;
{0}float2 Position2D : TEXCOORD2;
}};

// Fragment shader data structure definition
struct PixelToFrame
{{
{0}float4 Color      : COLOR0;
}};";

const string FileEnd =
@"// Shader compilation
technique Simplest
{{
{0}pass Pass0
{0}{{
{0}{0}VertexShader = compile VERTEX_SHADER StandardVertexShader();
{0}{0}PixelShader = compile PIXEL_SHADER FragmentShader();
{0}}}
}}";

public const string BoilerFileBegin =
@"// This file was auto-generated by FragSharp. It will be regenerated on the next compilation.
// Manual changes made will not persist and may cause incorrect behavior between compilations.

using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using FragSharpFramework;";

public const string BoilerBeginInitializer = 
@"namespace FragSharpFramework.Boilerplate
{{
{0}public class _
{0}{{
{0}{0}public static void Initialize(ContentManager Content)
{0}{0}{{";

public const string BoilerEndInitializer =
@"{0}{0}}}
{0}}}
}}";
    }
}
