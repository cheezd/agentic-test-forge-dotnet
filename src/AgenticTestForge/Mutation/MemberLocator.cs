using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AgenticTestForge.Mutation;

internal static class MemberLocator
{
    public static string Find(string source, int line)
    {
        if (string.IsNullOrEmpty(source) || line < 1)
        {
            return "";
        }

        var tree = CSharpSyntaxTree.ParseText(source);
        var text = tree.GetText();
        if (line > text.Lines.Count)
        {
            return "";
        }

        var span = text.Lines[line - 1].Span;
        var node = tree.GetRoot().FindNode(span, getInnermostNodeForTie: true);
        var member = node.AncestorsAndSelf()
            .FirstOrDefault(static candidate => IsMember(candidate));
        return member is null ? "" : Qualified(member);
    }

    private static bool IsMember(SyntaxNode candidate)
    {
        if (candidate is VariableDeclaratorSyntax declarator)
        {
            return declarator.Parent?.Parent is BaseFieldDeclarationSyntax;
        }

        return candidate
            is MethodDeclarationSyntax
                or ConstructorDeclarationSyntax
                or DestructorDeclarationSyntax
                or OperatorDeclarationSyntax
                or ConversionOperatorDeclarationSyntax
                or LocalFunctionStatementSyntax
                or PropertyDeclarationSyntax
                or IndexerDeclarationSyntax
                or EventDeclarationSyntax
                or BaseFieldDeclarationSyntax;
    }

    private static string Qualified(SyntaxNode member)
    {
        var name = Name(member);
        var containers = member
            .Ancestors()
            .Select(ContainerName)
            .OfType<string>()
            .Where(static container => container.Length > 0)
            .Reverse();
        return string.Join('.', containers.Append(name).Where(static part => part.Length > 0));
    }

    private static string Name(SyntaxNode member) =>
        member switch
        {
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            ConstructorDeclarationSyntax constructor => constructor.Identifier.ValueText,
            DestructorDeclarationSyntax destructor => destructor.Identifier.ValueText,
            LocalFunctionStatementSyntax local => local.Identifier.ValueText,
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            IndexerDeclarationSyntax => "this",
            OperatorDeclarationSyntax op => op.OperatorToken.ValueText,
            ConversionOperatorDeclarationSyntax conversion => conversion
                .ImplicitOrExplicitKeyword
                .ValueText,
            EventDeclarationSyntax ev => ev.Identifier.ValueText,
            BaseFieldDeclarationSyntax field => field
                .Declaration.Variables.First()
                .Identifier.ValueText,
            VariableDeclaratorSyntax variable => variable.Identifier.ValueText,
            _ => "",
        };

    private static string? ContainerName(SyntaxNode node) =>
        node switch
        {
            BaseNamespaceDeclarationSyntax ns => ns.Name.ToString(),
            BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
            _ => null,
        };
}
