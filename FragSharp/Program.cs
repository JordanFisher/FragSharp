using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using Roslyn.Compilers.CSharp;

using FragSharp.Build;
using FragSharpFramework;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

namespace FragSharp
{
    public static class Assert
    {
        public class AssertFail : Exception { public AssertFail() { } }

        public static void That(bool expression)
        {
            if (!expression)
            {
                throw new AssertFail();
            }
        }
    }

    static class ListExtension
    {
        public static void AddUnique<T>(this List<T> list, T item)
        {
            if (!list.Contains(item)) list.Add(item);
        }

        public static void AddUnique<T>(this List<T> list1, List<T> list2)
        {
            list1.AddRange(list2.Distinct().Except(list1));
        }
    }

    public enum TranslationType { ReplaceMember, ReplaceExpression, UnderscoreAppend };
    public struct MapInfo
    {
        public string Translation;
        public TranslationType TranslationType;

        public MapInfo(string translation, TranslationType translation_type = TranslationType.ReplaceMember)
        {
            this.Translation = translation;
            this.TranslationType = translation_type;
        }
    }

    public static class TranslationLookup
    {
        public static Dictionary<Symbol, MapInfo> SymbolMap = new Dictionary<Symbol, MapInfo>();

        public static MapInfo RecursiveLookup(TypeSymbol symbol)
        {
            if (SymbolMap.ContainsKey(symbol))
            {
                return SymbolMap[symbol];
            }
            else
            {
                if (symbol.BaseType != null)
                    return RecursiveLookup(symbol.BaseType);
            }

            return new MapInfo(null);
        }

        static void ProcessAccessor(TypeDeclarationSyntax type, AccessorDeclarationSyntax accessor)
        {
            Console.Write(0);
        }

        static void ProcessTypeMap(NamedTypeSymbol typemap)
        {
            var members = typemap.GetMembers();

            foreach (var member in members)
            {
                var attribute = member.GetAttribute("Hlsl");
                if (attribute != null)
                {
                    // Get single argument to this function. It will serve as the key in the map.
                    var method = member as MethodSymbol;
                    if (null != method)
                    {
                        var type = method.Parameters.First();
                        CreateMapEntry(type.Type, attribute);
                    }
                }
            }
        }

        public static void ProcessTypes(Dictionary<SyntaxTree, SemanticModel> Models, IEnumerable<SyntaxNode> Nodes)
        {
            var classes = Nodes.OfType<TypeDeclarationSyntax>();

            foreach (var _class in classes)
            {
                var symbol = Models[_class.SyntaxTree].GetDeclaredSymbol(_class);

                if (SymbolMap.ContainsKey(symbol)) continue;

                if (symbol.Name == "__TypeMaps")
                {
                    ProcessTypeMap(symbol);
                    continue;
                }

                var attribute = symbol.GetAttribute("Hlsl");

                if (attribute != null)
                {
                    CreateMapEntry(symbol, attribute);
                }
            }
        }

        private static void CreateMapEntry(Symbol member, AttributeData attribute)
        {
            var args = attribute.ConstructorArguments.ToList();
            if (args.Count > 1)
            {
                SymbolMap.Add(member, new MapInfo(args[0].Value.ToString(), (TranslationType)args[1].Value));
            }
            else if (args.Count > 0)
            {
                SymbolMap.Add(member, new MapInfo(args[0].Value.ToString()));
            }
            else
            {
                SymbolMap.Add(member, new MapInfo(member.Name));
            }
        }

        public static void ProcessMembers(Dictionary<SyntaxTree, SemanticModel> Models, IEnumerable<SyntaxNode> Nodes)
        {
            var classes = Nodes.OfType<TypeDeclarationSyntax>();
            HashSet<Symbol> Processed = new HashSet<Symbol>(); // Keep track of which classes have been processed so we don't double-process.
            
            foreach (var _class in classes)
            {
                var symbol = Models[_class.SyntaxTree].GetDeclaredSymbol(_class);

                if (Processed.Contains(symbol)) continue;

                Processed.Add(symbol);
                
                var members = symbol.GetMembers();

                foreach (var member in members)
                {
                    if (member is NamedTypeSymbol) continue; // Skip nested type defintions. We alraedy processed all types.

                    var attribute = member.GetAttribute("Hlsl");
                    if (attribute != null)
                    {
                        CreateMapEntry(member, attribute);
                    }
                }
            }
        }

