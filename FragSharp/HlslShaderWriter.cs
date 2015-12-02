﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers.CSharp;

using FragSharpFramework;

namespace FragSharp
{
    public class ShaderCompilation
    {
        public string Code, Boilerplate;
        public ShaderCompilation(string Code, string Boilerplate) { this.Code = Code; this.Boilerplate = Boilerplate; }
    }

    internal class HlslShaderWriter : HlslWriter
    {
        public HlslShaderWriter(Dictionary<SyntaxTree, SemanticModel> models, Compilation compilation, Dictionary<Symbol, string> specialization)
            : base(models, compilation)
        {
            this.specialization = specialization;
        }

        Dictionary<Symbol, string> specialization;

        int SamplerNumber = 0;

        enum Compiling { None, VertexMethod, FragmentMethod };
        Compiling CurrentMethod = Compiling.None;

        public string CompileShader(NamedTypeSymbol Symbol, MethodDeclarationSyntax vertex_method, MethodDeclarationSyntax fragment_method)
        {
            ClearString();
            
            // Declare samplers and other relevant structures needed for the Fragment Shader
            Write(SpaceFormat(FileBegin));
            EndLine();
            
            WriteLine();

            WriteLine(VertexMethodParameters);
            CompileVertexSignature(vertex_method);
            EndLine();

            WriteLine(FragmentMethodParameters);
            var LocalFragmentLookup = CompileFragmentSignature(fragment_method);
            EndLine();

            // Referenced foreign variables
            Write(SpaceFormat(ReferencedForeignVarsPreamble));
            EndLine();
            Write("<$0$>"); // This is where we will insert referenced foreign variables.

            WriteLine();

            // Referenced methods
            Write(SpaceFormat(ReferencedMethodsPreamble));
            EndLine();
            Write("<$1$>"); // This is where we will insert referenced methods.
            
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
            UseLocalSymbolMap(LocalFragmentLookup);

            CurrentMethod = Compiling.FragmentMethod;
            Write(FragmentShaderBegin, Tab, LineBreak, VertexToPixelDecl);
            EndLine();
            PrevIndent = Indent();
            FunctionParameterPrefix = FragmentShaderParameterPrefix;
            CompileStatement(fragment_method.Body);
            RestoreIndent(PrevIndent);
            Write(SpaceFormat(FragmentShaderEnd));
            EndLine();

            UseLocalSymbolMap(null);

            WriteLine();

            Write(SpaceFormat(FileEnd));

            // We must wait until after compiling the shader to know which methods that shader references.
            string methods = GetReferencedMethods();

            // We must wait until after compiling the shader to know which foreign variables that shader references.
            string foreign_vars = GetForeignVars();
            
            // Now get the full string written so far and insert the referenced methods.
            string fragment = GetString();
            fragment = SpecialFormat(fragment, foreign_vars, methods);

            return fragment;
        }

        virtual protected string GetForeignVars()
        {
            string foreign_vars = string.Empty;

            if (ReferencedForeignVars.Count == 0)
                return foreign_vars;

            var hold_writer = writer;
            writer = new StringWriter();

            var last = ReferencedForeignVars.Last();
            foreach (var foreign_var in ReferencedForeignVars)
            {
                //if shader
                CompileSamplerParameter_Foreign(foreign_var);
                WriteLine();
                // else
                //var param = new Param(GetType(foreign_var).Name, type, name, mapped_name, Param.ParamType.VertexParam);
                //Params.Add(param);
                //AllParams.Add(param);
            }

            foreign_vars = writer.ToString();
            writer = hold_writer;

            return foreign_vars;
        }

        public string CompileShaderBoilerplate(NamedTypeSymbol Symbol, List<Dictionary<Symbol, string>> Specializations)
        {
            ClearString();

            WriteLine("namespace {0}", Symbol.ContainingNamespace);
            WriteLine("{");

            var PrevIndent1 = Indent();

            WriteBoilerplateClass(Symbol, Specializations);

            RestoreIndent(PrevIndent1);

            WriteLine("}");
            WriteLine();

            string boilerplate = GetString();
            return boilerplate;
        }

