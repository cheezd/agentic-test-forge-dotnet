using AgenticTestForge.Reporting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AgenticTestForge.Dry;

internal sealed record MethodUnit(
    string QualifiedName,
    string Filepath,
    int Lines,
    int Nodes,
    IReadOnlySet<string> Fingerprints
);

internal static class JaccardSimilarity
{
    public const double Threshold = 0.82;
    public const int MinLines = 4;
    public const int MinNodes = 20;

    public static IReadOnlyList<MethodUnit> Read(string filepath, string source)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        var units = new List<MethodUnit>();
        foreach (var node in root.DescendantNodes())
        {
            var unit = Unit(node, filepath);
            if (unit is not null)
            {
                units.Add(unit);
            }
        }

        return units;
    }

    public static IReadOnlyList<DryFinding> Score(IReadOnlyList<MethodUnit> units)
    {
        var eligible = units
            .Where(static unit => unit.Lines >= MinLines && unit.Nodes >= MinNodes)
            .ToArray();
        var findings = new List<DryFinding>();
        for (var left = 0; left < eligible.Length; left++)
        {
            for (var right = left + 1; right < eligible.Length; right++)
            {
                var finding = Pair(eligible[left], eligible[right]);
                if (finding is not null)
                {
                    findings.Add(finding);
                }
            }
        }

        return findings
            .OrderByDescending(static finding => finding.Similarity)
            .ThenBy(static finding => finding.Filepath, StringComparer.Ordinal)
            .ThenBy(static finding => finding.QualifiedName, StringComparer.Ordinal)
            .ToArray();
    }

    private static MethodUnit? Unit(SyntaxNode node, string filepath)
    {
        var body = Body(node);
        if (body is null)
        {
            return null;
        }

        var normalized = Normalize(body);
        var lines = node.GetLocation().GetLineSpan();
        return new MethodUnit(
            Qualified(node),
            filepath,
            lines.EndLinePosition.Line - lines.StartLinePosition.Line + 1,
            normalized.DescendantNodesAndSelf().Count(),
            Fingerprints(normalized)
        );
    }

    private static SyntaxNode? Body(SyntaxNode node) =>
        node switch
        {
            MethodDeclarationSyntax method => method.Body
                ?? (SyntaxNode?)method.ExpressionBody?.Expression,
            LocalFunctionStatementSyntax local => local.Body
                ?? (SyntaxNode?)local.ExpressionBody?.Expression,
            _ => null,
        };

    private static SyntaxNode Normalize(SyntaxNode body) => new Normalizer().Visit(body)!;

    private static HashSet<string> Fingerprints(SyntaxNode body)
    {
        var fingerprints = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in body.DescendantNodesAndSelf())
        {
            fingerprints.Add(Dump(node));
        }

        return fingerprints;
    }

    private static string Dump(SyntaxNode node)
    {
        var parts = new List<string> { node.Kind().ToString() };
        foreach (var child in node.ChildNodesAndTokens())
        {
            parts.Add(child.IsNode ? Dump(child.AsNode()!) : Token(child.AsToken()));
        }

        return string.Join(' ', parts);
    }

    private static string Token(SyntaxToken token) =>
        token.IsKind(SyntaxKind.IdentifierToken) || token.IsKind(SyntaxKind.StringLiteralToken)
            ? token.ValueText
            : token.Kind().ToString();

    private static DryFinding? Pair(MethodUnit left, MethodUnit right)
    {
        var score = Jaccard(left.Fingerprints, right.Fingerprints);
        if (score < Threshold)
        {
            return null;
        }

        var ordered = Order(left, right);
        return new DryFinding(
            true,
            null,
            ordered.Left.Filepath,
            ordered.Left.QualifiedName,
            ordered.Right.Filepath,
            ordered.Right.QualifiedName,
            JaccardDrySource.IdValue,
            Math.Round(score, 2, MidpointRounding.AwayFromZero)
        );
    }

    private static double Jaccard(IReadOnlySet<string> left, IReadOnlySet<string> right)
    {
        if (left.Count == 0 && right.Count == 0)
        {
            return 1;
        }

        var intersection = left.Count(right.Contains);
        var union = left.Count + right.Count - intersection;
        return union == 0 ? 1 : (double)intersection / union;
    }

    private static (MethodUnit Left, MethodUnit Right) Order(MethodUnit left, MethodUnit right)
    {
        var leftKey = left.Filepath + "\n" + left.QualifiedName;
        var rightKey = right.Filepath + "\n" + right.QualifiedName;
        return string.Compare(leftKey, rightKey, StringComparison.Ordinal) <= 0
            ? (left, right)
            : (right, left);
    }

    private static string Qualified(SyntaxNode member)
    {
        var names = new List<string>();
        for (var node = member.Parent; node is not null; node = node.Parent)
        {
            var name = Container(node);
            if (name is not null)
            {
                names.Add(name);
            }
        }

        names.Reverse();
        names.Add(Name(member));
        return string.Join('.', names.Where(static name => name.Length > 0));
    }

    private static string? Container(SyntaxNode node) =>
        node switch
        {
            BaseNamespaceDeclarationSyntax space => space.Name.ToString(),
            BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            LocalFunctionStatementSyntax local => local.Identifier.ValueText,
            _ => null,
        };

    private static string Name(SyntaxNode member) =>
        member switch
        {
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            LocalFunctionStatementSyntax local => local.Identifier.ValueText,
            _ => "",
        };

    private sealed class Normalizer : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitLiteralExpression(LiteralExpressionSyntax node) =>
            node.Kind() switch
            {
                SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression =>
                    SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression),
                SyntaxKind.NumericLiteralExpression => Number(node),
                SyntaxKind.StringLiteralExpression or SyntaxKind.Utf8StringLiteralExpression =>
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal("_str_")
                    ),
                SyntaxKind.CharacterLiteralExpression => SyntaxFactory.LiteralExpression(
                    SyntaxKind.CharacterLiteralExpression,
                    SyntaxFactory.Literal('_')
                ),
                _ => base.VisitLiteralExpression(node),
            };

        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            var visited = base.VisitToken(token);
            if (!visited.IsKind(SyntaxKind.IdentifierToken))
            {
                return visited;
            }

            return SyntaxFactory.Identifier("_");
        }

        private static LiteralExpressionSyntax Number(LiteralExpressionSyntax node)
        {
            var real = node.Token.Value is double or float or decimal;
            var token = real ? SyntaxFactory.Literal(0.0) : SyntaxFactory.Literal(0);
            return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, token);
        }
    }
}
