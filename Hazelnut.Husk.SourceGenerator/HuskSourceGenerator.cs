using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Hazelnut.Husk.SourceGenerator;

[Generator]
public class HuskSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<TypeDeclarationSyntax> typeDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)
            )
            .Where(static m => m is not null)!;

        IncrementalValueProvider<(Compilation, ImmutableArray<TypeDeclarationSyntax>)> compilationAndTypes
            = context.CompilationProvider.Combine(typeDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndTypes,
            static (spc, source) => Execute(source.Item1, source.Item2, spc));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is TypeDeclarationSyntax { AttributeLists.Count: > 0 }
            and (ClassDeclarationSyntax or StructDeclarationSyntax);
    }
    
    private static TypeDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var typeDeclarationSyntax = (TypeDeclarationSyntax)context.Node;

        var argumentSerializableAttribute = typeDeclarationSyntax.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(attr => attr.Name.ToString() is "ArgumentSerializable" or "ArgumentSerializableAttribute");

        if (argumentSerializableAttribute == null)
            return null;

        bool generateParserSource =
            !(argumentSerializableAttribute.ArgumentList?.Arguments.Count > 0 &&
              argumentSerializableAttribute.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal &&
              literal.Token.IsKind(SyntaxKind.FalseKeyword));

        return generateParserSource
            ? typeDeclarationSyntax
            : null;
    }
    
    private static void Execute(Compilation compilation, ImmutableArray<TypeDeclarationSyntax> types,
        SourceProductionContext context)
    {
        if (types.IsDefaultOrEmpty)
            return;

        var processedTypeNames = new HashSet<string>();

        foreach (var typeDeclarationSyntax in types)
        {
            var semanticModel = compilation.GetSemanticModel(typeDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(typeDeclarationSyntax) is not { } typeSymbol)
                continue;

            var typeName = typeSymbol.Name;
            if (!processedTypeNames.Add(typeName))
                continue;

            var namespaceName = typeSymbol.ContainingNamespace.ToDisplayString();

            bool needToMakeDefaultConstructor = typeSymbol.Constructors.Length > 0;

            var arguments = new List<ArgumentInfo>();
            foreach (var member in typeSymbol.GetMembers())
            {
                if (member is not IPropertySymbol and not IFieldSymbol)
                    continue;

                var attributes = member.GetAttributes();
                var argumentAttribute = attributes.FirstOrDefault(a =>
                    a.AttributeClass?.ToDisplayString() is "Hazelnut.Husk.ArgumentAttribute" or "ArgumentAttribute");

                if (argumentAttribute == null)
                    continue;

                var memberName = member.Name;
                var memberType = member switch
                {
                    IPropertySymbol property => property.Type.ToDisplayString(),
                    IFieldSymbol field => field.Type.ToDisplayString(),
                    _ => throw new InvalidOperationException("Unexpected member type")
                };

                bool isCollection = IsCollectionType(member);

                var argumentInfo = new ArgumentInfo
                {
                    Name = memberName,
                    Type = memberType,
                    IsCollection = isCollection,
                    IsEnum = IsEnumType(member),
                    IsNullable = IsNullableType(member)
                };

                foreach (var namedArgument in argumentAttribute.NamedArguments)
                {
                    switch (namedArgument.Key)
                    {
                        case "LongName":
                            argumentInfo.LongName = namedArgument.Value.Value?.ToString();
                            break;
                        case "ShortName":
                            argumentInfo.ShortName = namedArgument.Value.Value?.ToString();
                            break;
                        case "Order":
                            if (namedArgument.Value.Value is int orderValue)
                                argumentInfo.Order = orderValue;
                            break;
                        case "IsRequired":
                            if (namedArgument.Value.Value is bool requiredValue)
                                argumentInfo.IsRequired = requiredValue;
                            break;
                        case "IgnoreCaseLongName":
                            if (namedArgument.Value.Value is bool ignoreCaseLongNameValue)
                                argumentInfo.IgnoreCaseLongName = ignoreCaseLongNameValue;
                            break;
                        case "IgnoreCaseShortName":
                            if (namedArgument.Value.Value is bool ignoreCaseShortNameValue)
                                argumentInfo.IgnoreCaseShortName = ignoreCaseShortNameValue;
                            break;
                    }
                }

                if (argumentInfo.IsEnum)
                {
                    argumentInfo.EnumType = GetEnumType(member);
                }

                if (argumentInfo.IsCollection)
                {
                    argumentInfo.ElementType = GetElementType(member);
                }

                arguments.Add(argumentInfo);
            }

            if (arguments.Count <= 0)
                continue;
            
            var sourceCode = ParseConstructorGenerator.GenerateParser(namespaceName, typeDeclarationSyntax.Keyword.Text, typeName,
                arguments, needToMakeDefaultConstructor);
            context.AddSource($"{typeName}.Parser.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
        }
    }

    private static bool IsCollectionType(ISymbol member)
    {
        var type = GetMemberType(member);
        if (type.SpecialType == SpecialType.System_String)
            return false;

        return type.AllInterfaces.Any(i => i.ToDisplayString() == "System.Collections.IEnumerable") &&
               type.ToDisplayString() != "System.Object";
    }

    private static bool IsEnumType(ISymbol member)
    {
        var type = GetMemberType(member);
        return type.TypeKind == TypeKind.Enum ||
               (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
                ((INamedTypeSymbol)type).TypeArguments[0].TypeKind == TypeKind.Enum);
    }

    private static bool IsNullableType(ISymbol member)
    {
        var type = GetMemberType(member);
        return type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T ||
               type.NullableAnnotation == NullableAnnotation.Annotated;
    }

    private static string GetEnumType(ISymbol member)
    {
        var type = GetMemberType(member);
        if (type.TypeKind == TypeKind.Enum)
            return type.ToDisplayString();

        return type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            ? ((INamedTypeSymbol)type).TypeArguments[0].ToDisplayString()
            : string.Empty;
    }

    private static string GetElementType(ISymbol member)
    {
        var type = GetMemberType(member);

        return type switch
        {
            IArrayTypeSymbol arrayType => arrayType.ElementType.ToDisplayString(),
            INamedTypeSymbol { IsGenericType: true } namedType => namedType.TypeArguments[0].ToDisplayString(),
            _ => "object"
        };
    }

    private static ITypeSymbol GetMemberType(ISymbol member)
    {
        return member switch
        {
            IPropertySymbol property => property.Type,
            IFieldSymbol field => field.Type,
            _ => throw new InvalidOperationException($"지원되지 않는 멤버 타입: {member.GetType().Name}")
        };
    }
}