        const string ApplyName = "Apply", UsingName = "Using";

        void WriteBoilerplateClass(NamedTypeSymbol symbol, List<Dictionary<Symbol, string>> Specializations)
        {
            WriteLine("public partial class {0}", symbol.Name);
            WriteLine("{");

            var PrevIndent = Indent();

            // If there are no specializations (count == 1) then we only need a single Effect.
            if (Specializations.Count == 1)
            {
                WriteLine("public static Effect CompiledEffect;");
            }
            else
            {
                // Otherwise we need an Effect for each specialization.
                foreach (var specialization in Specializations)
                {
                    WriteLine("public static Effect CompiledEffect{0};", ShaderClass.SpecializationVarSuffix(specialization));
                }
            }

            WriteLine();

            WriteBoilerplateInitFunc(symbol, Specializations);

            WriteBoilerplateSignature(ApplyName, "RenderTarget2D Output, Color Clear");
            WriteBoilerplateApplyFunc("Output", "Clear");
            WriteBoilerplateSignature(ApplyName, "RenderTarget2D Output");
            WriteBoilerplateApplyFunc("Output", "Color.Transparent");

            WriteBoilerplateSignature(UsingName, "RenderTarget2D Output, Color Clear");
            WriteBoilerplateUsingFuncOverload("Output", "Clear");
            WriteBoilerplateSignature(UsingName, "RenderTarget2D Output");
            WriteBoilerplateUsingFuncOverload("Output", "Color.Transparent");

            WriteBoilerplateSignature(UsingName);
            WriteBoilerplateUsingFunc(Specializations);

            RestoreIndent(PrevIndent);

            WriteLine("}");    
        }

        void WriteBoilerplateInitFunc(NamedTypeSymbol Symbol, List<Dictionary<Symbol, string>> Specializations)
        {
            BeginLine("public static void Init()");
            WriteLine("{");
            var PrevIndent = Indent();

            foreach (var specialization in Specializations)
            {
                string filename = ShaderClass.SpecializationFileName(Symbol, specialization);
                string suffix = ShaderClass.SpecializationVarSuffix(specialization);

                // Load the effect file.
                WriteLine("{0}{0}{0}{1}.{2}.CompiledEffect{3} = Content.Load<Effect>(\"FragSharpShaders/{4}\");", Tab, Symbol.ContainingNamespace, Symbol.Name, suffix, filename);

                // Grab the parameters for the effect.
                foreach (var param in Params)
                {
                    if (param.MappedType == "shader")
                    {
                        WriteLine("var param{3}_{0}_Texture = CompiledEffect{3}.Parameters[\"{0}_Texture\"];", param.MappedName, param.Name, suffix);
                        WriteLine("var param{3}_{0}_{2} = CompiledEffect{3}.Parameters[\"{0}_{2}\"];", param.MappedName, param.Name, Sampler.SizeSuffix, suffix);
                        WriteLine("var param{3}_{0}_{2} = CompiledEffect{3}.Parameters[\"{0}_{2}\"];", param.MappedName, param.Name, Sampler.DxDySuffix, suffix);
                    }
                    else
                    {
                        WriteLine("var param{3}_{0} = CompiledEffect{3}.Parameters[\"{0}\"];", param.MappedName, param.Name, suffix);
                    }
                }
            }

            RestoreIndent(PrevIndent);
            WriteLine("}");
        }

        void WriteBoilerplateSignature(string FunctionName, string ExtraParams = null)
        {
            BeginLine("public static void {0}(", FunctionName);

            foreach (var param in NonForeignParams)
            {
                Write("{0} {1}", param.TypeName, param.Name);

                if (ExtraParams != null || param != NonForeignParams.Last())
                {
                    Write(",{0}", Space);
                }
            }

            if (ExtraParams != null)
            {
                Write(ExtraParams);
            }

            EndLine(")");
        }

        void WriteInvokeUsing()
        {
            BeginLine("{0}(", UsingName);

            foreach (var param in NonForeignParams)
            {
                Write(param.Name);

                if (param != NonForeignParams.Last())
                {
                    Write(",{0}", Space);
                }
            }

            EndLine(");");
        }

