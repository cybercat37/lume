using System;
using System.Collections.Generic;
using System.Linq;
using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class Lowerer
{
    private int tempIndex;

    public LoweredProgram Lower(BoundProgram program)
    {
        var functions = program.Functions.Select(LowerFunction).ToList();
        var statements = program.Statements.Select(LowerStatement).ToList();
        return new LoweredProgram(
            program,
            program.RecordTypes,
            program.SumTypes,
            functions,
            statements);
    }

    private LoweredFunctionDeclaration LowerFunction(BoundFunctionDeclaration function)
    {
        var body = LowerBlockStatement(function.Body);
        return new LoweredFunctionDeclaration(function.Symbol, function.Parameters, body);
    }

    private LoweredStatement LowerStatement(BoundStatement statement)
    {
        return statement switch
        {
            BoundBlockStatement block => LowerBlockStatement(block),
            BoundVariableDeclaration declaration => new LoweredVariableDeclaration(
                declaration.Symbol,
                LowerExpression(declaration.Initializer)),
            BoundDeconstructionStatement deconstruction => LowerDeconstructionStatement(deconstruction),
            BoundPrintStatement print => new LoweredPrintStatement(LowerExpression(print.Expression)),
            BoundExpressionStatement expressionStatement => new LoweredExpressionStatement(
                LowerExpression(expressionStatement.Expression)),
            BoundReturnStatement returnStatement => LowerReturnStatement(returnStatement),
            _ => throw new InvalidOperationException($"Unexpected statement: {statement.GetType().Name}")
        };
    }

    private LoweredBlockStatement LowerBlockStatement(BoundBlockStatement block)
    {
        var lowered = block.Statements.Select(LowerStatement).ToList();
        return new LoweredBlockStatement(lowered);
    }

    private LoweredStatement LowerDeconstructionStatement(BoundDeconstructionStatement deconstruction)
    {
        var valueTemp = NewTemp(deconstruction.Initializer.Type);
        var valueName = new LoweredNameExpression(valueTemp);
        var patternResult = LowerPattern(deconstruction.Pattern, valueName);

        var statements = new List<LoweredStatement>
        {
            new LoweredVariableDeclaration(valueTemp, LowerExpression(deconstruction.Initializer))
        };

        var thenBlock = new LoweredBlockStatement(patternResult.Bindings);
        var elseBlock = new LoweredBlockStatement(new List<LoweredStatement>
        {
            new LoweredExpressionStatement(new LoweredMatchFailureExpression(TypeSymbol.Unit))
        });

        statements.Add(new LoweredIfStatement(patternResult.Condition, thenBlock, elseBlock));
        return new LoweredBlockStatement(statements);
    }

    private LoweredStatement LowerReturnStatement(BoundReturnStatement returnStatement)
    {
        if (returnStatement.Expression is BoundMatchExpression match)
        {
            return LowerMatchReturnStatement(match);
        }

        return new LoweredReturnStatement(returnStatement.Expression is null
            ? null
            : LowerExpression(returnStatement.Expression));
    }

    private LoweredExpression LowerExpression(BoundExpression expression)
    {
        return expression switch
        {
            BoundLiteralExpression literal => new LoweredLiteralExpression(literal.Value, literal.Type),
            BoundNameExpression name => new LoweredNameExpression(name.Symbol),
            BoundAssignmentExpression assignment => new LoweredAssignmentExpression(
                assignment.Symbol,
                LowerExpression(assignment.Expression)),
            BoundUnaryExpression unary => new LoweredUnaryExpression(
                unary.OperatorKind,
                LowerExpression(unary.Operand),
                unary.Type),
            BoundBinaryExpression binary => new LoweredBinaryExpression(
                LowerExpression(binary.Left),
                binary.OperatorKind,
                LowerExpression(binary.Right),
                binary.Type),
            BoundInputExpression => new LoweredInputExpression(),
            BoundCallExpression call => new LoweredCallExpression(
                LowerExpression(call.Callee),
                call.Arguments.Select(LowerExpression).ToList(),
                call.Type),
            BoundFunctionExpression function => new LoweredFunctionExpression(function.Function),
            BoundLambdaExpression lambda => new LoweredLambdaExpression(
                lambda.Parameters,
                LowerBlockStatement(lambda.Body),
                lambda.Captures,
                lambda.FunctionType),
            BoundMatchExpression match => LowerMatchExpression(match),
            BoundTupleExpression tuple => new LoweredTupleExpression(
                tuple.Elements.Select(LowerExpression).ToList(),
                tuple.Type),
            BoundListExpression list => new LoweredListExpression(
                list.Elements.Select(LowerExpression).ToList(),
                list.Type),
            BoundIndexExpression index => new LoweredIndexExpression(
                LowerExpression(index.Target),
                LowerExpression(index.Index),
                index.Type),
            BoundMapExpression map => new LoweredMapExpression(
                map.Entries.Select(entry => new LoweredMapEntry(
                    LowerExpression(entry.Key),
                    LowerExpression(entry.Value))).ToList(),
                map.Type),
            BoundQuestionExpression question => LowerQuestionExpression(question),
            BoundUnwrapExpression unwrap => new LoweredUnwrapExpression(
                LowerExpression(unwrap.Expression),
                unwrap.FailureVariant,
                unwrap.Type),
            BoundRecordLiteralExpression record => new LoweredRecordLiteralExpression(
                record.RecordType,
                record.Fields.Select(field => new LoweredRecordFieldAssignment(
                    field.Field,
                    LowerExpression(field.Expression))).ToList()),
            BoundFieldAccessExpression fieldAccess => new LoweredFieldAccessExpression(
                LowerExpression(fieldAccess.Target),
                fieldAccess.Field),
            BoundSumConstructorExpression sum => new LoweredSumConstructorExpression(
                sum.Variant,
                sum.Payload is null ? null : LowerExpression(sum.Payload)),
            _ => throw new InvalidOperationException($"Unexpected expression: {expression.GetType().Name}")
        };
    }

    private LoweredExpression LowerMatchExpression(BoundMatchExpression match)
    {
        var valueTemp = NewTemp(match.Expression.Type);
        var resultTemp = NewTemp(match.Type);

        var statements = new List<LoweredStatement>
        {
            new LoweredVariableDeclaration(valueTemp, LowerExpression(match.Expression)),
            new LoweredVariableDeclaration(resultTemp, new LoweredDefaultExpression(match.Type))
        };

        var matchValue = new LoweredNameExpression(valueTemp);

        LoweredStatement elseStatement = new LoweredExpressionStatement(
            new LoweredAssignmentExpression(resultTemp, new LoweredMatchFailureExpression(match.Type)));

        for (var i = match.Arms.Count - 1; i >= 0; i--)
        {
            var arm = match.Arms[i];
            var loweredArmExpression = LowerExpression(arm.Expression);
            var patternResult = LowerPattern(arm.Pattern, matchValue);

            var assignStatement = new LoweredExpressionStatement(
                new LoweredAssignmentExpression(resultTemp, loweredArmExpression));

            LoweredStatement armBody = new LoweredBlockStatement(new List<LoweredStatement> { assignStatement });
            if (arm.Guard is not null)
            {
                var guardCondition = LowerExpression(arm.Guard);
                armBody = new LoweredIfStatement(guardCondition, armBody, elseStatement);
            }

            var armStatements = new List<LoweredStatement>();
            armStatements.AddRange(patternResult.Bindings);
            armStatements.Add(armBody);

            var armBlock = new LoweredBlockStatement(armStatements);
            elseStatement = new LoweredIfStatement(patternResult.Condition, armBlock, elseStatement);
        }

        statements.Add(elseStatement);

        return new LoweredBlockExpression(statements, new LoweredNameExpression(resultTemp));
    }

    private LoweredExpression LowerQuestionExpression(BoundQuestionExpression question)
    {
        var valueTemp = NewTemp(question.Expression.Type);
        var tempName = new LoweredNameExpression(valueTemp);

        var statements = new List<LoweredStatement>
        {
            new LoweredVariableDeclaration(valueTemp, LowerExpression(question.Expression))
        };

        var failureTag = new LoweredLiteralExpression(question.FailureVariant.Name, TypeSymbol.String);
        var isFailure = new LoweredBinaryExpression(
            new LoweredSumTagExpression(tempName),
            Lexing.TokenKind.EqualEqual,
            failureTag,
            TypeSymbol.Bool);

        LoweredExpression? failurePayload = question.FailureVariant.PayloadType is null
            ? null
            : new LoweredSumValueExpression(tempName, question.FailureVariant.PayloadType);

        var failureReturn = new LoweredReturnStatement(new LoweredSumConstructorExpression(
            question.FailureVariant,
            failurePayload));

        statements.Add(new LoweredIfStatement(isFailure, failureReturn, null));

        var successPayloadType = question.SuccessVariant.PayloadType ?? TypeSymbol.Error;
        var successValue = new LoweredSumValueExpression(tempName, successPayloadType);
        return new LoweredBlockExpression(statements, successValue);
    }

    private LoweredStatement LowerMatchReturnStatement(BoundMatchExpression match)
    {
        var valueTemp = NewTemp(match.Expression.Type);
        var statements = new List<LoweredStatement>
        {
            new LoweredVariableDeclaration(valueTemp, LowerExpression(match.Expression))
        };

        var matchValue = new LoweredNameExpression(valueTemp);

        LoweredStatement elseStatement = new LoweredBlockStatement(new List<LoweredStatement>
        {
            new LoweredReturnStatement(new LoweredMatchFailureExpression(match.Type))
        });

        for (var i = match.Arms.Count - 1; i >= 0; i--)
        {
            var arm = match.Arms[i];
            var patternResult = LowerPattern(arm.Pattern, matchValue);

            var armStatements = new List<LoweredStatement>();
            armStatements.AddRange(patternResult.Bindings);

            LoweredStatement armBody;
            if (arm.Expression is BoundMatchExpression nestedMatch)
            {
                armBody = LowerMatchReturnStatement(nestedMatch);
            }
            else
            {
                armBody = new LoweredReturnStatement(LowerExpression(arm.Expression));
            }

            if (arm.Guard is not null)
            {
                var guardCondition = LowerExpression(arm.Guard);
                armBody = new LoweredIfStatement(guardCondition, armBody, elseStatement);
            }

            armStatements.Add(armBody);

            var armBlock = new LoweredBlockStatement(armStatements);
            elseStatement = new LoweredIfStatement(patternResult.Condition, armBlock, elseStatement);
        }

        statements.Add(elseStatement);
        return new LoweredBlockStatement(statements);
    }

    private PatternLoweringResult LowerPattern(BoundPattern pattern, LoweredExpression value)
    {
        switch (pattern)
        {
            case BoundWildcardPattern:
                return PatternLoweringResult.MatchAlways();
            case BoundLiteralPattern literal:
                return new PatternLoweringResult(
                    new LoweredBinaryExpression(
                        value,
                        Lexing.TokenKind.EqualEqual,
                        new LoweredLiteralExpression(literal.Value, literal.Type),
                        TypeSymbol.Bool));
            case BoundIdentifierPattern identifier:
                return new PatternLoweringResult(
                    PatternLoweringResult.TrueLiteral,
                    new List<LoweredStatement>
                    {
                        new LoweredVariableDeclaration(identifier.Symbol, value)
                    });
            case BoundTuplePattern tuple:
                return LowerTuplePattern(tuple, value);
            case BoundVariantPattern variant:
                return LowerVariantPattern(variant, value);
            case BoundRecordPattern record:
                return LowerRecordPattern(record, value);
            default:
                return PatternLoweringResult.MatchAlways();
        }
    }

    private PatternLoweringResult LowerTuplePattern(BoundTuplePattern tuple, LoweredExpression value)
    {
        LoweredExpression condition = new LoweredIsTupleExpression(value, tuple.Type);
        var bindings = new List<LoweredStatement>();

        for (var i = 0; i < tuple.Elements.Count; i++)
        {
            var elementPattern = tuple.Elements[i];
            var elementValue = new LoweredTupleAccessExpression(value, i, elementPattern.Type);
            var elementResult = LowerPattern(elementPattern, elementValue);
            condition = new LoweredBinaryExpression(condition, Lexing.TokenKind.AmpersandAmpersand, elementResult.Condition, TypeSymbol.Bool);
            bindings.AddRange(elementResult.Bindings);
        }

        return new PatternLoweringResult(condition, bindings);
    }

    private PatternLoweringResult LowerVariantPattern(BoundVariantPattern variant, LoweredExpression value)
    {
        var condition = new LoweredBinaryExpression(
            new LoweredIsSumExpression(value, variant.Type),
            Lexing.TokenKind.AmpersandAmpersand,
            new LoweredBinaryExpression(
                new LoweredSumTagExpression(value),
                Lexing.TokenKind.EqualEqual,
                new LoweredLiteralExpression(variant.Variant.Name, TypeSymbol.String),
                TypeSymbol.Bool),
            TypeSymbol.Bool);

        var bindings = new List<LoweredStatement>();

        if (variant.Payload is null)
        {
            return new PatternLoweringResult(condition, bindings);
        }

        var payloadValue = new LoweredSumValueExpression(value, variant.Payload.Type);
        var payloadResult = LowerPattern(variant.Payload, payloadValue);
        condition = new LoweredBinaryExpression(condition, Lexing.TokenKind.AmpersandAmpersand, payloadResult.Condition, TypeSymbol.Bool);
        bindings.AddRange(payloadResult.Bindings);
        return new PatternLoweringResult(condition, bindings);
    }

    private PatternLoweringResult LowerRecordPattern(BoundRecordPattern record, LoweredExpression value)
    {
        LoweredExpression condition = new LoweredIsRecordExpression(value, record.RecordType.Type);
        var bindings = new List<LoweredStatement>();

        foreach (var field in record.Fields)
        {
            var fieldValue = new LoweredFieldAccessExpression(value, field.Field);
            var fieldResult = LowerPattern(field.Pattern, fieldValue);
            condition = new LoweredBinaryExpression(condition, Lexing.TokenKind.AmpersandAmpersand, fieldResult.Condition, TypeSymbol.Bool);
            bindings.AddRange(fieldResult.Bindings);
        }

        return new PatternLoweringResult(condition, bindings);
    }

    private VariableSymbol NewTemp(TypeSymbol type)
    {
        return new VariableSymbol($"$tmp{tempIndex++}", false, type);
    }

    private sealed class PatternLoweringResult
    {
        public static readonly LoweredLiteralExpression TrueLiteral = new(true, TypeSymbol.Bool);

        public LoweredExpression Condition { get; }
        public List<LoweredStatement> Bindings { get; }

        public PatternLoweringResult(LoweredExpression condition)
            : this(condition, new List<LoweredStatement>())
        {
        }

        public PatternLoweringResult(LoweredExpression condition, List<LoweredStatement> bindings)
        {
            Condition = condition;
            Bindings = bindings;
        }

        public static PatternLoweringResult MatchAlways()
        {
            return new PatternLoweringResult(TrueLiteral);
        }
    }
}