        public static void ProcessReadonlys(Dictionary<SyntaxTree, SemanticModel> Models, IEnumerable<SyntaxNode> Nodes, Compilation Compilation)
        {
            var classes = Nodes.OfType<TypeDeclarationSyntax>();

            foreach (var _class in classes)
            {
                var symbol = Models[_class.SyntaxTree].GetDeclaredSymbol(_class);
                var members = symbol.GetMembers();

                foreach (var member in members)
                {
                    if (!SymbolMap.ContainsKey(member) && member.IsDefinition && member is FieldSymbol)
                    {
                        var field = (FieldSymbol)member;

                        if (field.IsReadOnly)
                        {
                            var decleration = member.DeclaringSyntaxNodes.First() as VariableDeclaratorSyntax;

                            if (decleration.Initializer == null) continue; // Skip readonly's that have no compile time values.

                            var creation = decleration.Initializer.Value;// as ObjectCreationExpressionSyntax;
                            if (null != creation)
                            {
                                var constructor_info = Models[creation.SyntaxTree].GetSymbolInfo(creation);
                                var constructor = constructor_info.Symbol;

                                if (constructor != null && SymbolMap.ContainsKey(constructor))
                                {
                                    var writer = new HlslWriter(Models, Compilation);

                                    writer.CompileExpression(creation);
                                    var translation = writer.GetString();

                                    SymbolMap.Add(member, new MapInfo(translation, TranslationType.ReplaceExpression));
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    class Paths
    {
        public readonly string CompilerDir, FrameworkDir, ProjectDir, TargetDir, BoilerRoot, ShaderCompileDir, ShaderBuildDir, ProjectPath;

        public Paths(string[] args)
        {
            ProjectPath = args[0];
            TargetDir = args[1];

            ProjectDir = Path.GetDirectoryName(ProjectPath);

            BoilerRoot = Path.Combine(ProjectDir, "__FragSharp");
            ShaderCompileDir = Path.Combine(ProjectDir, "__GeneratedShaders");
            ShaderBuildDir = Path.Combine(Path.Combine(TargetDir, "Content"), "FragSharpShaders");

            CompilerDir  = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            FrameworkDir = Path.Combine("FragSharpFramework", CompilerDir);
        }
    }

    class ShaderClass : RoslynHelper
    {
        public static List<ShaderClass> Shaders = new List<ShaderClass>();

        public string TargetFile;

        static Dictionary<NamedTypeSymbol, ShaderClass> SymbolLookup = new Dictionary<NamedTypeSymbol, ShaderClass>();
        //static Dictionary<MethodDeclarationSyntax, HlslShaderWriter>
        //    VertexCompilations   = new Dictionary<MethodDeclarationSyntax, HlslShaderWriter>(),
        //    FragmentCompilations = new Dictionary<MethodDeclarationSyntax, HlslShaderWriter>();

        Dictionary<SyntaxTree, SemanticModel> Models;
        Compilation SourceCompilation;

        public static void AddShader(NamedTypeSymbol symbol, Dictionary<SyntaxTree, SemanticModel> Models, Compilation SourceCompilation)
        {
            if (!SymbolLookup.ContainsKey(symbol))
            {
                var shader = new ShaderClass(symbol, Models, SourceCompilation);

                SymbolLookup.Add(symbol, shader);
                Shaders.Add(shader);
            }
        }

        ShaderClass(NamedTypeSymbol symbol, Dictionary<SyntaxTree, SemanticModel> Models, Compilation SourceCompilation)
        {
            Symbol = symbol;
            this.Models = Models;
            this.SourceCompilation = SourceCompilation;

            // Get all syntax nodes
            Nodes = new List<SyntaxNode>();
            foreach (var node in Symbol.DeclaringSyntaxNodes)
            {
                Nodes.AddRange(node.DescendantNodes());
            }

            // Find all methods
            Methods = Nodes.OfType<MethodDeclarationSyntax>().ToList();
        }

        public NamedTypeSymbol Symbol;

        public List<SyntaxNode> Nodes;
        public List<MethodDeclarationSyntax> Methods;

        public ShaderClass BaseClass;

        public MethodDeclarationSyntax VertexShaderDecleration, FragmentShaderDecleration;

        public List<Dictionary<Symbol, string>> Specializations = new List<Dictionary<Symbol, string>>();

        public void Setup()
        {
            GetBaseClass();
            GetVertexShaderDecleration();
            GetFragmentShaderDecleration();
        }

        public static string SpecializationVarSuffix(Dictionary<Symbol, string> specialization)
        {
            string suffix = "";
            foreach (var variable in specialization)
            {
                suffix += string.Format("_{0}_{1}", variable.Key.Name, variable.Value.Replace("=", "_eq_").Replace(".", "p"));
            }

            return suffix;
        }

        public string SpecializationFileName(Dictionary<Symbol, string> specialization)
        {
            string suffix = "";
            foreach (var variable in specialization)
            {
                suffix += string.Format("_{0}={1}", variable.Key.Name, variable.Value);
            }

            return Symbol.Name + suffix;
        }

        public void WriteLoadCode(StringWriter BoilerWriter, string Tab)
        {
            if (!IsValidShader()) return;

            foreach (var specialization in Specializations)
            {
                string filename = SpecializationFileName(specialization);
                string suffix = SpecializationVarSuffix(specialization);
                BoilerWriter.WriteLine("{0}{0}{0}{1}.{2}.CompiledEffect{3} = Content.Load<Effect>(\"FragSharpShaders/{4}\");", Tab, Symbol.ContainingNamespace, Symbol.Name, suffix, filename);
            }
        }

        public void CompileAndWrite(StringWriter BoilerWriter, string CompileDir)
        {
            if (!IsValidShader()) return;

            foreach (var specialization in Specializations)
            {
                string name = SpecializationFileName(specialization);

                var compiled = Compile(specialization, specialization == Specializations.Last());

                TargetFile = Path.Combine(CompileDir, name) + ".fx";

                File.WriteAllText(TargetFile, compiled.Code);

                BoilerWriter.Write(compiled.Boilerplate);
                BoilerWriter.WriteLine();
            }
        }

        public ShaderCompilation Compile(Dictionary<Symbol, string> specialization, bool CompileBoilerplate)
        {
            if (!IsValidShader()) return null;

            HlslShaderWriter writer = new HlslShaderWriter(Models, SourceCompilation, specialization);
            
            string fragment = writer.CompileShader(Symbol, VertexShaderDecleration, FragmentShaderDecleration);
            
            string boilerplate = null;
            if (CompileBoilerplate)
            {
                boilerplate = writer.CompileShaderBoilerplate(Symbol, Specializations);
            }

            return new ShaderCompilation(fragment, boilerplate);
        }

        private bool IsValidShader()
        {
            return VertexShaderDecleration != null && FragmentShaderDecleration != null;
        }

        ShaderClass GetBaseClass()
        {
            if (BaseClass != null) return BaseClass;

            if (SymbolLookup.ContainsKey(Symbol.BaseType))
            {
                BaseClass = SymbolLookup[Symbol.BaseType];
                return BaseClass;
            }

            return null;
        }

        MethodDeclarationSyntax GetVertexShaderDecleration()
        {
            if (VertexShaderDecleration != null)
            {
                return VertexShaderDecleration;
            }

            var vertex_shaders = Methods.Where(method => HasAttribute(method, "VertexShader")).ToList();

            if (vertex_shaders.Count > 0)
            {
                VertexShaderDecleration = vertex_shaders[0];
                return VertexShaderDecleration;
            }

            if (GetBaseClass() != null)
            {
                VertexShaderDecleration = BaseClass.GetVertexShaderDecleration();
                return VertexShaderDecleration;
            }

            VertexShaderDecleration = null;
            return null;
        }

        MethodDeclarationSyntax GetFragmentShaderDecleration()
        {
            if (FragmentShaderDecleration != null)
            {
                return FragmentShaderDecleration;
            }

            var Fragment_shaders = Methods.Where(method => HasAttribute(method, "FragmentShader")).ToList();

            if (Fragment_shaders.Count > 0)
            {
                FragmentShaderDecleration = Fragment_shaders[0];
                return FragmentShaderDecleration;
            }

            if (BaseClass != null)
            {
                FragmentShaderDecleration = BaseClass.GetFragmentShaderDecleration();
                return FragmentShaderDecleration;
            }

            FragmentShaderDecleration = null;
            return null;
        }

        static bool HasAttribute(MethodDeclarationSyntax method, string AttributeName)
        {
            return method.AttributeLists.Any(
              list => list.Attributes.Any(
                attribute => attribute.Name.ToString() == AttributeName));
        }

        public void GetSpecializations()
        {
            if (!IsValidShader()) return;

            var ParameterList = FragmentShaderDecleration.ParameterList.Parameters;
            if (ParameterList.Count == 0) return;

            Specializations.Clear();
            Specializations.Add(new Dictionary<Symbol, string>());

            var first = ParameterList.First();
            foreach (var parameter in ParameterList)
            {
                var symbol = GetSymbol(parameter, Models);

                // The first parameter must be a VertexOut and can't be specialized, so skip it
                if (parameter == first)
                {
                    continue;
                }

                List<TypedConstant> speciliazation_vals = new List<TypedConstant>();

                // Get specialization values
                var Vals = symbol.GetAttribute("Vals");
                if (Vals != null && Vals.ConstructorArguments.Count() > 0)
                {
                    var args = Vals.ConstructorArguments;

                    speciliazation_vals.AddRange(args.First().Values);
                }

                // Get second-order specialization values
                foreach (var attr in symbol.GetAttributes())
                {
                    var vals_attr = attr.AttributeClass.GetAttribute("Vals");
                    if (vals_attr != null && vals_attr.ConstructorArguments.Count() > 0)
                    {
                        var args = vals_attr.ConstructorArguments;

                        speciliazation_vals.AddRange(args.First().Values);
                    }
                }

                // Add specialization values to 
                if (speciliazation_vals.Count > 0)
                {
                    List<Dictionary<Symbol, string>> copy = new List<Dictionary<Symbol, string>>(Specializations);
                    Specializations.Clear();

                    if (copy.Count == 0)
                    {
                        copy.Add(new Dictionary<Symbol, string>());
                    }

                    foreach (var specialization in copy)
                    {
                        foreach (var arg in speciliazation_vals)
                        {
                            var _specialization = new Dictionary<Symbol, string>(specialization);

                            string val_str = arg.Value.ToString();
                            if (val_str == "True") val_str = "true";
                            else if (val_str == "False") val_str = "false";

                            _specialization.Add(symbol, val_str);

                            Specializations.Add(_specialization);
                        }
                    }
                }
            }
        }
    }

    internal static class Program
    {
        static Paths BuildPaths;

        public const string Tab = "    ";

        const string ExtensionFileName = "__ExtensionBoilerplate.cs";
        const string BoilerplateFileName = "__ShaderBoilerplate.cs";

        static Dictionary<SyntaxTree, SemanticModel> Models;
        static Compilation SourceCompilation;
        static List<SyntaxNode> Nodes;

        static bool IsShader(this NamedTypeSymbol symbol)
        {
            if (symbol.BaseType == null) return false;
            else return 
                    symbol.BaseType.ToString() == "FragSharpFramework.FragSharpStd" ||
                    symbol.BaseType.IsShader();
        }

        static SemanticModel Model(SyntaxNode node)
        {
            return Models[node.SyntaxTree];
        }

        static bool CreateExtensionBoilerplate(Dictionary<SyntaxTree, SemanticModel> Models, IEnumerable<SyntaxNode> Nodes)
        {
            StringWriter writer = new StringWriter();

            writer.WriteLine(
@"// This file was auto-generated by FragSharp. It will be regenerated on the next compilation.
// Manual changes made will not persist and may cause incorrect behavior between compilations.

using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using FragSharpFramework;
");

            var classes = Nodes.OfType<TypeDeclarationSyntax>();
            HashSet<Symbol> Processed = new HashSet<Symbol>(); // Keep track of which classes have been processed so we don't double-process.

            foreach (var _class in classes)
            {
                var copy_attribute = GetCopyAttribute(_class);
                if (copy_attribute == null) continue;

                var args = copy_attribute.ArgumentList.Arguments;
                var arg = args[0].Expression as TypeOfExpressionSyntax;
                var type = Models[_class.SyntaxTree].GetSymbolInfo(arg.Type).Symbol as NamedTypeSymbol;

                string cast_type = "explicit";
                if (args.Count >= 2)
                {
                    var style_name = Models[_class.SyntaxTree].GetSymbolInfo(args[1].Expression).Symbol.Name;

                    if (style_name == Enum.GetName(typeof(CastStyle), CastStyle.ImplicitCast)) cast_type = "implicit";
                    if (style_name == Enum.GetName(typeof(CastStyle), CastStyle.ExplicitCasts)) cast_type = "explicit";
                    if (style_name == Enum.GetName(typeof(CastStyle), CastStyle.NoCasts)) cast_type = null;
                }

                var class_symbol = Models[_class.SyntaxTree].GetDeclaredSymbol(_class);
                if (Processed.Contains(class_symbol)) continue;
                Processed.Add(class_symbol);

                if (type != null)
                {
                    var code = type.DeclaringSyntaxNodes.First();

                    var output = code.ToFullString().Replace(type.Name, class_symbol.Name);
                    output = output.Replace("/*KeepInCopy*/"  + class_symbol.Name, type.Name);
                    output = output.Replace("/*KeepInCopy*/ " + class_symbol.Name, type.Name);

                    switch (cast_type)
                    {
                        case null:
                            output = output.Replace("// Extra code gen goes here", "");
                            break;

                        default:
                            output = output.Replace("// Extra code gen goes here",
                                string.Format(@"public static {2} operator {1}(vec4 v) {{ return new {1}(v.x, v.y, v.z, v.w); }}
        public static {2} operator vec4({1} v) {{ return new vec4(v.x, v.y, v.z, v.w); }}", " ", class_symbol.Name, cast_type));

                            break;
                    }
                    
                    writer.WriteLine("namespace {0}", class_symbol.ContainingNamespace);
                    writer.Write("{");
                    writer.Write(output);
                    writer.WriteLine("}");
                    writer.WriteLine();
                }
            }

            var text = writer.ToString();
            string path = Path.Combine(BuildPaths.BoilerRoot, ExtensionFileName);

            try
            {
                if (text.GetHashCode() == File.ReadAllText(path).GetHashCode())
                {
                    return false;
                }
            }
            catch
            {
            }

            Directory.CreateDirectory(BuildPaths.BoilerRoot);
            File.WriteAllText(path, writer.ToString());
            return true;
        }

        static AttributeSyntax GetValsAttribute(TypeDeclarationSyntax _class)
        {
            return GetAttribute(_class, "Vals");
        }

        static AttributeSyntax GetCopyAttribute(TypeDeclarationSyntax _class)
        {
            return GetAttribute(_class, "Copy");
        }

        static AttributeSyntax GetAttribute(TypeDeclarationSyntax _class, string AttributeName)
        {
            foreach (var node in _class.ChildNodes())
            {
                var attribute_node = node as AttributeListSyntax;
                if (null != attribute_node)
                {
                    foreach (var attribute in attribute_node.Attributes)
                    {
                        if (attribute.Name.ToString() == AttributeName)
                        {
                            return attribute;
                        }
                    }
                }
            }

            return null;
        }

        static void ParseArgs(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("FragSharp requires two arguments: Source directory; Output directory.");
                Console.WriteLine("Defaulting to debug directories.");

                //ParseArgs(new string[] {
                //    /* Source */ "C:/Users/Jordan/Desktop/Dir/Projects/FragSharp/Examples/Life/",
                //    /* Output */ "C:/Users/Jordan/Desktop/Dir/Projects/FragSharp/Examples/Life/bin/x86/Debug/" });

                ParseArgs(new string[] {
                    /* Source */ "C:/Users/Jordan/Desktop/Dir/Pwnee/Games/Pinnacle/GpuSim/GpuSim/GpuSim/GpuSim.csproj",
                    /* Output */ "C:/Users/Jordan/Desktop/Dir/Pwnee/Games/Pinnacle/GpuSim/GpuSim/GpuSim/bin/x86/Debug/" });
            }
            else
            {
                BuildPaths = new Paths(args);
            }
        }

        private static void Main(string[] args)
        {
            ParseArgs(args);

            // Get and compile the user's code
            CompileUserCode();

            // Create __ExtensionBoilerplate.cs
            bool changed = CreateExtensionBoilerplate(Models, Nodes);
            
            // Recompile
            if (changed)
                CompileUserCode();

            // Create translation lookup
            TranslationLookup.ProcessTypes(Models, Nodes);
            TranslationLookup.ProcessMembers(Models, Nodes);
            TranslationLookup.ProcessReadonlys(Models, Nodes, SourceCompilation);

            // Find all shader classes
            var classes = Nodes.OfType<ClassDeclarationSyntax>();

            foreach (var _class in classes)
            {
                var symbol = Model(_class).GetDeclaredSymbol(_class);

                if (IsShader(symbol))
                    ShaderClass.AddShader(symbol, Models, SourceCompilation);
            }

            foreach (var shader in ShaderClass.Shaders)
            {
                shader.Setup();
                Console.WriteLine("{0}, vertex = {1}, fragment = {2}", shader.Symbol,
                    shader.VertexShaderDecleration == null ? "none" : shader.VertexShaderDecleration.Identifier.ToString(),
                    shader.FragmentShaderDecleration == null ? "none" : shader.FragmentShaderDecleration.Identifier.ToString());
            }

            foreach (var shader in ShaderClass.Shaders)
            {
                shader.GetSpecializations();
            }

            // Create shader directory to store compiled shaders. Empty it if it has files in it.
            Directory.CreateDirectory(BuildPaths.ShaderCompileDir);
            foreach (var file in Directory.GetFiles(BuildPaths.ShaderCompileDir, "*", SearchOption.AllDirectories))
            {
                File.Delete(file);
            }

            // Compile shaders from C# to target language
            StringWriter BoilerWriter = new StringWriter();
            BoilerWriter.WriteLine(HlslShaderWriter.BoilerFileBegin, Tab);
            BoilerWriter.WriteLine();

            BoilerWriter.WriteLine(HlslShaderWriter.BoilerBeginInitializer, Tab);

            foreach (var shader in ShaderClass.Shaders)
            {
                shader.WriteLoadCode(BoilerWriter, Tab);
            }

            BoilerWriter.WriteLine(HlslShaderWriter.BoilerEndInitializer, Tab);
            BoilerWriter.WriteLine();

            foreach (var shader in ShaderClass.Shaders)
            {
                shader.CompileAndWrite(BoilerWriter, BuildPaths.ShaderCompileDir);
            }

            Directory.CreateDirectory(BuildPaths.BoilerRoot);
            File.WriteAllText(Path.Combine(BuildPaths.BoilerRoot, BoilerplateFileName), BoilerWriter.ToString());

            // Compile target shaders
            BuildGeneratedShaders();
            //BuildGeneratedShaders2();
        }

        static void BuildGeneratedShaders2()
        {
            // Empty the build directory
            Directory.CreateDirectory(BuildPaths.ShaderBuildDir);
            foreach (var file in Directory.GetFiles(BuildPaths.ShaderBuildDir))
            {
                File.Delete(file);
            }

            // Build each shader
            foreach (var file in Directory.GetFiles(BuildPaths.ShaderCompileDir, "*.fx"))
            {
                string fx = File.ReadAllText(file);

                EffectProcessor effectProcessor = new EffectProcessor();
                effectProcessor.DebugMode = EffectProcessorDebugMode.Debug;
                var effect = effectProcessor.Process(new EffectContent { EffectCode = fx }, new MyProcessorContext());

                byte[] ShaderObject = effect.GetEffectCode();

                string output_file = Path.Combine(BuildPaths.ShaderBuildDir, Path.GetFileNameWithoutExtension(file)) + ".xnb";
                File.WriteAllBytes(output_file, ShaderObject);
            }

            //foreach (var shader in ShaderClass.Shaders)
            //{
            //    if (shader.TargetFile == null) continue;

            //    string fx = File.ReadAllText(shader.TargetFile);

            //    EffectProcessor effectProcessor = new EffectProcessor();
            //    effectProcessor.DebugMode = EffectProcessorDebugMode.Debug;
            //    var effect = effectProcessor.Process(new EffectContent { EffectCode = fx }, new MyProcessorContext());

            //    byte[] ShaderObject = effect.GetEffectCode();

            //    string output_file = Path.Combine(BuildPaths.ShaderBuildDir, Path.GetFileNameWithoutExtension(shader.TargetFile)) + ".xnb";
            //    File.WriteAllBytes(output_file, ShaderObject);
            //}
        }

        class MyProcessorLogger : ContentBuildLogger
        {
            public static string Log = string.Empty;
            public override void LogMessage(string message, params object[] messageArgs) { Log += message; }
            public override void LogImportantMessage(string message, params object[] messageArgs) { Log += message; }
            public override void LogWarning(string helpLink, ContentIdentity contentIdentity, string message, params object[] messageArgs) { Log += message; }
        }

        class MyProcessorContext : ContentProcessorContext
        {
            public override TargetPlatform TargetPlatform { get { return TargetPlatform.Windows; } }
            public override GraphicsProfile TargetProfile { get { return GraphicsProfile.HiDef; } }
            public override string BuildConfiguration { get { return string.Empty; } }
            public override string IntermediateDirectory { get { return string.Empty; } }
            public override string OutputDirectory { get { return string.Empty; } }
            public override string OutputFilename { get { return string.Empty; } }

            public override OpaqueDataDictionary Parameters { get { return parameters; } }
            OpaqueDataDictionary parameters = new OpaqueDataDictionary();

            public override ContentBuildLogger Logger { get { return logger; } }
            ContentBuildLogger logger = new MyProcessorLogger();

            public override void AddDependency(string filename) { }
            public override void AddOutputFile(string filename) { }

            public override TOutput Convert<TInput, TOutput>(TInput input, string processorName, OpaqueDataDictionary processorParameters) { throw new NotImplementedException(); }
            public override TOutput BuildAndLoadAsset<TInput, TOutput>(ExternalReference<TInput> sourceAsset, string processorName, OpaqueDataDictionary processorParameters, string importerName) { throw new NotImplementedException(); }
            public override ExternalReference<TOutput> BuildAsset<TInput, TOutput>(ExternalReference<TInput> sourceAsset, string processorName, OpaqueDataDictionary processorParameters, string importerName, string assetName) { throw new NotImplementedException(); }
        }

        static void BuildGeneratedShaders()
        {
            ContentBuilder contentBuilder = new ContentBuilder();

            contentBuilder.Clear();

            foreach (var file in Directory.GetFiles(BuildPaths.ShaderCompileDir, "*.fx"))
            {
                string name = Path.GetFileNameWithoutExtension(file);

                contentBuilder.Add(file, name, "EffectImporter", "EffectProcessor");
            }

            //foreach (var shader in ShaderClass.Shaders)
            //{
            //    if (shader.TargetFile == null) continue;

            //    contentBuilder.Add(shader.TargetFile, shader.Symbol.Name, "EffectImporter", "EffectProcessor");
            //}

            // Empty the build directory
            Directory.CreateDirectory(BuildPaths.ShaderBuildDir);
            foreach (var file in Directory.GetFiles(BuildPaths.ShaderBuildDir))
            {
                File.Delete(file);
            }

            // Build the shaders
            string buildError = contentBuilder.Build();

            /* fxc build, debug, and asm output
            if (buildError != null && buildError.Length > 0)
            {
                string fxc = "C:/Program Files (x86)/Microsoft DirectX SDK (June 2010)/Utilities/bin/x86/fxc.exe";
                string file = "C:/Users/Jordan/Desktop/Dir/Projects/Million/GpuSim/GpuSim/GpuSim/__GeneratedShaders/_Counting.fx";
                
                //string arguments = string.Format("/Od /Zi /Tfx_2_0 /Fo {0}o {0}", file);
                //string arguments = string.Format("/Tfx_2_0 /Fc {0}o.asm {0}", file); // Generate asm text
                string arguments = string.Format("/Tfx_2_0 /Fe {0}.out {0}", file); // Generate errors and warnings

                string output = RunCommand(fxc, arguments);
                Console.WriteLine(output);
            }
            */

            var files = Directory.GetFiles(contentBuilder.BuiltDirectory);

            foreach (var file in files)
            {
                string new_file = Path.Combine(BuildPaths.ShaderBuildDir, Path.GetFileName(file));
                File.Copy(file, new_file);
            }
        }

        static void CompileUserCode()
        {
            //var sln = Solution.LoadStandAloneProject(BuildPaths.ProjectPath).Solution;
            //sln.Projects.First().GetCompilation();
            //foreach (var doc in sln.Projects.First().DocumentIds)
            //    sln = sln.ReloadDocument(doc);
            //sln.Projects.First().GetCompilation();

            // Get all the relevant source files
            var files =    Directory.GetFiles(BuildPaths.ProjectDir, "*.cs", SearchOption.AllDirectories).ToList();
            files.AddRange(Directory.GetFiles(BuildPaths.FrameworkDir, "*.cs", SearchOption.AllDirectories).ToList());

            // Get all the syntax trees from the source files
            List<SyntaxTree> Trees = new List<SyntaxTree>();
            foreach (var file in files)
            {
                Trees.Add(SyntaxTree.ParseFile(file));
            }

            Nodes = new List<SyntaxNode>();
            foreach (var tree in Trees)
            {
                Nodes.AddRange(tree.GetRoot().DescendantNodes());
            }

            // Compile all the sources together
            SourceCompilation = Compilation.Create("MyCompilation",
                                             syntaxTrees: Trees,
                                             references: new List<MetadataReference>() { MetadataReference.CreateAssemblyReference(typeof(object).Assembly.FullName) });

            Models = new Dictionary<SyntaxTree, SemanticModel>();
            foreach (var tree in Trees)
            {
                var model = SourceCompilation.GetSemanticModel(tree);
                Models.Add(tree, model);
            }
        }

        static void PrintTree(SyntaxNodeOrToken node, string indent = "")
        {
            using (var writer = new StringWriter())
            {
                WriteTree(node, writer, indent);
                Console.Write(writer);
            }
        }

        static void WriteTree(SyntaxNodeOrToken node, StringWriter output, string indent = "")
        {
            var nodes = node.ChildNodesAndTokens();
            foreach (var child in nodes)
            {
                string kind = string.Empty;
                string value = string.Empty;

                if (child.IsNode)
                {
                    kind = child.AsNode().Kind.ToString();
                }
                else
                {
                    kind = child.AsToken().Kind.ToString();
                    value = child.AsToken().ValueText;
                }

                output.WriteLine("{0}{1}  {2}", indent, kind, value);

                WriteTree(child, output, indent + "--");
            }
        }

        static bool IsGridComputtion(MethodDeclarationSyntax method)
        {
            return true;
        }

        static List<T> GetNodes<T>(SyntaxNodeOrToken node, List<T> methods = null) where T : class
        {
            if (methods == null)
            {
                methods = new List<T>();
            }

            var nodes = node.ChildNodesAndTokens();
            foreach (var child in nodes)
            {
                var method = child.AsNode() as T;
                if (null != method)
                {
                    methods.Add(method);
                }

                GetNodes(child, methods);
            }

            return methods;
        }

        static string RunCommand(string Executable, string Arguments)
        {
            // Start the child process.
            Process p = new Process();

            // Redirect the output stream of the child process.
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = Executable;
            p.StartInfo.Arguments = Arguments;

            p.Start();

            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            return output;
        }
   }
}
