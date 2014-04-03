using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace TransformationCS
{
    public class TypeInferenceRewriter : SyntaxRewriter
    {
        private readonly SemanticModel SemanticModel;

        public TypeInferenceRewriter(SemanticModel semanticModel)
        {
            this.SemanticModel = semanticModel;
        }

        public override SyntaxNode VisitLocalDeclarationStatement(
                                          LocalDeclarationStatementSyntax node)
        {
            if (node.Declaration.Variables.Count > 1)
            {
                return node;
            }
            if (node.Declaration.Variables[0].Initializer == null)
            {
                return node;
            }

            VariableDeclaratorSyntax declarator = node.Declaration.Variables.First(); TypeSyntax variableTypeName = node.Declaration.Type;

            TypeSymbol variableType =
                           (TypeSymbol)SemanticModel.GetSymbolInfo(variableTypeName)
                                                    .Symbol;

            TypeInfo initializerInfo =
                         SemanticModel.GetTypeInfo(declarator
                                                   .Initializer
                                                   .Value);

            if (variableType == initializerInfo.Type)
            {
                TypeSyntax varTypeName =
                               Syntax.IdentifierName("var")
                                     .WithLeadingTrivia(
                                          variableTypeName.GetLeadingTrivia())
                                     .WithTrailingTrivia(
                                          variableTypeName.GetTrailingTrivia());

                return node.ReplaceNode(variableTypeName, varTypeName);
            }
            else
            {
                return node;
            }
        }
    }
}