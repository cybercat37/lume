using Lume.Compiler.Diagnostics;
using Lume.Compiler.Parsing;
using Lume.Compiler.Syntax;
using Lume.Compiler.Text;

namespace Lume.Compiler.Binding;

public sealed class Binder
{
    private readonly List<Diagnostic> diagnostics = new();
    private BoundScope scope = new(null);
    private SourceText? sourceText;

    public BinderResult Bind(SyntaxTree syntaxTree)
    {
        sourceText = syntaxTree.SourceText;
        var statements = new List<BoundStatement>();

        foreach (var statement in syntaxTree.Root.Statements)
        {
            statements.Add(BindStatement(statement));
        }

        return new BinderResult(new BoundProgram(statements), diagnostics);
    }

    private BoundStatement BindStatement(StatementSyntax statement)
    {
        switch (statement)
        {
            case BlockStatementSyntax block:
                return BindBlockStatement(block);
            case VariableDeclarationSyntax declaration:
                return BindVariableDeclaration(declaration);
            case PrintStatementSyntax print:
                return new BoundPrintStatement(BindExpression(print.Expression));
            case ExpressionStatementSyntax expressionStatement:
                return new BoundExpressionStatement(BindExpression(expressionStatement.Expression));
            default:
                return new BoundExpressionStatement(new BoundLiteralExpression(null));
        }
    }

    private BoundStatement BindBlockStatement(BlockStatementSyntax block)
    {
        var previousScope = scope;
        scope = new BoundScope(previousScope);
        var statements = new List<BoundStatement>();

        foreach (var statement in block.Statements)
        {
            statements.Add(BindStatement(statement));
        }

        scope = previousScope;
        return new BoundBlockStatement(statements);
    }

    private BoundStatement BindVariableDeclaration(VariableDeclarationSyntax declaration)
    {
        var name = declaration.IdentifierToken.Text;
        var isMutable = declaration.MutKeyword is not null;
        if (!string.IsNullOrEmpty(name))
        {
            var symbol = new VariableSymbol(name, isMutable);

            if (!scope.TryDeclare(symbol))
            {
                diagnostics.Add(Diagnostic.Error(
                    sourceText ?? new SourceText(string.Empty, string.Empty),
                    declaration.IdentifierToken.Span,
                    $"Variable '{name}' is already declared in this scope."));
            }
        }

        var initializer = BindExpression(declaration.Initializer);
        var declaredSymbol = scope.TryLookup(name) ?? new VariableSymbol(name, isMutable);
        return new BoundVariableDeclaration(declaredSymbol, initializer);
    }

    private BoundExpression BindExpression(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case LiteralExpressionSyntax literal:
                return new BoundLiteralExpression(literal.LiteralToken.Value);
            case NameExpressionSyntax name:
                return BindNameExpression(name);
            case AssignmentExpressionSyntax assignment:
                return BindAssignmentExpression(assignment);
            case BinaryExpressionSyntax binary:
                return new BoundBinaryExpression(
                    BindExpression(binary.Left),
                    BindExpression(binary.Right));
            case UnaryExpressionSyntax unary:
                return new BoundUnaryExpression(BindExpression(unary.Operand));
            case ParenthesizedExpressionSyntax parenthesized:
                return BindExpression(parenthesized.Expression);
            default:
                return new BoundLiteralExpression(null);
        }
    }

    private BoundExpression BindNameExpression(NameExpressionSyntax nameExpression)
    {
        var name = nameExpression.IdentifierToken.Text;
        var symbol = scope.TryLookup(name);
        if (symbol is null)
        {
            diagnostics.Add(Diagnostic.Error(
                sourceText ?? new SourceText(string.Empty, string.Empty),
                nameExpression.IdentifierToken.Span,
                $"Undefined variable '{name}'."));
            return new BoundLiteralExpression(null);
        }

        return new BoundNameExpression(symbol);
    }

    private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax assignment)
    {
        var name = assignment.IdentifierToken.Text;
        var symbol = scope.TryLookup(name);

        if (symbol is null)
        {
            diagnostics.Add(Diagnostic.Error(
                sourceText ?? new SourceText(string.Empty, string.Empty),
                assignment.IdentifierToken.Span,
                $"Undefined variable '{name}'."));
            return BindExpression(assignment.Expression);
        }

        if (!symbol.IsMutable)
        {
            diagnostics.Add(Diagnostic.Error(
                sourceText ?? new SourceText(string.Empty, string.Empty),
                assignment.IdentifierToken.Span,
                $"Variable '{name}' is immutable."));
        }

        var boundExpression = BindExpression(assignment.Expression);
        return new BoundAssignmentExpression(symbol, boundExpression);
    }
}