        void WriteBoilerplateApplyFunc(string SetRenderTarget, string ClearTarget)
        {
            WriteLine("{");
            var PrevIndent = Indent();

            CodeFor_SetRender_Clear(SetRenderTarget, ClearTarget);

            WriteInvokeUsing();

            WriteLine("GridHelper.DrawGrid();");

            RestoreIndent(PrevIndent);
            WriteLine("}");    
        }

        private void CodeFor_SetRender_Clear(string SetRenderTarget, string ClearTarget)
        {
            if (SetRenderTarget != null)
                WriteLine("GridHelper.GraphicsDevice.SetRenderTarget({0});", SetRenderTarget);

            if (ClearTarget != null)
                WriteLine("GridHelper.GraphicsDevice.Clear({0});", ClearTarget);
        }

        void WriteBoilerplateUsingFuncOverload(string SetRenderTarget, string ClearTarget)
        {
            WriteLine("{");
            var PrevIndent = Indent();

            CodeFor_SetRender_Clear(SetRenderTarget, ClearTarget);

            WriteInvokeUsing();

            RestoreIndent(PrevIndent);
            WriteLine("}");
        }

        void WriteBoilerplateUsingFunc(List<Dictionary<Symbol, string>> Specializations)
        {
            WriteLine("{");
            var PrevIndent = Indent();

            if (Specializations.Count > 1)
            {
                foreach (var specialization in Specializations)
                {
                    WriteLine("{0}if ({1})",
                        specialization == Specializations.First() ? "" : "else ",
                        SpecializationEquality(specialization));

                    WriteLine("{");
                    var PrevIndent2 = Indent();

                    string suffix = ShaderClass.SpecializationVarSuffix(specialization);
                    WriteBoilerplateEffectApply(suffix, specialization);

                    RestoreIndent(PrevIndent2);
                    WriteLine("}");
                }
                WriteLine();

                WriteLine("else if (CompiledEffect == null) throw new Exception(\"Parameters do not match any specified specialization.\");");
                WriteLine();
            }
            else
            {
                WriteBoilerplateEffectApply();
            }

            RestoreIndent(PrevIndent);
            WriteLine("}");
        }

        void WriteBoilerplateEffectApply(string Suffix = "", Dictionary<Symbol, string> Specialization = null)
        {
            string param_name;

            foreach (var param in Params)
            {
                if (param.MappedType == "shader")
                {
                    param_name = $"param{Suffix}_{param.MappedName}_Texture";
                    WriteLine($"if ({param_name} != null) {param_name}.SetValue(FragSharpMarshal.Marshal({param.Name}));");

                    param_name = $"param{Suffix}_{param.MappedName}_{Sampler.SizeSuffix}";
                    WriteLine($"if ({param_name} != null) {param_name}.SetValue(FragSharpMarshal.Marshal(vec({param.Name}.Width, {param.Name}.Height)));");

                    param_name = $"param{Suffix}_{param.MappedName}_{Sampler.DxDySuffix}";
                    WriteLine($"if ({param_name} != null) {param_name}.SetValue(FragSharpMarshal.Marshal(1.0f / vec({param.Name}.Width, {param.Name}.Height)));");
                }
                else
                {
                    param_name = $"param{Suffix}_{param.MappedName}";
                    WriteLine("if (param_{0} != null) param_{0}.SetValue(FragSharpMarshal.Marshal({1}));", param.MappedName, param.Name);
                }
            }

            WriteLine($"CompiledEffect{Suffix}.CurrentTechnique.Passes[0].Apply();");
        }

        string SpecializationEquality(Dictionary<Symbol, string> specialization)
        {
            string equality = "";
            foreach (var variable in specialization)
            {
                if (variable.Key.ToString() == "bool")
                    equality += string.Format("{1}{0}=={0}{2}", Space, variable.Key.Name, variable.Value);
                else
                    equality += string.Format("abs((float)({1}{0}-{0}{2})){0}<{0}{3}", Space, variable.Key.Name, variable.Value, eps);

                if (variable.Key != specialization.Last().Key)
                    equality += string.Format("{0}&&{0}", Space);
            }

            return equality;
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

            public string TypeName, MappedType, Name, MappedName;
            public ParamType Type;

            public bool Foreign;

            public Param(string TypeName, string MappedType, string Name, string MappedName, ParamType Type)
            {
                this.TypeName = TypeName;
                this.MappedType = MappedType;
                this.Name = Name;
                this.MappedName = MappedName;
                this.Type = Type;
            }
        }

