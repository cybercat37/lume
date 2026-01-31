using Lume.Compiler.Diagnostics;
using Lume.Compiler.Lexing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

namespace Lume.Compiler.Parsing;

public sealed class SyntaxTree
{
    public SourceText SourceText { get; }
    public CompilationUnitSyntax Root { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    private SyntaxTree(SourceText sourceText, CompilationUnitSyntax root, IReadOnlyList<Diagnostic> diagnostics)
    {
        SourceText = sourceText;
        Root = root;
        Diagnostics = diagnostics;
    }

    public static SyntaxTree Parse(SourceText sourceText)
    {
        var lexer = new Lexer(sourceText);
        var tokens = new List<SyntaxToken>();

        SyntaxToken token;
        do
        {
            token = lexer.Lex();
            if (token.Kind != TokenKind.BadToken)
            {
                tokens.Add(token);
            }
        } while (token.Kind != TokenKind.EndOfFile);

        var parser = new Parser(sourceText, tokens);
        var root = parser.ParseCompilationUnit();

        var diagnostics = lexer.Diagnostics
            .Concat(parser.Diagnostics)
            .ToList();

        return new SyntaxTree(sourceText, root, diagnostics);
    }

    public static SyntaxTree ParseCached(SourceText sourceText, SyntaxTreeCache cache)
    {
        if (cache.TryGet(sourceText.Text, out var cached) && cached is not null)
        {
            return cached;
        }

        var tree = Parse(sourceText);
        cache.Store(sourceText.Text, tree);
        return tree;
    }
}
