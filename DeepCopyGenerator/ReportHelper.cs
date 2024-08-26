using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DeepCopyGenerator;

public static class ReportHelper
{
    private static readonly DiagnosticDescriptor MissingDefaultConstructorDescriptor =
        new (
            id: "MDCFDCAT01234",
            title: "Missing Default Constructor",
            messageFormat: "Type '{0}' must have a default (parameterless) constructor for deep copy purposes",
            category: "DeepCopy",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "All types marked with DeepCopyableAttribute need to have a default constructor defined."
        );
    
    private static readonly DiagnosticDescriptor MissingSetterForPropertyDescriptor =
        new (
            id: "MHPWNPSDC43210",
            title: "Has Property With No Public Setter",
            messageFormat: "Property '{0}' must have a public setter for deep copy purposes",
            category: "DeepCopy",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "All properties of type marked with DeepCopyableAttribute need to have public setter."
        );
    
    public static void ReportIfTypeDoNotHaveDefaultConstructor(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<TypeDeclarationSyntax> deepCopiableSyntaxes)
    {
        foreach (var deepCopiableSyntax in deepCopiableSyntaxes)
        {
            var semanticModel = compilation.GetSemanticModel(deepCopiableSyntax.SyntaxTree);
            var typeSymbol = semanticModel.GetDeclaredSymbol(deepCopiableSyntax);
            if (typeSymbol == null)
            {
                return;
            }

            if (typeSymbol.Constructors.All(ctor => ctor.Parameters.Length > 0))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    MissingDefaultConstructorDescriptor,
                    deepCopiableSyntax.GetLocation(),
                    deepCopiableSyntax.Identifier.ToString()));
            }
        }
    }
    
    public static void ReportIfTypeHasPropertiesWithoutPublicSetter(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<TypeDeclarationSyntax> deepCopiableSyntaxes)
    {
        foreach (var deepCopiableSyntax in deepCopiableSyntaxes)
        {
            var semanticModel = compilation.GetSemanticModel(deepCopiableSyntax.SyntaxTree);
            var typeSymbol = semanticModel.GetDeclaredSymbol(deepCopiableSyntax);
            if (typeSymbol == null)
            {
                return;
            }

            foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>())
            {
                var setMethod = property.SetMethod;
            
                if (setMethod == null || setMethod.IsInitOnly || setMethod.DeclaredAccessibility != Accessibility.Public)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        MissingSetterForPropertyDescriptor,
                        property.GetPropertyLocation(),
                        deepCopiableSyntax.Identifier.ToString()));
                }
            }
        }
    }
}