        /// <summary>
        /// Parameters that are used in the vertex/fragment shader, NOT including parameters factored out as constant specializations.
        /// </summary>
        List<Param> Params = new List<Param>();

        /// <summary>
        /// Parameters that are used in the vertex/fragment shader, INCLUDING parameters factored out as constant specializations.
        /// </summary>
        List<Param> AllParams = new List<Param>();

        IEnumerable<Param> NonForeignParams
        {
            get
            {
                return AllParams.Where(p => !p.Foreign);
            }
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
            }
        }

        Dictionary<Symbol, string> CompileFragmentSignature(MethodDeclarationSyntax method)
        {
            var SignatureMap = new Dictionary<Symbol, string>(specialization);

            var ParameterList = method.ParameterList.Parameters;
            if (ParameterList.Count == 0) return SignatureMap;

            var first = ParameterList.First();
            var last  = ParameterList.Last();
            foreach (var parameter in ParameterList)
            {
                var symbol = GetSymbol(parameter);

                // The first parameter must be a VertexOut, which maps to "psin"
                if (parameter == first)
                {
                    SignatureMap.Add(symbol, "psin");
                    continue;
                }

                // Skip specialization values
                bool specialized = SignatureMap.ContainsKey(symbol);

                CompileFragmentParameter(parameter, specialized);

                if (!specialized)
                {
                    EndLine();

                    if (parameter != last)
                    {
                        WriteLine();
                    }
                }
            }

            return SignatureMap;
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

                        var param = new Param(parameter.Type.ToString(), type, name, mapped_name, Param.ParamType.VertexParam);
                        Params.Add(param);
                        AllParams.Add(param);
                    }
                }
            }
        }

        void CompileFragmentParameter(ParameterSyntax parameter, bool specialized)
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
                        CompileSamplerParameter(parameter, symbol);
                    }
                    else
                    {
                        string type = translation_info.Translation;
                        string name = parameter.Identifier.ValueText;
                        string mapped_name = FragmentShaderParameterPrefix + name;

                        if (!specialized)
                        {
                            Write(type);
                            Write(" ");
                            Write(mapped_name);
                            Write(";");
                        }

                        var param = new Param(parameter.Type.ToString(), type, name, mapped_name, Param.ParamType.FragmentParam);
                        if (!specialized) Params.Add(param);
                        AllParams.Add(param);
                    }
                }
            }
        }

        /// <summary>
        /// All sampler types must derive directly or indirectly from the generic SamplerBase type.
        /// This method takes a sampler type and returns the realized SamplerBase type it derives from.
        /// </summary>
        /// <param name="sampler">The sampler type</param>
        /// <returns></returns>
        NamedTypeSymbol GetSamplerBase(TypeSymbol sampler)
        {
            if (sampler.HasAttribute("SamplerBase")) return sampler as NamedTypeSymbol;
            if (sampler.BaseType != null) return GetSamplerBase(sampler.BaseType);
            return null;
        }

        string TypeToFilter(TypeSymbol type)
        {
            switch (type.Name)
            {
                case "Linear": return "Linear";
                case "Point": return "Point";

                default: return "Point";
            }
        }

        string TypeToAddress(TypeSymbol type)
        {
            switch (type.Name)
            {
                case "Wrap": return "Wrap";
                case "Clamp": return "Clamp";

                default: return "Point";
            }
        }

        void CompileSamplerParameter_Foreign(Symbol symbol)
        {
            SamplerNumber++;

            string name = symbol.Name;
            string mapped_name = FragmentShaderParameterPrefix + name;

            var type = GetType(symbol);
            var sampler_base = GetSamplerBase(type);
            string address_u = TypeToAddress(sampler_base.TypeArguments[1]);
            string address_v = TypeToAddress(sampler_base.TypeArguments[2]);
            string min_filter = TypeToFilter(sampler_base.TypeArguments[3]);
            string mag_filter = TypeToFilter(sampler_base.TypeArguments[4]);
            string mip_filter = TypeToFilter(sampler_base.TypeArguments[5]);

            Write(SamplerTemplate, Tab, SamplerNumber, mapped_name, address_u, address_v, min_filter, mag_filter, mip_filter);

            var param = new Param("Texture2D", "shader", name, mapped_name, Param.ParamType.FragmentParam);
            param.Foreign = true;
            Params.Add(param);
            AllParams.Add(param);
        }

        void CompileSamplerParameter(ParameterSyntax parameter, TypeSymbol symbol)
        {
            SamplerNumber++;

            string name = parameter.Identifier.ValueText;
            string mapped_name = FragmentShaderParameterPrefix + name;

            var sampler_base = GetSamplerBase(symbol);
            string address_u = TypeToAddress(sampler_base.TypeArguments[1]);
            string address_v = TypeToAddress(sampler_base.TypeArguments[2]);
            string min_filter = TypeToFilter(sampler_base.TypeArguments[3]);
            string mag_filter = TypeToFilter(sampler_base.TypeArguments[4]);
            string mip_filter = TypeToFilter(sampler_base.TypeArguments[5]);
            
            Write(SamplerTemplate, Tab, SamplerNumber, mapped_name, address_u, address_v, min_filter, mag_filter, mip_filter);

            var param = new Param("Texture2D", "shader", name, mapped_name, Param.ParamType.FragmentParam);
            Params.Add(param);
            AllParams.Add(param);
        }

