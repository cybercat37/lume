using System.Globalization;
using System.Text;
using System.Linq;
using Axom.Compiler.Binding;
using Axom.Compiler.Lowering;
using Axom.Compiler.Lexing;

namespace Axom.Compiler.Emitting;

public sealed class Emitter
{
    public string Emit(LoweredProgram program)
    {
        var builder = new StringBuilder();
        builder.AppendLine("using System;");
        if (RequiresCollections(program))
        {
            builder.AppendLine("using System.Collections.Generic;");
        }

        if (RequiresChannels(program))
        {
            builder.AppendLine("using System.Collections.Concurrent;");
        }

        if (RequiresTasks(program))
        {
            builder.AppendLine("using System.Threading.Tasks;");
        }
        builder.AppendLine();
        foreach (var record in program.RecordTypes)
        {
            WriteRecordType(builder, record);
            builder.AppendLine();
        }
        foreach (var sum in program.SumTypes)
        {
            WriteSumType(builder, sum);
            builder.AppendLine();
        }

        if (RequiresChannels(program))
        {
            WriteChannelRuntime(builder);
            builder.AppendLine();
        }
        builder.AppendLine("class Program");
        builder.AppendLine("{");
        foreach (var function in program.Functions)
        {
            WriteFunction(builder, function);
            builder.AppendLine();
        }
        builder.AppendLine("    static void Main()");
        builder.AppendLine("    {");
        var writer = new IndentedWriter(builder, 2);
        foreach (var statement in program.Statements)
        {
            WriteStatement(writer, statement);
        }
        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static bool RequiresCollections(LoweredProgram program)
    {
        return program.Statements.Any(UsesCollections) || program.Functions.Any(function => UsesCollections(function.Body));
    }

    private static bool RequiresTasks(LoweredProgram program)
    {
        return program.Statements.Any(UsesTasks) || program.Functions.Any(function => UsesTasks(function.Body));
    }

    private static bool RequiresChannels(LoweredProgram program)
    {
        return program.Statements.Any(UsesChannels) || program.Functions.Any(function => UsesChannels(function.Body));
    }

    private static bool UsesCollections(LoweredStatement statement)
    {
        return statement switch
        {
            LoweredBlockStatement block => block.Statements.Any(UsesCollections),
            LoweredVariableDeclaration declaration => UsesCollections(declaration.Initializer),
            LoweredPrintStatement print => UsesCollections(print.Expression),
            LoweredExpressionStatement expressionStatement => UsesCollections(expressionStatement.Expression),
            LoweredReturnStatement returnStatement => returnStatement.Expression is not null && UsesCollections(returnStatement.Expression),
            LoweredIfStatement ifStatement => UsesCollections(ifStatement.Condition) || UsesCollections(ifStatement.Then) || (ifStatement.Else is not null && UsesCollections(ifStatement.Else)),
            _ => false
        };
    }

    private static bool UsesTasks(LoweredStatement statement)
    {
        return statement switch
        {
            LoweredBlockStatement block => block.Statements.Any(UsesTasks),
            LoweredVariableDeclaration declaration => UsesTasks(declaration.Initializer),
            LoweredPrintStatement print => UsesTasks(print.Expression),
            LoweredExpressionStatement expressionStatement => UsesTasks(expressionStatement.Expression),
            LoweredReturnStatement returnStatement => returnStatement.Expression is not null && UsesTasks(returnStatement.Expression),
            LoweredIfStatement ifStatement => UsesTasks(ifStatement.Condition) || UsesTasks(ifStatement.Then) || (ifStatement.Else is not null && UsesTasks(ifStatement.Else)),
            _ => false
        };
    }

    private static bool UsesChannels(LoweredStatement statement)
    {
        return statement switch
        {
            LoweredBlockStatement block => block.Statements.Any(UsesChannels),
            LoweredVariableDeclaration declaration => UsesChannels(declaration.Initializer),
            LoweredPrintStatement print => UsesChannels(print.Expression),
            LoweredExpressionStatement expressionStatement => UsesChannels(expressionStatement.Expression),
            LoweredReturnStatement returnStatement => returnStatement.Expression is not null && UsesChannels(returnStatement.Expression),
            LoweredIfStatement ifStatement => UsesChannels(ifStatement.Condition) || UsesChannels(ifStatement.Then) || (ifStatement.Else is not null && UsesChannels(ifStatement.Else)),
            _ => false
        };
    }

    private static bool UsesCollections(LoweredExpression expression)
    {
        return expression switch
        {
            LoweredListExpression => true,
            LoweredMapExpression => true,
            LoweredIndexExpression index => UsesCollections(index.Target) || UsesCollections(index.Index),
            LoweredTupleExpression tuple => tuple.Elements.Any(UsesCollections),
            LoweredCallExpression call => UsesCollections(call.Callee) || call.Arguments.Any(UsesCollections),
            LoweredUnaryExpression unary => UsesCollections(unary.Operand),
            LoweredBinaryExpression binary => UsesCollections(binary.Left) || UsesCollections(binary.Right),
            LoweredAssignmentExpression assignment => UsesCollections(assignment.Expression),
            LoweredBlockExpression block => block.Statements.Any(UsesCollections) || UsesCollections(block.Result),
            LoweredLambdaExpression lambda => UsesCollections(lambda.Body),
            LoweredFieldAccessExpression fieldAccess => UsesCollections(fieldAccess.Target),
            LoweredRecordLiteralExpression record => record.Fields.Any(field => UsesCollections(field.Expression)),
            LoweredSumConstructorExpression sum => sum.Payload is not null && UsesCollections(sum.Payload),
            LoweredIsTupleExpression isTuple => UsesCollections(isTuple.Target),
            LoweredIsSumExpression isSum => UsesCollections(isSum.Target),
            LoweredIsRecordExpression isRecord => UsesCollections(isRecord.Target),
            LoweredSumTagExpression sumTag => UsesCollections(sumTag.Target),
            LoweredSumValueExpression sumValue => UsesCollections(sumValue.Target),
            LoweredUnwrapExpression unwrap => UsesCollections(unwrap.Target),
            LoweredSpawnExpression spawn => UsesCollections(spawn.Body),
            LoweredJoinExpression join => UsesCollections(join.Expression),
            _ => false
        };
    }

    private static bool UsesTasks(LoweredExpression expression)
    {
        return expression switch
        {
            LoweredSpawnExpression => true,
            LoweredJoinExpression => true,
            LoweredCallExpression call => UsesTasks(call.Callee) || call.Arguments.Any(UsesTasks),
            LoweredUnaryExpression unary => UsesTasks(unary.Operand),
            LoweredBinaryExpression binary => UsesTasks(binary.Left) || UsesTasks(binary.Right),
            LoweredAssignmentExpression assignment => UsesTasks(assignment.Expression),
            LoweredBlockExpression block => block.Statements.Any(UsesTasks) || UsesTasks(block.Result),
            LoweredLambdaExpression lambda => UsesTasks(lambda.Body),
            LoweredFieldAccessExpression fieldAccess => UsesTasks(fieldAccess.Target),
            LoweredRecordLiteralExpression record => record.Fields.Any(field => UsesTasks(field.Expression)),
            LoweredSumConstructorExpression sum => sum.Payload is not null && UsesTasks(sum.Payload),
            LoweredIsTupleExpression isTuple => UsesTasks(isTuple.Target),
            LoweredIsSumExpression isSum => UsesTasks(isSum.Target),
            LoweredIsRecordExpression isRecord => UsesTasks(isRecord.Target),
            LoweredSumTagExpression sumTag => UsesTasks(sumTag.Target),
            LoweredSumValueExpression sumValue => UsesTasks(sumValue.Target),
            LoweredUnwrapExpression unwrap => UsesTasks(unwrap.Target),
            LoweredIndexExpression index => UsesTasks(index.Target) || UsesTasks(index.Index),
            LoweredTupleExpression tuple => tuple.Elements.Any(UsesTasks),
            LoweredListExpression list => list.Elements.Any(UsesTasks),
            LoweredMapExpression map => map.Entries.Any(entry => UsesTasks(entry.Key) || UsesTasks(entry.Value)),
            _ => false
        };
    }

    private static bool UsesChannels(LoweredExpression expression)
    {
        return expression switch
        {
            LoweredChannelCreateExpression => true,
            LoweredChannelSendExpression => true,
            LoweredChannelReceiveExpression => true,
            LoweredCallExpression call => UsesChannels(call.Callee) || call.Arguments.Any(UsesChannels),
            LoweredUnaryExpression unary => UsesChannels(unary.Operand),
            LoweredBinaryExpression binary => UsesChannels(binary.Left) || UsesChannels(binary.Right),
            LoweredAssignmentExpression assignment => UsesChannels(assignment.Expression),
            LoweredBlockExpression block => block.Statements.Any(UsesChannels) || UsesChannels(block.Result),
            LoweredLambdaExpression lambda => UsesChannels(lambda.Body),
            LoweredFieldAccessExpression fieldAccess => UsesChannels(fieldAccess.Target),
            LoweredRecordLiteralExpression record => record.Fields.Any(field => UsesChannels(field.Expression)),
            LoweredSumConstructorExpression sum => sum.Payload is not null && UsesChannels(sum.Payload),
            LoweredIsTupleExpression isTuple => UsesChannels(isTuple.Target),
            LoweredIsSumExpression isSum => UsesChannels(isSum.Target),
            LoweredIsRecordExpression isRecord => UsesChannels(isRecord.Target),
            LoweredSumTagExpression sumTag => UsesChannels(sumTag.Target),
            LoweredSumValueExpression sumValue => UsesChannels(sumValue.Target),
            LoweredUnwrapExpression unwrap => UsesChannels(unwrap.Target),
            LoweredIndexExpression index => UsesChannels(index.Target) || UsesChannels(index.Index),
            LoweredTupleExpression tuple => tuple.Elements.Any(UsesChannels),
            LoweredListExpression list => list.Elements.Any(UsesChannels),
            LoweredMapExpression map => map.Entries.Any(entry => UsesChannels(entry.Key) || UsesChannels(entry.Value)),
            LoweredSpawnExpression spawn => UsesChannels(spawn.Body),
            LoweredJoinExpression join => UsesChannels(join.Expression),
            _ => false
        };
    }

    public string EmitCached(LoweredProgram program, EmitCache cache)
    {
        if (cache.TryGet(program, out var cached) && cached is not null)
        {
            return cached;
        }

        var code = Emit(program);
        cache.Store(program, code);
        return code;
    }

    private static void WriteStatement(IndentedWriter writer, LoweredStatement statement)
    {
        switch (statement)
        {
            case LoweredBlockStatement block:
                writer.WriteLine("{");
                writer.Indent();
                foreach (var inner in block.Statements)
                {
                    WriteStatement(writer, inner);
                }
                writer.Unindent();
                writer.WriteLine("}");
                return;
            case LoweredVariableDeclaration declaration:
                writer.WriteLine($"var {EscapeIdentifier(declaration.Symbol.Name)} = {WriteExpression(declaration.Initializer)};");
                return;
            case LoweredPrintStatement print:
                writer.WriteLine($"Console.WriteLine({WriteExpression(print.Expression)});");
                return;
            case LoweredExpressionStatement expressionStatement:
                writer.WriteLine($"{WriteExpression(expressionStatement.Expression)};");
                return;
            case LoweredReturnStatement returnStatement:
                if (returnStatement.Expression is null)
                {
                    writer.WriteLine("return;");
                    return;
                }

                writer.WriteLine($"return {WriteExpression(returnStatement.Expression)};");
                return;
            case LoweredIfStatement ifStatement:
                writer.WriteLine($"if ({WriteExpression(ifStatement.Condition)})");
                WriteStatement(writer, ifStatement.Then);
                if (ifStatement.Else is not null)
                {
                    writer.WriteLine("else");
                    WriteStatement(writer, ifStatement.Else);
                }
                return;
            default:
                throw new InvalidOperationException($"Unexpected statement: {statement.GetType().Name}");
        }
    }

    private static void WriteRecordType(StringBuilder builder, BoundRecordTypeDeclaration record)
    {
        builder.AppendLine($"sealed class {EscapeIdentifier(record.Type.Name)}");
        builder.AppendLine("{");
        var writer = new IndentedWriter(builder, 1);
        foreach (var field in record.Fields)
        {
            writer.WriteLine($"public {TypeToCSharp(field.Type)} {EscapeIdentifier(field.Name)} {{ get; init; }}");
        }
        builder.AppendLine("}");
    }

    private static void WriteSumType(StringBuilder builder, BoundSumTypeDeclaration sum)
    {
        builder.AppendLine($"sealed class {EscapeIdentifier(sum.Type.Name)}");
        builder.AppendLine("{");
        var writer = new IndentedWriter(builder, 1);
        writer.WriteLine("public string Tag { get; init; } = string.Empty;");
        writer.WriteLine("public object? Value { get; init; }");
        builder.AppendLine("}");
    }

    private static string WriteExpression(LoweredExpression expression, int parentPrecedence = 0)
    {
        return expression switch
        {
            LoweredLiteralExpression literal => FormatLiteral(literal.Value),
            LoweredNameExpression name => EscapeIdentifier(name.Symbol.Name),
            LoweredAssignmentExpression assignment => WrapIfNeeded(
                $"{EscapeIdentifier(assignment.Symbol.Name)} = {WriteExpression(assignment.Expression, GetAssignmentPrecedence())}",
                GetAssignmentPrecedence(),
                parentPrecedence),
            LoweredUnaryExpression unary => WrapIfNeeded(
                $"{OperatorText(unary.OperatorKind)}{WriteExpression(unary.Operand, GetUnaryPrecedence())}",
                GetUnaryPrecedence(),
                parentPrecedence),
            LoweredBinaryExpression binary => WriteBinaryExpression(binary, parentPrecedence),
            LoweredInputExpression => "Console.ReadLine()",
            LoweredCallExpression call => WriteCallExpression(call),
            LoweredFunctionExpression function => EscapeIdentifier(function.Function.Name),
            LoweredLambdaExpression lambda => WriteLambdaExpression(lambda),
            LoweredTupleExpression tuple => WriteTupleExpression(tuple),
            LoweredListExpression list => WriteListExpression(list),
            LoweredIndexExpression index => WriteIndexExpression(index),
            LoweredMapExpression map => WriteMapExpression(map),
            LoweredChannelCreateExpression channelCreate => WriteChannelCreateExpression(channelCreate),
            LoweredUnwrapExpression unwrap => WriteUnwrapExpression(unwrap),
            LoweredSpawnExpression spawn => WriteSpawnExpression(spawn),
            LoweredJoinExpression join => WriteJoinExpression(join),
            LoweredChannelSendExpression send => WriteChannelSendExpression(send),
            LoweredChannelReceiveExpression recv => WriteChannelReceiveExpression(recv),
            LoweredTupleAccessExpression tupleAccess => WriteTupleAccessExpression(tupleAccess),
            LoweredRecordLiteralExpression record => WriteRecordLiteralExpression(record),
            LoweredFieldAccessExpression fieldAccess => WriteFieldAccessExpression(fieldAccess),
            LoweredSumConstructorExpression sum => WriteSumConstructorExpression(sum),
            LoweredIsTupleExpression isTuple => WriteIsTupleExpression(isTuple),
            LoweredIsSumExpression isSum => WriteIsSumExpression(isSum),
            LoweredIsRecordExpression isRecord => WriteIsRecordExpression(isRecord),
            LoweredSumTagExpression sumTag => WriteSumTagExpression(sumTag),
            LoweredSumValueExpression sumValue => WriteSumValueExpression(sumValue),
            LoweredBlockExpression block => WriteBlockExpression(block),
            LoweredDefaultExpression defaultExpression => WriteDefaultExpression(defaultExpression),
            LoweredMatchFailureExpression matchFailure => WriteMatchFailureExpression(matchFailure),
            _ => throw new InvalidOperationException($"Unexpected expression: {expression.GetType().Name}")
        };
    }

    private static string WriteSumConstructorExpression(LoweredSumConstructorExpression sum)
    {
        var typeName = EscapeIdentifier(sum.Variant.DeclaringType.Name);
        if (sum.Payload is null)
        {
            return $"new {typeName} {{ Tag = \"{sum.Variant.Name}\" }}";
        }

        var payload = WriteExpression(sum.Payload);
        return $"new {typeName} {{ Tag = \"{sum.Variant.Name}\", Value = {payload} }}";
    }

    private static string WriteRecordLiteralExpression(LoweredRecordLiteralExpression record)
    {
        var assignments = string.Join(", ", record.Fields.Select(field =>
            $"{EscapeIdentifier(field.Field.Name)} = {WriteExpression(field.Expression)}"));
        return $"new {EscapeIdentifier(record.RecordType.Name)} {{ {assignments} }}";
    }

    private static string WriteFieldAccessExpression(LoweredFieldAccessExpression fieldAccess)
    {
        var target = WriteExpression(fieldAccess.Target);
        var needsParens = fieldAccess.Target is LoweredAssignmentExpression or LoweredBinaryExpression or LoweredUnaryExpression or LoweredBlockExpression;
        if (needsParens)
        {
            target = $"({target})";
        }

        return $"{target}.{EscapeIdentifier(fieldAccess.Field.Name)}";
    }

    private static string WriteTupleExpression(LoweredTupleExpression tuple)
    {
        var elements = string.Join(", ", tuple.Elements.Select(WriteExpression));
        return $"({elements})";
    }

    private static string WriteListExpression(LoweredListExpression list)
    {
        var elementType = list.Type.ListElementType ?? TypeSymbol.Error;
        var elements = string.Join(", ", list.Elements.Select(WriteExpression));
        return $"new List<{TypeToCSharp(elementType)}> {{ {elements} }}";
    }

    private static string WriteIndexExpression(LoweredIndexExpression index)
    {
        var target = WriteExpression(index.Target);
        var indexValue = WriteExpression(index.Index);
        return $"{target}[{indexValue}]";
    }

    private static string WriteMapExpression(LoweredMapExpression map)
    {
        var valueType = map.Type.MapValueType ?? TypeSymbol.Error;
        var entries = string.Join(", ", map.Entries.Select(entry =>
            $"{{ {WriteExpression(entry.Key)}, {WriteExpression(entry.Value)} }}"));
        return $"new Dictionary<string, {TypeToCSharp(valueType)}> {{ {entries} }}";
    }

    private static string WriteUnwrapExpression(LoweredUnwrapExpression unwrap)
    {
        var target = WriteExpression(unwrap.Target);
        var payloadType = TypeToCSharp(unwrap.Type);
        var failureTag = unwrap.FailureVariant.Name.Replace("\"", "\\\"");
        return $"(() => {{ var __tmp = {target}; if (__tmp.Tag == \"{failureTag}\") throw new InvalidOperationException(\"Unwrap failed.\"); return ({payloadType})__tmp.Value; }})()";
    }

    private static string WriteSpawnExpression(LoweredSpawnExpression spawn)
    {
        var taskType = TypeToCSharp(spawn.Type.TaskResultType ?? TypeSymbol.Error);
        var body = WriteExpression(spawn.Body);
        return $"Task.Run<{taskType}>(() => {body})";
    }

    private static string WriteJoinExpression(LoweredJoinExpression join)
    {
        var target = WriteExpression(join.Expression);
        return $"{target}.Result";
    }

    private static string WriteChannelCreateExpression(LoweredChannelCreateExpression channelCreate)
    {
        return $"AxomChannels.channel<{TypeToCSharp(channelCreate.ElementType)}>()";
    }

    private static string WriteChannelSendExpression(LoweredChannelSendExpression send)
    {
        var sender = WriteExpression(send.Sender);
        var value = WriteExpression(send.Value);
        return $"{sender}.send({value})";
    }

    private static string WriteChannelReceiveExpression(LoweredChannelReceiveExpression recv)
    {
        var receiver = WriteExpression(recv.Receiver);
        return $"{receiver}.recv()";
    }

    private static string WriteTupleAccessExpression(LoweredTupleAccessExpression tupleAccess)
    {
        var target = WriteExpression(tupleAccess.Target);
        return $"{target}.Item{tupleAccess.Index + 1}";
    }

    private static string WriteIsTupleExpression(LoweredIsTupleExpression isTuple)
    {
        var target = WriteExpression(isTuple.Target);
        var tupleType = TypeToCSharp(isTuple.TupleType);
        return $"{target} is {tupleType}";
    }

    private static string WriteIsSumExpression(LoweredIsSumExpression isSum)
    {
        var target = WriteExpression(isSum.Target);
        var sumType = TypeToCSharp(isSum.SumType);
        return $"{target} is {sumType}";
    }

    private static string WriteIsRecordExpression(LoweredIsRecordExpression isRecord)
    {
        var target = WriteExpression(isRecord.Target);
        var recordType = TypeToCSharp(isRecord.RecordType);
        return $"{target} is {recordType}";
    }

    private static string WriteSumTagExpression(LoweredSumTagExpression sumTag)
    {
        var target = WriteExpression(sumTag.Target);
        return $"{target}.Tag";
    }

    private static string WriteSumValueExpression(LoweredSumValueExpression sumValue)
    {
        var target = WriteExpression(sumValue.Target);
        var payloadType = TypeToCSharp(sumValue.Type);
        return $"({payloadType}){target}.Value";
    }

    private static string WriteBlockExpression(LoweredBlockExpression block)
    {
        var builder = new StringBuilder();
        var isUnit = block.Type == TypeSymbol.Unit;
        var delegateType = isUnit
            ? "Action"
            : $"Func<{TypeToCSharp(block.Type)}>";
        builder.Append($"(({delegateType})(() => ");
        builder.AppendLine("{");
        var writer = new IndentedWriter(builder, 1);
        foreach (var statement in block.Statements)
        {
            WriteStatement(writer, statement);
        }

        if (!isUnit)
        {
            writer.WriteLine($"return {WriteExpression(block.Result)};");
        }
        else
        {
            writer.WriteLine("return;");
        }

        builder.Append("}))()" );

        return builder.ToString();
    }

    private static string WriteDefaultExpression(LoweredDefaultExpression defaultExpression)
    {
        return defaultExpression.Type == TypeSymbol.Unit
            ? "null"
            : $"default({TypeToCSharp(defaultExpression.Type)})";
    }

    private static string WriteMatchFailureExpression(LoweredMatchFailureExpression matchFailure)
    {
        return matchFailure.Type == TypeSymbol.Unit
            ? "null"
            : $"default({TypeToCSharp(matchFailure.Type)})";
    }

    private static string WriteCallExpression(LoweredCallExpression call)
    {
        var args = string.Join(", ", call.Arguments.Select(arg => WriteExpression(arg)));
        if (call.Callee is LoweredFunctionExpression function && function.Function.IsBuiltin)
        {
            return function.Function.Name switch
            {
                "println" => $"Console.WriteLine({args})",
                "print" => $"Console.WriteLine({args})",
                "input" => "Console.ReadLine()",
                "len" => $"{args}.Length",
                "abs" => $"Math.Abs({args})",
                "min" => $"Math.Min({args})",
                "max" => $"Math.Max({args})",
                "float" => $"(double){args}",
                "int" => $"(int){args}",
                _ => $"{EscapeIdentifier(function.Function.Name)}({args})"
            };
        }

        var calleeText = WriteExpression(call.Callee);
        var wrapped = call.Callee is LoweredNameExpression or LoweredFunctionExpression
            ? calleeText
            : $"({calleeText})";
        return $"{wrapped}({args})";
    }

    private static string WriteLambdaExpression(LoweredLambdaExpression lambda)
    {
        var parameters = string.Join(", ", lambda.Parameters.Select(parameter =>
            $"{TypeToCSharp(parameter.Type)} {EscapeIdentifier(parameter.Name)}"));

        if (lambda.Body.Statements.Count == 1 && lambda.Body.Statements[0] is LoweredReturnStatement returnStatement)
        {
            var expression = returnStatement.Expression is null
                ? string.Empty
                : WriteExpression(returnStatement.Expression);
            return $"({parameters}) => {expression}".TrimEnd();
        }

        var builder = new StringBuilder();
        builder.Append($"({parameters}) => ");
        builder.AppendLine("{");
        var writer = new IndentedWriter(builder, 2);
        foreach (var statement in lambda.Body.Statements)
        {
            WriteStatement(writer, statement);
        }
        builder.Append("}");
        return builder.ToString();
    }

    private static void WriteFunction(StringBuilder builder, LoweredFunctionDeclaration function)
    {
        var returnType = TypeToCSharp(function.Symbol.ReturnType);
        var typeParameters = function.Symbol.GenericParameters.Count > 0
            ? $"<{string.Join(", ", function.Symbol.GenericParameters.Select(parameter => EscapeIdentifier(parameter.Name)))}>"
            : string.Empty;
        var parameters = string.Join(", ", function.Parameters.Select(parameter =>
            $"{TypeToCSharp(parameter.Type)} {EscapeIdentifier(parameter.Name)}"));
        builder.AppendLine($"    static {returnType} {EscapeIdentifier(function.Symbol.Name)}{typeParameters}({parameters})");
        builder.AppendLine("    {");
        var writer = new IndentedWriter(builder, 2);
        foreach (var statement in function.Body.Statements)
        {
            WriteStatement(writer, statement);
        }
        builder.AppendLine("    }");
    }

    private static string WriteBinaryExpression(LoweredBinaryExpression binary, int parentPrecedence)
    {
        var precedence = GetBinaryPrecedence(binary.OperatorKind);
        var left = WriteExpression(binary.Left, precedence);
        var right = WriteExpression(binary.Right, precedence);
        var text = $"{left} {OperatorText(binary.OperatorKind)} {right}";
        return WrapIfNeeded(text, precedence, parentPrecedence);
    }

    private static string WrapIfNeeded(string text, int precedence, int parentPrecedence)
    {
        return precedence < parentPrecedence ? $"({text})" : text;
    }

    private static int GetAssignmentPrecedence() => 1;

    private static int GetUnaryPrecedence() => 6;

    private static int GetBinaryPrecedence(TokenKind kind)
    {
        return kind switch
        {
            TokenKind.Star => 5,
            TokenKind.Slash => 5,
            TokenKind.Plus => 4,
            TokenKind.Minus => 4,
            TokenKind.EqualEqual => 3,
            TokenKind.BangEqual => 3,
            TokenKind.Less => 3,
            TokenKind.LessOrEqual => 3,
            TokenKind.Greater => 3,
            TokenKind.GreaterOrEqual => 3,
            TokenKind.AmpersandAmpersand => 2,
            TokenKind.PipePipe => 1,
            _ => 0
        };
    }

    private static string OperatorText(TokenKind kind)
    {
        return kind switch
        {
            TokenKind.Plus => "+",
            TokenKind.Minus => "-",
            TokenKind.Star => "*",
            TokenKind.Slash => "/",
            TokenKind.EqualEqual => "==",
            TokenKind.BangEqual => "!=",
            TokenKind.Less => "<",
            TokenKind.LessOrEqual => "<=",
            TokenKind.Greater => ">",
            TokenKind.GreaterOrEqual => ">=",
            TokenKind.AmpersandAmpersand => "&&",
            TokenKind.PipePipe => "||",
            TokenKind.Bang => "!",
            _ => throw new InvalidOperationException($"Unexpected operator: {kind}")
        };
    }

    private static string FormatLiteral(object? value)
    {
        return value switch
        {
            null => "null",
            bool boolValue => boolValue ? "true" : "false",
            string stringValue => $"\"{EscapeString(stringValue)}\"",
            double doubleValue => doubleValue.ToString(CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "null"
        };
    }

    private static string EscapeIdentifier(string name)
    {
        return CSharpKeywords.Contains(name) ? $"@{name}" : name;
    }

    private static string TypeToCSharp(TypeSymbol type)
    {
        if (type.IsChannelSender && type.ChannelElementType is not null)
        {
            return $"AxomSender<{TypeToCSharp(type.ChannelElementType)}>";
        }

        if (type.IsChannelReceiver && type.ChannelElementType is not null)
        {
            return $"AxomReceiver<{TypeToCSharp(type.ChannelElementType)}>";
        }

        if (type.MapValueType is not null)
        {
            return $"Dictionary<string, {TypeToCSharp(type.MapValueType)}>";
        }

        if (type.TaskResultType is not null)
        {
            return $"Task<{TypeToCSharp(type.TaskResultType)}>";
        }

        if (type.ListElementType is not null)
        {
            return $"List<{TypeToCSharp(type.ListElementType)}>";
        }

        if (type.TupleElementTypes is not null)
        {
            var elementTypes = type.TupleElementTypes.Select(TypeToCSharp);
            return $"({string.Join(", ", elementTypes)})";
        }

        if (type.ParameterTypes is not null)
        {
            var parameterTypes = type.ParameterTypes.Select(TypeToCSharp).ToList();
            var returnType = type.ReturnType ?? TypeSymbol.Unit;
            if (returnType == TypeSymbol.Unit)
            {
                if (parameterTypes.Count == 0)
                {
                    return "Action";
                }

                return $"Action<{string.Join(", ", parameterTypes)}>";
            }

            parameterTypes.Add(TypeToCSharp(returnType));
            return $"Func<{string.Join(", ", parameterTypes)}>";
        }

        return type switch
        {
            var t when t == TypeSymbol.Int => "int",
            var t when t == TypeSymbol.Float => "double",
            var t when t == TypeSymbol.Bool => "bool",
            var t when t == TypeSymbol.String => "string",
            var t when t == TypeSymbol.Unit => "void",
            _ => EscapeIdentifier(type.Name)
        };
    }

    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract",
        "as",
        "base",
        "bool",
        "break",
        "byte",
        "case",
        "catch",
        "char",
        "checked",
        "class",
        "const",
        "continue",
        "decimal",
        "default",
        "delegate",
        "do",
        "double",
        "else",
        "enum",
        "event",
        "explicit",
        "extern",
        "false",
        "finally",
        "fixed",
        "float",
        "for",
        "foreach",
        "goto",
        "if",
        "implicit",
        "in",
        "int",
        "interface",
        "internal",
        "is",
        "lock",
        "long",
        "namespace",
        "new",
        "null",
        "object",
        "operator",
        "out",
        "override",
        "params",
        "private",
        "protected",
        "public",
        "readonly",
        "ref",
        "return",
        "sbyte",
        "sealed",
        "short",
        "sizeof",
        "stackalloc",
        "static",
        "string",
        "struct",
        "switch",
        "this",
        "throw",
        "true",
        "try",
        "typeof",
        "uint",
        "ulong",
        "unchecked",
        "unsafe",
        "ushort",
        "using",
        "virtual",
        "void",
        "volatile",
        "while",
        "var"
    };

    private static void WriteChannelRuntime(StringBuilder builder)
    {
        builder.AppendLine("sealed class AxomChannelState<T>");
        builder.AppendLine("{");
        builder.AppendLine("    public BlockingCollection<T> Queue { get; } = new();");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("sealed class AxomSender<T>");
        builder.AppendLine("{");
        builder.AppendLine("    private readonly AxomChannelState<T> state;");
        builder.AppendLine("    public AxomSender(AxomChannelState<T> state)");
        builder.AppendLine("    {");
        builder.AppendLine("        this.state = state;");
        builder.AppendLine("    }");
        builder.AppendLine("    public void send(T value)");
        builder.AppendLine("    {");
        builder.AppendLine("        state.Queue.Add(value);");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("sealed class AxomReceiver<T>");
        builder.AppendLine("{");
        builder.AppendLine("    private readonly AxomChannelState<T> state;");
        builder.AppendLine("    public AxomReceiver(AxomChannelState<T> state)");
        builder.AppendLine("    {");
        builder.AppendLine("        this.state = state;");
        builder.AppendLine("    }");
        builder.AppendLine("    public T recv()");
        builder.AppendLine("    {");
        builder.AppendLine("        return state.Queue.Take();");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("static class AxomChannels");
        builder.AppendLine("{");
        builder.AppendLine("    public static (AxomSender<T>, AxomReceiver<T>) channel<T>()");
        builder.AppendLine("    {");
        builder.AppendLine("        var state = new AxomChannelState<T>();");
        builder.AppendLine("        return (new AxomSender<T>(state), new AxomReceiver<T>(state));");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }

    private static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t")
            .Replace("\r", "\\r");
    }

    private sealed class IndentedWriter
    {
        private readonly StringBuilder builder;
        private int indentLevel;

        public IndentedWriter(StringBuilder builder, int indentLevel)
        {
            this.builder = builder;
            this.indentLevel = indentLevel;
        }

        public void Indent()
        {
            indentLevel++;
        }

        public void Unindent()
        {
            indentLevel--;
        }

        public void WriteLine(string text)
        {
            builder.Append(' ', indentLevel * 4);
            builder.AppendLine(text);
        }
    }
}
