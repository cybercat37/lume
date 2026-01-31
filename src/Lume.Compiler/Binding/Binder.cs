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
    private SourceText SourceText => sourceText ?? new SourceText(string.Empty, string.Empty);

    public BinderResult Bind(SyntaxTree syntaxTree)
    {
        sourceText = syntaxTree.SourceText;
        var statements = new List<BoundStatement>();

        foreach (var statement in syntaxTree.Root.Statements)
        {
            statements.Add(BindStatement(statement));
        }

        var allDiagnostics = syntaxTree.Diagnostics
            .Concat(diagnostics)
            .ToList();
        return new BinderResult(new BoundProgram(statements), allDiagnostics);
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
                throw new InvalidOperationException($"Unexpected statement: {statement.GetType().Name}");
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
        var initializer = BindExpression(declaration.Initializer);
        var type = initializer.Type;

        VariableSymbol? declaredSymbol = null;
        if (!string.IsNullOrEmpty(name))
        {
            var symbol = new VariableSymbol(name, isMutable, type);
            declaredSymbol = scope.TryDeclare(symbol);
            if (declaredSymbol is null)
            {
                diagnostics.Add(Diagnostic.Error(
                    SourceText,
                    declaration.IdentifierToken.Span,
                    $"Variable '{name}' is already declared in this scope."));
            }
        }

        declaredSymbol ??= new VariableSymbol(name, isMutable, type);
        return new BoundVariableDeclaration(declaredSymbol, initializer);
    }

    private BoundExpression BindExpression(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case LiteralExpressionSyntax literal:
                return BindLiteralExpression(literal);
            case NameExpressionSyntax name:
                return BindNameExpression(name);
            case AssignmentExpressionSyntax assignment:
                return BindAssignmentExpression(assignment);
            case BinaryExpressionSyntax binary:
                return BindBinaryExpression(binary);
            case UnaryExpressionSyntax unary:
                return BindUnaryExpression(unary);
            case ParenthesizedExpressionSyntax parenthesized:
                return BindExpression(parenthesized.Expression);
            case InputExpressionSyntax:
                return new BoundInputExpression();
            default:
                throw new InvalidOperationException($"Unexpected expression: {expression.GetType().Name}");
        }
    }

    private BoundExpression BindLiteralExpression(LiteralExpressionSyntax literal)
    {
        var value = literal.LiteralToken.Value;
        if (value is int)
        {
            return new BoundLiteralExpression(value, TypeSymbol.Int);
        }

        if (value is bool)
        {
            return new BoundLiteralExpression(value, TypeSymbol.Bool);
        }

        if (value is string)
        {
            return new BoundLiteralExpression(value, TypeSymbol.String);
        }

        return new BoundLiteralExpression(value, TypeSymbol.Error);
    }

    private BoundExpression BindBinaryExpression(BinaryExpressionSyntax binary)
    {
        var left = BindExpression(binary.Left);
        var right = BindExpression(binary.Right);

        if (left.Type == TypeSymbol.Int && right.Type == TypeSymbol.Int)
        {
            return new BoundBinaryExpression(left, binary.OperatorToken.Kind, right, TypeSymbol.Int);
        }

        if (left.Type != TypeSymbol.Error && right.Type != TypeSymbol.Error)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                binary.OperatorToken.Span,
                $"Operator '{binary.OperatorToken.Text}' is not defined for types '{left.Type}' and '{right.Type}'."));
        }

        return new BoundBinaryExpression(left, binary.OperatorToken.Kind, right, TypeSymbol.Error);
    }

    private BoundExpression BindUnaryExpression(UnaryExpressionSyntax unary)
    {
        var operand = BindExpression(unary.Operand);
        if (operand.Type == TypeSymbol.Int)
        {
            return new BoundUnaryExpression(operand, unary.OperatorToken.Kind, TypeSymbol.Int);
        }

        if (operand.Type != TypeSymbol.Error)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                unary.OperatorToken.Span,
                $"Operator '{unary.OperatorToken.Text}' is not defined for type '{operand.Type}'."));
        }

        return new BoundUnaryExpression(operand, unary.OperatorToken.Kind, TypeSymbol.Error);
    }

    private BoundExpression BindNameExpression(NameExpressionSyntax nameExpression)
    {
        var name = nameExpression.IdentifierToken.Text;
        var symbol = scope.TryLookup(name);
        if (symbol is null)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                nameExpression.IdentifierToken.Span,
                $"Undefined variable '{name}'."));
            return new BoundLiteralExpression(null, TypeSymbol.Error);
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
                SourceText,
                assignment.IdentifierToken.Span,
                $"Undefined variable '{name}'."));
            return BindExpression(assignment.Expression);
        }

        if (!symbol.IsMutable)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                assignment.IdentifierToken.Span,
                $"Variable '{name}' is immutable."));
        }

        var boundExpression = BindExpression(assignment.Expression);
        if (boundExpression.Type != symbol.Type && boundExpression.Type != TypeSymbol.Error && symbol.Type != TypeSymbol.Error)
        {
            diagnostics.Add(Diagnostic.Error(
                SourceText,
                assignment.IdentifierToken.Span,
                $"Cannot assign expression of type '{boundExpression.Type}' to variable of type '{symbol.Type}'."));
        }

        return new BoundAssignmentExpression(symbol, boundExpression);
    }
}