const string SamplerTemplate =
@"// Texture Sampler for {2}, using register location {1}
float2 {2}_" + Sampler.SizeSuffix + @";
float2 {2}_" + Sampler.DxDySuffix + @";

Texture {2}_Texture;
sampler {2} : register(s{1}) = sampler_state
{{
{0}texture   = <{2}_Texture>;
{0}MipFilter = {7};
{0}MagFilter = {6};
{0}MinFilter = {5};
{0}AddressU  = {3};
{0}AddressV  = {4};
}};";

const string ReferencedMethodsPreamble =
@"// The following methods are included because they are referenced by the fragment shader.";

const string ReferencedForeignVarsPreamble =
@"// The following variables are included because they are referenced but are not function parameters. Their values will be set at call time.";

const string VertexMethodParameters =
@"// The following are variables used by the vertex shader (vertex parameters).";

const string FragmentMethodParameters =
@"// The following are variables used by the fragment shader (fragment parameters).";

const string VertexShaderBegin =
@"// Compiled vertex shader
VertexToPixel StandardVertexShader(float2 inPos : POSITION0, float2 inTexCoords : TEXCOORD0, float4 inColor : COLOR0)
{{";

const string VertexShaderEnd =
@"}}";

const string FragmentShaderBegin = 
@"// Compiled fragment shader
PixelToFrame FragmentShader({2})
{{
{0}PixelToFrame __FinalOutput = (PixelToFrame)0;";

const string FragmentShaderEnd =
@"}}";

const string FileBegin =
@"// This file was auto-generated by FragSharp. It will be regenerated on the next compilation.
// Manual changes made will not persist and may cause incorrect behavior between compilations.

#define PIXEL_SHADER ps_3_0
#define VERTEX_SHADER vs_3_0

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
@"namespace FragSharpFramework
{{
{0}public class FragSharp
{0}{{
{0}{0}public static ContentManager Content;
{0}{0}public static GraphicsDevice GraphicsDevice;
{0}{0}public static void Initialize(ContentManager Content, GraphicsDevice GraphicsDevice)
{0}{0}{{
{0}{0}{0}FragSharp.Content = Content;
{0}{0}{0}FragSharp.GraphicsDevice = GraphicsDevice;";

public const string BoilerEndInitializer =
@"{0}{0}}}
{0}}}
}}";
    }
}
