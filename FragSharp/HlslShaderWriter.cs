using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace FragSharp
{
    internal class HlslShaderWriter : HlslWriter
    {
        public HlslShaderWriter(Dictionary<SyntaxTree, SemanticModel> models, Compilation compilation)
            : base(models, compilation)
        {
        }

        int SamplerNumber = 0;

        enum Compiling { None, VertexMethod, FragmentMethod };
        Compiling CurrentMethod = Compiling.None;

        public string CompileShader(MethodDeclarationSyntax vertex_method, MethodDeclarationSyntax fragment_method)
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

            return fragment;
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

                //if (parameter != last)
                //{
                //    WriteLine();
                //}
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
                        // Specialize
                    }
                    else
                    {
                        Write(translation_info.Translation);
                        Write(" ");
                        Write(VertexShaderParameterPrefix + parameter.Identifier);
                        Write(";");
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
                        Write(translation_info.Translation);
                        Write(" ");
                        Write(FragmentShaderParameterPrefix + parameter.Identifier);
                        Write(";");
                    }
                }
            }
        }

        void CompileSamplerParameter(ParameterSyntax parameter)
        {
            SamplerNumber++;

            Write(SamplerTemplate, Tab, SamplerNumber, parameter.Identifier.ValueText);
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

    }
}
