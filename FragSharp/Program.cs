using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

using FragSharp.Build;

namespace FragSharp
{
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
    
    public enum TranslationType { ReplaceMember, ReplaceExpression };
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

        static AttributeData GetHlslAttribute(Symbol symbol)
        {
            var attributes = symbol.GetAttributes();
            if (attributes.Count == 0) return null;

            var attribute = attributes.FirstOrDefault(data => data.AttributeClass.Name == "HlslAttribute");

            return attribute;
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
                var attribute = GetHlslAttribute(member);
                if (attribute != null)
                {
                    // Get single argument to this function. It was serve as the key in the map.
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

                var attribute = GetHlslAttribute(symbol);

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

                    //if (member.DeclaringSyntaxNodes.Count > 0 && member.DeclaringSyntaxNodes[0] is AccessorDeclarationSyntax)
                    //{
                    //    ProcessAccessor(_class, (AccessorDeclarationSyntax)member.DeclaringSyntaxNodes[0]);
                    //}
                    //else

                    var attribute = GetHlslAttribute(member);
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

    internal static class Program
    {
        static readonly string ProjRoot = "C:/Users/Jordan/Desktop/Dir/Projects/Million/GpuSim";
        static readonly string SrcRoot = "C:/Users/Jordan/Desktop/Dir/Projects/Million/GpuSim/GpuSim/GpuSim";
        static readonly string ShaderCompileDir = "C:/Users/Jordan/Desktop/Dir/Projects/Million/GpuSim/GpuSim/GpuSim/__GeneratedShaders";
        static readonly string ShaderBuildDir = "C:/Users/Jordan/Desktop/Dir/Projects/Million/GpuSim/GpuSim/GpuSim/bin/x86/Debug/Content/FragSharpShaders";

        const string ExtensionFileName = "__ExtensionBoilerplate.cs";
        const string BoilerplateFileName = "__ShaderBoilerplate.cs";

        class ShaderClass
        {
            public static List<ShaderClass> Shaders = new List<ShaderClass>();

            public string TargetFile;

            static Dictionary<NamedTypeSymbol, ShaderClass> SymbolLookup = new Dictionary<NamedTypeSymbol, ShaderClass>();
            //static Dictionary<MethodDeclarationSyntax, HlslShaderWriter>
            //    VertexCompilations   = new Dictionary<MethodDeclarationSyntax, HlslShaderWriter>(),
            //    FragmentCompilations = new Dictionary<MethodDeclarationSyntax, HlslShaderWriter>();

            public static void AddShader(NamedTypeSymbol symbol)
            {
                if (!SymbolLookup.ContainsKey(symbol))
                {
                    var shader = new ShaderClass(symbol);

                    SymbolLookup.Add(symbol, shader);
                    Shaders.Add(shader);
                }
            }

            ShaderClass(NamedTypeSymbol symbol)
            {
                Symbol = symbol;

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

            public void Setup()
            {
                GetBaseClass();
                GetVertexShaderDecleration();
                GetFragmentShaderDecleration();
            }

            public ShaderCompilation Compile()
            {
                if (VertexShaderDecleration == null || FragmentShaderDecleration == null) return null;

                HlslShaderWriter writer = new HlslShaderWriter(Models, SourceCompilation);
                var output = writer.CompileShader(Symbol, VertexShaderDecleration, FragmentShaderDecleration);

                return output;
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

                if (BaseClass != null)
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
        }

        static Dictionary<SyntaxTree, SemanticModel> Models;
        static Compilation SourceCompilation;
        static List<SyntaxNode> Nodes;

        static bool IsShader(this NamedTypeSymbol symbol)
        {
            if (symbol.BaseType == null) return false;
            else return symbol.BaseType.ToString() == "FragSharpFramework.Shader" || symbol.BaseType.IsShader();
        }

        static SemanticModel Model(SyntaxNode node)
        {
            return Models[node.SyntaxTree];
        }

        static void CreateExtensionBoilerplate(Dictionary<SyntaxTree, SemanticModel> Models, IEnumerable<SyntaxNode> Nodes)
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

");

            var classes = Nodes.OfType<TypeDeclarationSyntax>();
            HashSet<Symbol> Processed = new HashSet<Symbol>(); // Keep track of which classes have been processed so we don't double-process.

            foreach (var _class in classes)
            {
                var symbol = Models[_class.SyntaxTree].GetDeclaredSymbol(_class);

                if (Processed.Contains(symbol)) continue;

                Processed.Add(symbol);

                var attributes = symbol.GetAttributes().ToList();
                if (attributes.Count == 0) continue;

                var copy = attributes.Find(a => a.AttributeClass.Name.ToString() == "CopyAttribute");
                if (copy != null)
                {
                    var type = copy.ConstructorArguments.First().Value as NamedTypeSymbol;
                    var code = type.DeclaringSyntaxNodes.First();

                    var output = code.ToFullString().Replace(type.Name, symbol.Name);
                    
                    writer.WriteLine("namespace {0}", symbol.ContainingNamespace);
                    writer.Write("{");
                    writer.Write(output);
                    writer.WriteLine("}");
                    writer.WriteLine();
                }
            }

            File.WriteAllText(Path.Combine(SrcRoot, ExtensionFileName), writer.ToString());
        }

        private static void Main()
        {
            string Tab = "    ";

            // Get and compile the user's code
            CompileUserCode();

            // Create __ExtensionBoilerplate.cs
            CreateExtensionBoilerplate(Models, Nodes);
            
            // Recompile
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
                    ShaderClass.AddShader(symbol);
            }

            foreach (var shader in ShaderClass.Shaders)
            {
                shader.Setup();
                Console.WriteLine("{0}, vertex = {1}, fragment = {2}", shader.Symbol,
                    shader.VertexShaderDecleration == null ? "none" : shader.VertexShaderDecleration.Identifier.ToString(),
                    shader.FragmentShaderDecleration == null ? "none" : shader.FragmentShaderDecleration.Identifier.ToString());
            }

            // Create shader directory to store compiled shaders. Empty it if it has files in it.
            Directory.CreateDirectory(ShaderCompileDir);
            foreach (var file in Directory.GetFiles(ShaderCompileDir, "*", SearchOption.AllDirectories))
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
                if (shader.VertexShaderDecleration == null || shader.FragmentShaderDecleration == null) continue;

                BoilerWriter.WriteLine("{0}{0}{0}{1}.{2}.CompiledEffect = Content.Load<Effect>(\"FragSharpShaders/{2}\");", Tab, shader.Symbol.ContainingNamespace, shader.Symbol.Name);
            }

            BoilerWriter.WriteLine(HlslShaderWriter.BoilerEndInitializer, Tab);
            BoilerWriter.WriteLine();

            foreach (var shader in ShaderClass.Shaders)
            {
                if (shader.VertexShaderDecleration == null || shader.FragmentShaderDecleration == null) continue;

                var compiled = shader.Compile();

                shader.TargetFile = Path.Combine(ShaderCompileDir, shader.Symbol.Name) + ".fx";

                File.WriteAllText(shader.TargetFile, compiled.Code);
                
                BoilerWriter.Write(compiled.Boilerplate);
                BoilerWriter.WriteLine();
            }
            File.WriteAllText(Path.Combine(SrcRoot, BoilerplateFileName), BoilerWriter.ToString());

            // Compile target shaders
            BuildGeneratedShaders();
        }

        static void BuildGeneratedShaders()
        {
            ContentBuilder contentBuilder = new ContentBuilder();

            contentBuilder.Clear();

            foreach (var shader in ShaderClass.Shaders)
            {
                if (shader.TargetFile == null) continue;

                contentBuilder.Add(shader.TargetFile, shader.Symbol.Name, "EffectImporter", "EffectProcessor");
            }

            // Empty the build directory
            Directory.CreateDirectory(ShaderBuildDir);
            foreach (var file in Directory.GetFiles(ShaderBuildDir))
            {
                File.Delete(file);
            }

            // Build the shaders
            string buildError = contentBuilder.Build();

            var files = Directory.GetFiles(contentBuilder.BuiltDirectory);

            foreach (var file in files)
            {
                string new_file = Path.Combine(ShaderBuildDir, Path.GetFileName(file));
                File.Copy(file, new_file);
            }
        }

        static void CompileUserCode()
        {
            // Get all the relevant source files
            var files = Directory.GetFiles(ProjRoot, "*.cs", SearchOption.AllDirectories);

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

            var Tree = Trees[0];
            var Root = Tree.GetRoot();
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

        static bool HasAttribute(MethodDeclarationSyntax method, string AttributeName)
        {
            return method.AttributeLists.Any(
              list => list.Attributes.Any(
                attribute => attribute.Name.ToString() == AttributeName));
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
   }
}
