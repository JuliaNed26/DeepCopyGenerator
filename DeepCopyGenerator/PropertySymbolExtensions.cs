using System.Linq;
using Microsoft.CodeAnalysis;

namespace DeepCopyGenerator;

public static class PropertySymbolExtensions
{
    public static Location GetPropertyLocation(this IPropertySymbol propertySymbol)
    {
        return Enumerable.FirstOrDefault(propertySymbol.Locations, location => location.IsInSource);
    }
}