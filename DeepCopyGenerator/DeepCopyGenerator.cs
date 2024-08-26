using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DeepCopyGenerator;

[Generator]
public class DeepCopyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }
#endif
        context.RegisterPostInitializationOutput(DeepCopyCodeGenerator.GenerateDeepCopyAttribute);
        
        var deepCopyableTypeNodes = context.SyntaxProvider.CreateSyntaxProvider(
                (node, _) => IsTypeDeclarationAndHasDeepCopiableAttribute(node),
                (ctx, _) => (TypeDeclarationSyntax)ctx.Node);

        var compilationWithDeepCopiableTypeDeclarations = context.CompilationProvider
            .Combine(deepCopyableTypeNodes.Collect());
        
        context.RegisterSourceOutput(compilationWithDeepCopiableTypeDeclarations, (ctx, compilationWithDcTypes) =>
        {
            ReportHelper.ReportIfTypeDoNotHaveDefaultConstructor(ctx, compilationWithDcTypes.Left, compilationWithDcTypes.Right);
            ReportHelper.ReportIfTypeHasPropertiesWithoutPublicSetter(ctx, compilationWithDcTypes.Left, compilationWithDcTypes.Right);
            DeepCopyCodeGenerator.GenerateClassesWithMethod(ctx, compilationWithDcTypes.Left, compilationWithDcTypes.Right);
        });
    }

    private static bool IsTypeDeclarationAndHasDeepCopiableAttribute(SyntaxNode syntaxNode)
    {
        if (syntaxNode is not (ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax))
        {
            return false;
        }

        var typeDeclarationNodeInfo = (TypeDeclarationSyntax)syntaxNode;
        return typeDeclarationNodeInfo.AttributeLists
            .ToList()
            .Any(al => al.Attributes.Any(attribute => attribute.Name.ToString() == "DeepCopiable"));
    }
}