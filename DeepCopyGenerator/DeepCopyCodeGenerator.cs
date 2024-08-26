using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DeepCopyGenerator;

public static class DeepCopyCodeGenerator
{
    public static void GenerateDeepCopyAttribute(IncrementalGeneratorPostInitializationContext context)
    {
        var bitwiseArgumentOperation = SyntaxFactory.BinaryExpression(
            SyntaxKind.BitwiseOrExpression,
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("AttributeTargets"),
                SyntaxFactory.IdentifierName("Class")),
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("AttributeTargets"),
                SyntaxFactory.IdentifierName("Struct")));

        var attributeList = SyntaxFactory.AttributeList(
            SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(
                SyntaxFactory.ParseName("AttributeUsage"),
                SyntaxFactory.AttributeArgumentList([SyntaxFactory.AttributeArgument(bitwiseArgumentOperation)]))));

        var attributeSyntaxTree = SyntaxFactory.CompilationUnit()
            .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")))
            .AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("DeepCopyGenerator"))
                .AddMembers(
                    SyntaxFactory.ClassDeclaration("DeepCopiableAttribute")
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("Attribute")))
                        .AddAttributeLists(attributeList)));
        
        var attributeCode = attributeSyntaxTree.NormalizeWhitespace().ToFullString();
        context.AddSource("DeepCopiableAttribute.g.cs", attributeCode);
    }
    
    public static void GenerateClassesWithMethod(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<TypeDeclarationSyntax> deepCopiableSyntaxes)
    {
        foreach (var deepCopiableSyntax in deepCopiableSyntaxes)
        {
            var semanticModel = compilation.GetSemanticModel(deepCopiableSyntax.SyntaxTree);
            var typeSymbol = semanticModel.GetDeclaredSymbol(deepCopiableSyntax);

            var code = GenerateDeepCopyCodeForClass(typeSymbol);
            context.AddSource($"{typeSymbol!.Name}DeepCopy.g.cs", SourceText.From(code, Encoding.UTF8));
        }
    }

    private static string GenerateDeepCopyCodeForClass(INamedTypeSymbol typeSymbol)
    {
        var classSyntaxTree = SyntaxFactory.CompilationUnit()
            .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")))
            .AddMembers(
                SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(typeSymbol.ContainingNamespace.ToString()))
                    .AddMembers(
                        SyntaxFactory.ClassDeclaration(typeSymbol.Name)
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.PartialKeyword))
                            .AddMembers(
                                SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(typeSymbol.Name), "DeepCopy")
                                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                                    .WithBody(SyntaxFactory.Block(GenerateReturnCopiedObjectSyntaxTree(typeSymbol)))
                            )
                    )
            );

        return classSyntaxTree.NormalizeWhitespace().ToFullString();
        // * if property of reference type and not string -- think about circular reference...
        // * think about structs and records
        // * if property is of type implements IEnumerable - create new list and in foreach add objects
    }

    private static SyntaxList<StatementSyntax> GenerateReturnCopiedObjectSyntaxTree(INamedTypeSymbol typeSymbol)
    {
        var statements = new List<StatementSyntax>();

        var assignments = new List<ExpressionSyntax>();
        foreach (var propertySymbol in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            var assignment = SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(propertySymbol.Name),
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ThisExpression(),
                    SyntaxFactory.IdentifierName(propertySymbol.Name)));

            assignments.Add(assignment);
        }

        var objectCreation = SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.ParseTypeName(typeSymbol.Name))
            .WithInitializer(SyntaxFactory.InitializerExpression(
                SyntaxKind.ObjectInitializerExpression, (new SeparatedSyntaxList<ExpressionSyntax>()).InsertRange(0, assignments)));

        var returnStatement = SyntaxFactory.ReturnStatement(objectCreation);
        statements.Add(returnStatement);
        
        return (new SyntaxList<StatementSyntax>()).InsertRange(0, statements);
    }
}