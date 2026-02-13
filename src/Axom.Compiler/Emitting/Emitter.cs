using System.Globalization;
using System.Text;
using System.Linq;
using Axom.Compiler.Binding;
using Axom.Compiler.Interop;
using Axom.Compiler.Lowering;
using Axom.Compiler.Lexing;

namespace Axom.Compiler.Emitting;

public sealed class Emitter
{
    public string Emit(LoweredProgram program)
    {
        var requiresFunctionLogging = program.Functions.Any(function => function.Symbol.EnableLogging);
        var requiresRandomBuiltins = RequiresRandomBuiltins(program);
        var requiresRangeBuiltin = RequiresRangeBuiltin(program);
        var requiresRandomResultRuntime = RequiresRandomResultRuntime(program);
        var requiresFormat = RequiresFormat(program);
        var requiresStringify = RequiresStringify(program) || requiresFormat || requiresFunctionLogging;
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

        if (RequiresDotNetInterop(program))
        {
            builder.AppendLine("using System.Reflection;");
            builder.AppendLine("using System.Globalization;");
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

        if (RequiresChannels(program) || RequiresDotNetInterop(program) || requiresRandomBuiltins || requiresRandomResultRuntime)
        {
            WriteAxomResultRuntime(builder);
            builder.AppendLine();
        }

        if (RequiresChannels(program))
        {
            WriteChannelRuntime(builder);
            builder.AppendLine();
        }

        if (RequiresDotNetInterop(program))
        {
            WriteDotNetInteropRuntime(builder);
            builder.AppendLine();
        }
        builder.AppendLine("class Program");
        builder.AppendLine("{");
        foreach (var function in program.Functions)
        {
            WriteFunction(builder, function);
            builder.AppendLine();
        }

        if (requiresStringify)
        {
            WriteStringifyHelper(builder);
            builder.AppendLine();
        }

        if (requiresFormat)
        {
            WriteFormatHelper(builder);
            builder.AppendLine();
        }

        if (requiresRandomBuiltins)
        {
            WriteRandomHelpers(builder);
            builder.AppendLine();
        }

        if (requiresRangeBuiltin)
        {
            WriteRangeHelper(builder);
            builder.AppendLine();
        }

        if (requiresFunctionLogging)
        {
            WriteFunctionLoggingHelpers(builder);
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
        return program.Statements.Any(UsesCollections)
            || program.Functions.Any(function => UsesCollections(function.Body))
            || RequiresRangeBuiltin(program);
    }

    private static bool RequiresTasks(LoweredProgram program)
    {
        return program.Statements.Any(UsesTasks) || program.Functions.Any(function => UsesTasks(function.Body));
    }

    private static bool RequiresChannels(LoweredProgram program)
    {
        return program.Statements.Any(UsesChannels) || program.Functions.Any(function => UsesChannels(function.Body));
    }

    private static bool RequiresDotNetInterop(LoweredProgram program)
    {
        return program.Statements.Any(UsesDotNetInterop) || program.Functions.Any(function => UsesDotNetInterop(function.Body));
    }

    private static bool RequiresStringify(LoweredProgram program)
    {
        return program.Statements.Any(UsesStringify) || program.Functions.Any(function => UsesStringify(function.Body));
    }

    private static bool RequiresFormat(LoweredProgram program)
    {
        return program.Statements.Any(UsesFormat) || program.Functions.Any(function => UsesFormat(function.Body));
    }

    private static bool RequiresRandomBuiltins(LoweredProgram program)
    {
        return program.Statements.Any(UsesRandomBuiltins) || program.Functions.Any(function => UsesRandomBuiltins(function.Body));
    }

    private static bool RequiresRangeBuiltin(LoweredProgram program)
    {
        return program.Statements.Any(UsesRangeBuiltin) || program.Functions.Any(function => UsesRangeBuiltin(function.Body));
    }

    private static bool RequiresRandomResultRuntime(LoweredProgram program)
    {
        return program.Statements.Any(UsesRandomResultBuiltin) || program.Functions.Any(function => UsesRandomResultBuiltin(function.Body));
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

    private static bool UsesDotNetInterop(LoweredStatement statement)
    {
        return statement switch
        {
            LoweredBlockStatement block => block.Statements.Any(UsesDotNetInterop),
            LoweredVariableDeclaration declaration => UsesDotNetInterop(declaration.Initializer),
            LoweredPrintStatement print => UsesDotNetInterop(print.Expression),
            LoweredExpressionStatement expressionStatement => UsesDotNetInterop(expressionStatement.Expression),
            LoweredReturnStatement returnStatement => returnStatement.Expression is not null && UsesDotNetInterop(returnStatement.Expression),
            LoweredIfStatement ifStatement => UsesDotNetInterop(ifStatement.Condition) || UsesDotNetInterop(ifStatement.Then) || (ifStatement.Else is not null && UsesDotNetInterop(ifStatement.Else)),
            _ => false
        };
    }

    private static bool UsesStringify(LoweredStatement statement)
    {
        return statement switch
        {
            LoweredBlockStatement block => block.Statements.Any(UsesStringify),
            LoweredVariableDeclaration declaration => UsesStringify(declaration.Initializer),
            LoweredPrintStatement print => UsesStringify(print.Expression),
            LoweredExpressionStatement expressionStatement => UsesStringify(expressionStatement.Expression),
            LoweredReturnStatement returnStatement => returnStatement.Expression is not null && UsesStringify(returnStatement.Expression),
            LoweredIfStatement ifStatement => UsesStringify(ifStatement.Condition) || UsesStringify(ifStatement.Then) || (ifStatement.Else is not null && UsesStringify(ifStatement.Else)),
            _ => false
        };
    }

    private static bool UsesFormat(LoweredStatement statement)
    {
        return statement switch
        {
            LoweredBlockStatement block => block.Statements.Any(UsesFormat),
            LoweredVariableDeclaration declaration => UsesFormat(declaration.Initializer),
            LoweredPrintStatement print => UsesFormat(print.Expression),
            LoweredExpressionStatement expressionStatement => UsesFormat(expressionStatement.Expression),
            LoweredReturnStatement returnStatement => returnStatement.Expression is not null && UsesFormat(returnStatement.Expression),
            LoweredIfStatement ifStatement => UsesFormat(ifStatement.Condition) || UsesFormat(ifStatement.Then) || (ifStatement.Else is not null && UsesFormat(ifStatement.Else)),
            _ => false
        };
    }

    private static bool UsesRandomBuiltins(LoweredStatement statement)
    {
        return statement switch
        {
            LoweredBlockStatement block => block.Statements.Any(UsesRandomBuiltins),
            LoweredVariableDeclaration declaration => UsesRandomBuiltins(declaration.Initializer),
            LoweredPrintStatement print => UsesRandomBuiltins(print.Expression),
            LoweredExpressionStatement expressionStatement => UsesRandomBuiltins(expressionStatement.Expression),
            LoweredReturnStatement returnStatement => returnStatement.Expression is not null && UsesRandomBuiltins(returnStatement.Expression),
            LoweredIfStatement ifStatement => UsesRandomBuiltins(ifStatement.Condition) || UsesRandomBuiltins(ifStatement.Then) || (ifStatement.Else is not null && UsesRandomBuiltins(ifStatement.Else)),
            _ => false
        };
    }

    private static bool UsesRangeBuiltin(LoweredStatement statement)
    {
        return statement switch
        {
            LoweredBlockStatement block => block.Statements.Any(UsesRangeBuiltin),
            LoweredVariableDeclaration declaration => UsesRangeBuiltin(declaration.Initializer),
            LoweredPrintStatement print => UsesRangeBuiltin(print.Expression),
            LoweredExpressionStatement expressionStatement => UsesRangeBuiltin(expressionStatement.Expression),
            LoweredReturnStatement returnStatement => returnStatement.Expression is not null && UsesRangeBuiltin(returnStatement.Expression),
            LoweredIfStatement ifStatement => UsesRangeBuiltin(ifStatement.Condition) || UsesRangeBuiltin(ifStatement.Then) || (ifStatement.Else is not null && UsesRangeBuiltin(ifStatement.Else)),
            _ => false
        };
    }

    private static bool UsesRandomResultBuiltin(LoweredStatement statement)
    {
        return statement switch
        {
            LoweredBlockStatement block => block.Statements.Any(UsesRandomResultBuiltin),
            LoweredVariableDeclaration declaration => UsesRandomResultBuiltin(declaration.Initializer),
            LoweredPrintStatement print => UsesRandomResultBuiltin(print.Expression),
            LoweredExpressionStatement expressionStatement => UsesRandomResultBuiltin(expressionStatement.Expression),
            LoweredReturnStatement returnStatement => returnStatement.Expression is not null && UsesRandomResultBuiltin(returnStatement.Expression),
            LoweredIfStatement ifStatement => UsesRandomResultBuiltin(ifStatement.Condition) || UsesRandomResultBuiltin(ifStatement.Then) || (ifStatement.Else is not null && UsesRandomResultBuiltin(ifStatement.Else)),
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

    private static bool UsesDotNetInterop(LoweredExpression expression)
    {
        return expression switch
        {
            LoweredDotNetCallExpression => true,
            LoweredCallExpression call => UsesDotNetInterop(call.Callee) || call.Arguments.Any(UsesDotNetInterop),
            LoweredUnaryExpression unary => UsesDotNetInterop(unary.Operand),
            LoweredBinaryExpression binary => UsesDotNetInterop(binary.Left) || UsesDotNetInterop(binary.Right),
            LoweredAssignmentExpression assignment => UsesDotNetInterop(assignment.Expression),
            LoweredBlockExpression block => block.Statements.Any(UsesDotNetInterop) || UsesDotNetInterop(block.Result),
            LoweredLambdaExpression lambda => UsesDotNetInterop(lambda.Body),
            LoweredFieldAccessExpression fieldAccess => UsesDotNetInterop(fieldAccess.Target),
            LoweredRecordLiteralExpression record => record.Fields.Any(field => UsesDotNetInterop(field.Expression)),
            LoweredSumConstructorExpression sum => sum.Payload is not null && UsesDotNetInterop(sum.Payload),
            LoweredIsTupleExpression isTuple => UsesDotNetInterop(isTuple.Target),
            LoweredIsSumExpression isSum => UsesDotNetInterop(isSum.Target),
            LoweredIsRecordExpression isRecord => UsesDotNetInterop(isRecord.Target),
            LoweredSumTagExpression sumTag => UsesDotNetInterop(sumTag.Target),
            LoweredSumValueExpression sumValue => UsesDotNetInterop(sumValue.Target),
            LoweredUnwrapExpression unwrap => UsesDotNetInterop(unwrap.Target),
            LoweredIndexExpression index => UsesDotNetInterop(index.Target) || UsesDotNetInterop(index.Index),
            LoweredTupleExpression tuple => tuple.Elements.Any(UsesDotNetInterop),
            LoweredListExpression list => list.Elements.Any(UsesDotNetInterop),
            LoweredMapExpression map => map.Entries.Any(entry => UsesDotNetInterop(entry.Key) || UsesDotNetInterop(entry.Value)),
            LoweredSpawnExpression spawn => UsesDotNetInterop(spawn.Body),
            LoweredJoinExpression join => UsesDotNetInterop(join.Expression),
            LoweredChannelCreateExpression => false,
            LoweredChannelSendExpression send => UsesDotNetInterop(send.Sender) || UsesDotNetInterop(send.Value),
            LoweredChannelReceiveExpression recv => UsesDotNetInterop(recv.Receiver),
            _ => false
        };
    }

    private static bool UsesStringify(LoweredExpression expression)
    {
        return expression switch
        {
            LoweredCallExpression call when call.Callee is LoweredFunctionExpression function
                && function.Function.IsBuiltin
                && string.Equals(function.Function.Name, "str", StringComparison.Ordinal) => true,
            LoweredCallExpression call => UsesStringify(call.Callee) || call.Arguments.Any(UsesStringify),
            LoweredUnaryExpression unary => UsesStringify(unary.Operand),
            LoweredBinaryExpression binary => UsesStringify(binary.Left) || UsesStringify(binary.Right),
            LoweredAssignmentExpression assignment => UsesStringify(assignment.Expression),
            LoweredBlockExpression block => block.Statements.Any(UsesStringify) || UsesStringify(block.Result),
            LoweredLambdaExpression lambda => UsesStringify(lambda.Body),
            LoweredFieldAccessExpression fieldAccess => UsesStringify(fieldAccess.Target),
            LoweredRecordLiteralExpression record => record.Fields.Any(field => UsesStringify(field.Expression)),
            LoweredSumConstructorExpression sum => sum.Payload is not null && UsesStringify(sum.Payload),
            LoweredIsTupleExpression isTuple => UsesStringify(isTuple.Target),
            LoweredIsSumExpression isSum => UsesStringify(isSum.Target),
            LoweredIsRecordExpression isRecord => UsesStringify(isRecord.Target),
            LoweredSumTagExpression sumTag => UsesStringify(sumTag.Target),
            LoweredSumValueExpression sumValue => UsesStringify(sumValue.Target),
            LoweredUnwrapExpression unwrap => UsesStringify(unwrap.Target),
            LoweredSpawnExpression spawn => UsesStringify(spawn.Body),
            LoweredJoinExpression join => UsesStringify(join.Expression),
            LoweredIndexExpression index => UsesStringify(index.Target) || UsesStringify(index.Index),
            LoweredTupleExpression tuple => tuple.Elements.Any(UsesStringify),
            LoweredListExpression list => list.Elements.Any(UsesStringify),
            LoweredMapExpression map => map.Entries.Any(entry => UsesStringify(entry.Key) || UsesStringify(entry.Value)),
            LoweredChannelSendExpression send => UsesStringify(send.Sender) || UsesStringify(send.Value),
            LoweredChannelReceiveExpression recv => UsesStringify(recv.Receiver),
            LoweredDotNetCallExpression dotNet => UsesStringify(dotNet.TypeNameExpression)
                || UsesStringify(dotNet.MethodNameExpression)
                || dotNet.Arguments.Any(UsesStringify),
            _ => false
        };
    }

    private static bool UsesFormat(LoweredExpression expression)
    {
        return expression switch
        {
            LoweredCallExpression call when call.Callee is LoweredFunctionExpression function
                && function.Function.IsBuiltin
                && string.Equals(function.Function.Name, "format", StringComparison.Ordinal) => true,
            LoweredCallExpression call => UsesFormat(call.Callee) || call.Arguments.Any(UsesFormat),
            LoweredUnaryExpression unary => UsesFormat(unary.Operand),
            LoweredBinaryExpression binary => UsesFormat(binary.Left) || UsesFormat(binary.Right),
            LoweredAssignmentExpression assignment => UsesFormat(assignment.Expression),
            LoweredBlockExpression block => block.Statements.Any(UsesFormat) || UsesFormat(block.Result),
            LoweredLambdaExpression lambda => UsesFormat(lambda.Body),
            LoweredFieldAccessExpression fieldAccess => UsesFormat(fieldAccess.Target),
            LoweredRecordLiteralExpression record => record.Fields.Any(field => UsesFormat(field.Expression)),
            LoweredSumConstructorExpression sum => sum.Payload is not null && UsesFormat(sum.Payload),
            LoweredIsTupleExpression isTuple => UsesFormat(isTuple.Target),
            LoweredIsSumExpression isSum => UsesFormat(isSum.Target),
            LoweredIsRecordExpression isRecord => UsesFormat(isRecord.Target),
            LoweredSumTagExpression sumTag => UsesFormat(sumTag.Target),
            LoweredSumValueExpression sumValue => UsesFormat(sumValue.Target),
            LoweredUnwrapExpression unwrap => UsesFormat(unwrap.Target),
            LoweredSpawnExpression spawn => UsesFormat(spawn.Body),
            LoweredJoinExpression join => UsesFormat(join.Expression),
            LoweredIndexExpression index => UsesFormat(index.Target) || UsesFormat(index.Index),
            LoweredTupleExpression tuple => tuple.Elements.Any(UsesFormat),
            LoweredListExpression list => list.Elements.Any(UsesFormat),
            LoweredMapExpression map => map.Entries.Any(entry => UsesFormat(entry.Key) || UsesFormat(entry.Value)),
            LoweredChannelSendExpression send => UsesFormat(send.Sender) || UsesFormat(send.Value),
            LoweredChannelReceiveExpression recv => UsesFormat(recv.Receiver),
            LoweredDotNetCallExpression dotNet => UsesFormat(dotNet.TypeNameExpression)
                || UsesFormat(dotNet.MethodNameExpression)
                || dotNet.Arguments.Any(UsesFormat),
            _ => false
        };
    }

    private static bool UsesRandomBuiltins(LoweredExpression expression)
    {
        return expression switch
        {
            LoweredCallExpression call when call.Callee is LoweredFunctionExpression function
                && function.Function.IsBuiltin
                && (string.Equals(function.Function.Name, "sleep", StringComparison.Ordinal)
                    || string.Equals(function.Function.Name, "rand_float", StringComparison.Ordinal)
                    || string.Equals(function.Function.Name, "rand_int", StringComparison.Ordinal)
                    || string.Equals(function.Function.Name, "rand_seed", StringComparison.Ordinal)) => true,
            LoweredCallExpression call => UsesRandomBuiltins(call.Callee) || call.Arguments.Any(UsesRandomBuiltins),
            LoweredUnaryExpression unary => UsesRandomBuiltins(unary.Operand),
            LoweredBinaryExpression binary => UsesRandomBuiltins(binary.Left) || UsesRandomBuiltins(binary.Right),
            LoweredAssignmentExpression assignment => UsesRandomBuiltins(assignment.Expression),
            LoweredBlockExpression block => block.Statements.Any(UsesRandomBuiltins) || UsesRandomBuiltins(block.Result),
            LoweredLambdaExpression lambda => UsesRandomBuiltins(lambda.Body),
            LoweredFieldAccessExpression fieldAccess => UsesRandomBuiltins(fieldAccess.Target),
            LoweredRecordLiteralExpression record => record.Fields.Any(field => UsesRandomBuiltins(field.Expression)),
            LoweredSumConstructorExpression sum => sum.Payload is not null && UsesRandomBuiltins(sum.Payload),
            LoweredIsTupleExpression isTuple => UsesRandomBuiltins(isTuple.Target),
            LoweredIsSumExpression isSum => UsesRandomBuiltins(isSum.Target),
            LoweredIsRecordExpression isRecord => UsesRandomBuiltins(isRecord.Target),
            LoweredSumTagExpression sumTag => UsesRandomBuiltins(sumTag.Target),
            LoweredSumValueExpression sumValue => UsesRandomBuiltins(sumValue.Target),
            LoweredUnwrapExpression unwrap => UsesRandomBuiltins(unwrap.Target),
            LoweredSpawnExpression spawn => UsesRandomBuiltins(spawn.Body),
            LoweredJoinExpression join => UsesRandomBuiltins(join.Expression),
            LoweredIndexExpression index => UsesRandomBuiltins(index.Target) || UsesRandomBuiltins(index.Index),
            LoweredTupleExpression tuple => tuple.Elements.Any(UsesRandomBuiltins),
            LoweredListExpression list => list.Elements.Any(UsesRandomBuiltins),
            LoweredMapExpression map => map.Entries.Any(entry => UsesRandomBuiltins(entry.Key) || UsesRandomBuiltins(entry.Value)),
            LoweredChannelSendExpression send => UsesRandomBuiltins(send.Sender) || UsesRandomBuiltins(send.Value),
            LoweredChannelReceiveExpression recv => UsesRandomBuiltins(recv.Receiver),
            LoweredDotNetCallExpression dotNet => UsesRandomBuiltins(dotNet.TypeNameExpression)
                || UsesRandomBuiltins(dotNet.MethodNameExpression)
                || dotNet.Arguments.Any(UsesRandomBuiltins),
            _ => false
        };
    }

    private static bool UsesRangeBuiltin(LoweredExpression expression)
    {
        return expression switch
        {
            LoweredCallExpression call when call.Callee is LoweredFunctionExpression function
                && function.Function.IsBuiltin
                && string.Equals(function.Function.Name, "range", StringComparison.Ordinal) => true,
            LoweredCallExpression call => UsesRangeBuiltin(call.Callee) || call.Arguments.Any(UsesRangeBuiltin),
            LoweredUnaryExpression unary => UsesRangeBuiltin(unary.Operand),
            LoweredBinaryExpression binary => UsesRangeBuiltin(binary.Left) || UsesRangeBuiltin(binary.Right),
            LoweredAssignmentExpression assignment => UsesRangeBuiltin(assignment.Expression),
            LoweredBlockExpression block => block.Statements.Any(UsesRangeBuiltin) || UsesRangeBuiltin(block.Result),
            LoweredLambdaExpression lambda => UsesRangeBuiltin(lambda.Body),
            LoweredFieldAccessExpression fieldAccess => UsesRangeBuiltin(fieldAccess.Target),
            LoweredRecordLiteralExpression record => record.Fields.Any(field => UsesRangeBuiltin(field.Expression)),
            LoweredSumConstructorExpression sum => sum.Payload is not null && UsesRangeBuiltin(sum.Payload),
            LoweredIsTupleExpression isTuple => UsesRangeBuiltin(isTuple.Target),
            LoweredIsSumExpression isSum => UsesRangeBuiltin(isSum.Target),
            LoweredIsRecordExpression isRecord => UsesRangeBuiltin(isRecord.Target),
            LoweredSumTagExpression sumTag => UsesRangeBuiltin(sumTag.Target),
            LoweredSumValueExpression sumValue => UsesRangeBuiltin(sumValue.Target),
            LoweredUnwrapExpression unwrap => UsesRangeBuiltin(unwrap.Target),
            LoweredSpawnExpression spawn => UsesRangeBuiltin(spawn.Body),
            LoweredJoinExpression join => UsesRangeBuiltin(join.Expression),
            LoweredIndexExpression index => UsesRangeBuiltin(index.Target) || UsesRangeBuiltin(index.Index),
            LoweredTupleExpression tuple => tuple.Elements.Any(UsesRangeBuiltin),
            LoweredListExpression list => list.Elements.Any(UsesRangeBuiltin),
            LoweredMapExpression map => map.Entries.Any(entry => UsesRangeBuiltin(entry.Key) || UsesRangeBuiltin(entry.Value)),
            LoweredChannelSendExpression send => UsesRangeBuiltin(send.Sender) || UsesRangeBuiltin(send.Value),
            LoweredChannelReceiveExpression recv => UsesRangeBuiltin(recv.Receiver),
            LoweredDotNetCallExpression dotNet => UsesRangeBuiltin(dotNet.TypeNameExpression)
                || UsesRangeBuiltin(dotNet.MethodNameExpression)
                || dotNet.Arguments.Any(UsesRangeBuiltin),
            _ => false
        };
    }

    private static bool UsesRandomResultBuiltin(LoweredExpression expression)
    {
        return expression switch
        {
            LoweredCallExpression call when call.Callee is LoweredFunctionExpression function
                && function.Function.IsBuiltin
                && string.Equals(function.Function.Name, "rand_int", StringComparison.Ordinal) => true,
            LoweredCallExpression call => UsesRandomResultBuiltin(call.Callee) || call.Arguments.Any(UsesRandomResultBuiltin),
            LoweredUnaryExpression unary => UsesRandomResultBuiltin(unary.Operand),
            LoweredBinaryExpression binary => UsesRandomResultBuiltin(binary.Left) || UsesRandomResultBuiltin(binary.Right),
            LoweredAssignmentExpression assignment => UsesRandomResultBuiltin(assignment.Expression),
            LoweredBlockExpression block => block.Statements.Any(UsesRandomResultBuiltin) || UsesRandomResultBuiltin(block.Result),
            LoweredLambdaExpression lambda => UsesRandomResultBuiltin(lambda.Body),
            LoweredFieldAccessExpression fieldAccess => UsesRandomResultBuiltin(fieldAccess.Target),
            LoweredRecordLiteralExpression record => record.Fields.Any(field => UsesRandomResultBuiltin(field.Expression)),
            LoweredSumConstructorExpression sum => sum.Payload is not null && UsesRandomResultBuiltin(sum.Payload),
            LoweredIsTupleExpression isTuple => UsesRandomResultBuiltin(isTuple.Target),
            LoweredIsSumExpression isSum => UsesRandomResultBuiltin(isSum.Target),
            LoweredIsRecordExpression isRecord => UsesRandomResultBuiltin(isRecord.Target),
            LoweredSumTagExpression sumTag => UsesRandomResultBuiltin(sumTag.Target),
            LoweredSumValueExpression sumValue => UsesRandomResultBuiltin(sumValue.Target),
            LoweredUnwrapExpression unwrap => UsesRandomResultBuiltin(unwrap.Target),
            LoweredSpawnExpression spawn => UsesRandomResultBuiltin(spawn.Body),
            LoweredJoinExpression join => UsesRandomResultBuiltin(join.Expression),
            LoweredIndexExpression index => UsesRandomResultBuiltin(index.Target) || UsesRandomResultBuiltin(index.Index),
            LoweredTupleExpression tuple => tuple.Elements.Any(UsesRandomResultBuiltin),
            LoweredListExpression list => list.Elements.Any(UsesRandomResultBuiltin),
            LoweredMapExpression map => map.Entries.Any(entry => UsesRandomResultBuiltin(entry.Key) || UsesRandomResultBuiltin(entry.Value)),
            LoweredChannelSendExpression send => UsesRandomResultBuiltin(send.Sender) || UsesRandomResultBuiltin(send.Value),
            LoweredChannelReceiveExpression recv => UsesRandomResultBuiltin(recv.Receiver),
            LoweredDotNetCallExpression dotNet => UsesRandomResultBuiltin(dotNet.TypeNameExpression)
                || UsesRandomResultBuiltin(dotNet.MethodNameExpression)
                || dotNet.Arguments.Any(UsesRandomResultBuiltin),
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

    private static void WriteStatement(
        IndentedWriter writer,
        LoweredStatement statement,
        bool logFunctionReturns = false,
        string? functionName = null)
    {
        switch (statement)
        {
            case LoweredBlockStatement block:
                if (block.IsTransparent)
                {
                    foreach (var inner in block.Statements)
                    {
                        WriteStatement(writer, inner, logFunctionReturns, functionName);
                    }

                    return;
                }

                writer.WriteLine("{");
                writer.Indent();
                foreach (var inner in block.Statements)
                {
                    WriteStatement(writer, inner, logFunctionReturns, functionName);
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
                    if (logFunctionReturns && functionName is not null)
                    {
                        writer.WriteLine("{");
                        writer.Indent();
                        writer.WriteLine($"AxomLogReturn(\"{EscapeString(functionName)}\", null);");
                        writer.WriteLine("return;");
                        writer.Unindent();
                        writer.WriteLine("}");
                        return;
                    }

                    writer.WriteLine("return;");
                    return;
                }

                if (logFunctionReturns && functionName is not null)
                {
                    writer.WriteLine("{");
                    writer.Indent();
                    writer.WriteLine($"var __axomReturn = {WriteExpression(returnStatement.Expression)};");
                    writer.WriteLine($"AxomLogReturn(\"{EscapeString(functionName)}\", __axomReturn);");
                    writer.WriteLine("return __axomReturn;");
                    writer.Unindent();
                    writer.WriteLine("}");
                    return;
                }

                writer.WriteLine($"return {WriteExpression(returnStatement.Expression)};");
                return;
            case LoweredIfStatement ifStatement:
                writer.WriteLine($"if ({WriteExpression(ifStatement.Condition)})");
                WriteStatement(writer, ifStatement.Then, logFunctionReturns, functionName);
                if (ifStatement.Else is not null)
                {
                    writer.WriteLine("else");
                    WriteStatement(writer, ifStatement.Else, logFunctionReturns, functionName);
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
            LoweredDotNetCallExpression dotNet => WriteDotNetCallExpression(dotNet),
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
        return $"((Func<{payloadType}>)(() => {{ var __tmp = {target}; if (__tmp.Tag == \"{failureTag}\") throw new InvalidOperationException(\"Unwrap failed.\"); if (__tmp.Value is {payloadType} __payload) return __payload; throw new InvalidOperationException(\"Unwrap failed.\"); }}))()";
    }

    private static string WriteSpawnExpression(LoweredSpawnExpression spawn)
    {
        var body = WriteExpression(spawn.Body);
        var resultType = spawn.Type.TaskResultType ?? TypeSymbol.Error;
        if (resultType == TypeSymbol.Unit)
        {
            return $"Task.Run(() => {body})";
        }

        var taskType = TypeToCSharp(resultType);
        return $"Task.Run<{taskType}>(() => {body})";
    }

    private static string WriteJoinExpression(LoweredJoinExpression join)
    {
        var target = WriteExpression(join.Expression);
        if (join.Type == TypeSymbol.Unit)
        {
            return $"((Func<object?>)(() => {{ {target}.Wait(); return null; }}))()";
        }

        return $"{target}.Result";
    }

    private static string WriteChannelCreateExpression(LoweredChannelCreateExpression channelCreate)
    {
        return $"AxomChannels.channel<{TypeToCSharp(channelCreate.ElementType)}>({channelCreate.Capacity})";
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

    private static string WriteDotNetCallExpression(LoweredDotNetCallExpression dotNet)
    {
        var typeName = WriteExpression(dotNet.TypeNameExpression);
        var methodName = WriteExpression(dotNet.MethodNameExpression);
        var args = string.Join(", ", dotNet.Arguments.Select(WriteExpression));
        var resultType = TypeToCSharp(dotNet.ReturnType);
        var method = dotNet.IsTryCall ? "TryCall" : "Call";
        if (args.Length == 0)
        {
            return $"DotNetInterop.{method}<{resultType}>({typeName}, {methodName})";
        }

        return $"DotNetInterop.{method}<{resultType}>({typeName}, {methodName}, {args})";
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
            if (block.Result is not LoweredDefaultExpression)
            {
                writer.WriteLine($"{WriteExpression(block.Result)};");
            }

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
        var argumentExpressions = call.Arguments.Select(WriteExpression).ToList();
        var args = string.Join(", ", argumentExpressions);
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
                "str" => $"AxomStringify({argumentExpressions[0]})",
                "format" => $"AxomFormat({argumentExpressions[0]}, {argumentExpressions[1]})",
                "sleep" => $"((Func<object?>)(() => {{ System.Threading.Thread.Sleep(Math.Max(0, {argumentExpressions[0]})); return null; }}))()",
                "rand_float" => "AxomRandFloat()",
                "rand_int" => $"AxomRandInt({argumentExpressions[0]})",
                "rand_seed" => $"((Func<object?>)(() => {{ AxomRandSeed({argumentExpressions[0]}); return null; }}))()",
                "range" => argumentExpressions.Count switch
                {
                    2 => $"AxomRange({argumentExpressions[0]}, {argumentExpressions[1]})",
                    3 => $"AxomRange({argumentExpressions[0]}, {argumentExpressions[1]}, {argumentExpressions[2]})",
                    _ => "new List<int>()"
                },
                "map" => $"System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select({argumentExpressions[0]}, {argumentExpressions[1]}))",
                "filter" => $"System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where({argumentExpressions[0]}, {argumentExpressions[1]}))",
                "fold" => $"System.Linq.Enumerable.Aggregate({argumentExpressions[0]}, {argumentExpressions[1]}, {argumentExpressions[2]})",
                "each" => $"{argumentExpressions[0]}.ForEach({argumentExpressions[1]})",
                "take" => $"System.Linq.Enumerable.ToList(System.Linq.Enumerable.Take({argumentExpressions[0]}, Math.Max(0, {argumentExpressions[1]})))",
                "skip" => $"System.Linq.Enumerable.ToList(System.Linq.Enumerable.Skip({argumentExpressions[0]}, Math.Max(0, {argumentExpressions[1]})))",
                "take_while" => $"System.Linq.Enumerable.ToList(System.Linq.Enumerable.TakeWhile({argumentExpressions[0]}, {argumentExpressions[1]}))",
                "skip_while" => $"System.Linq.Enumerable.ToList(System.Linq.Enumerable.SkipWhile({argumentExpressions[0]}, {argumentExpressions[1]}))",
                "zip" => $"System.Linq.Enumerable.ToList(System.Linq.Enumerable.Zip({argumentExpressions[0]}, {argumentExpressions[1]}, (left, right) => (left, right)))",
                "zip_with" => $"System.Linq.Enumerable.ToList(System.Linq.Enumerable.Zip({argumentExpressions[0]}, {argumentExpressions[1]}, {argumentExpressions[2]}))",
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
        var logFunction = function.Symbol.EnableLogging;
        builder.AppendLine($"    static {returnType} {EscapeIdentifier(function.Symbol.Name)}{typeParameters}({parameters})");
        builder.AppendLine("    {");
        var writer = new IndentedWriter(builder, 2);
        if (logFunction)
        {
            var argsArray = function.Parameters.Count == 0
                ? "Array.Empty<object?>()"
                : $"new object?[] {{ {string.Join(", ", function.Parameters.Select(parameter => EscapeIdentifier(parameter.Name)))} }}";
            writer.WriteLine($"AxomLogInvocation(\"{EscapeString(function.Symbol.Name)}\", {argsArray});");
        }

        foreach (var statement in function.Body.Statements)
        {
            WriteStatement(writer, statement, logFunction, function.Symbol.Name);
        }

        if (logFunction && function.Symbol.ReturnType == TypeSymbol.Unit)
        {
            writer.WriteLine($"AxomLogReturn(\"{EscapeString(function.Symbol.Name)}\", null);");
        }

        builder.AppendLine("    }");
    }

    private static void WriteFunctionLoggingHelpers(StringBuilder builder)
    {
        builder.AppendLine("    static void AxomLogInvocation(string name, object?[] args)");
        builder.AppendLine("    {");
        builder.AppendLine("        var rendered = string.Join(\", \", args.Select(AxomStringify));");
        builder.AppendLine("        var timestamp = DateTimeOffset.Now.ToString(\"yyyy-MM-dd HH:mm:ss\", System.Globalization.CultureInfo.InvariantCulture);");
        builder.AppendLine("        Console.WriteLine($\"{timestamp} [log] call {name}({rendered})\");");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    static void AxomLogReturn(string name, object? value)");
        builder.AppendLine("    {");
        builder.AppendLine("        var timestamp = DateTimeOffset.Now.ToString(\"yyyy-MM-dd HH:mm:ss\", System.Globalization.CultureInfo.InvariantCulture);");
        builder.AppendLine("        Console.WriteLine($\"{timestamp} [log] return {name} => {AxomStringify(value)}\");");
        builder.AppendLine("    }");
    }

    private static void WriteStringifyHelper(StringBuilder builder)
    {
        builder.AppendLine("    static string AxomStringify(object? value)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (value is not null)");
        builder.AppendLine("        {");
        builder.AppendLine("            var type = value.GetType();");
        builder.AppendLine("            var tagProperty = type.GetProperty(\"Tag\");");
        builder.AppendLine("            var payloadProperty = type.GetProperty(\"Value\");");
        builder.AppendLine("            if (tagProperty is not null && payloadProperty is not null && tagProperty.PropertyType == typeof(string))");
        builder.AppendLine("            {");
        builder.AppendLine("                var tag = tagProperty.GetValue(value) as string ?? string.Empty;");
        builder.AppendLine("                var payload = payloadProperty.GetValue(value);");
        builder.AppendLine("                if (payload is null)");
        builder.AppendLine("                {");
        builder.AppendLine("                    return tag;");
        builder.AppendLine("                }");
        builder.AppendLine();
        builder.AppendLine("                return $\"{tag}({AxomStringify(payload)})\";");
        builder.AppendLine("            }");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        return value switch");
        builder.AppendLine("        {");
        builder.AppendLine("            null => string.Empty,");
        builder.AppendLine("            double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),");
        builder.AppendLine("            float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture),");
        builder.AppendLine("            _ => value.ToString() ?? string.Empty");
        builder.AppendLine("        };");
        builder.AppendLine("    }");
    }

    private static void WriteFormatHelper(StringBuilder builder)
    {
        builder.AppendLine("    static string AxomFormat(object? value, string specifier)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (string.IsNullOrEmpty(specifier))");
        builder.AppendLine("        {");
        builder.AppendLine("            return AxomStringify(value);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        if (value is IFormattable formattable)");
        builder.AppendLine("        {");
        builder.AppendLine("            try");
        builder.AppendLine("            {");
        builder.AppendLine("                return formattable.ToString(specifier, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;");
        builder.AppendLine("            }");
        builder.AppendLine("            catch (FormatException)");
        builder.AppendLine("            {");
        builder.AppendLine("            }");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        if (value is string text)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (string.Equals(specifier, \"upper\", StringComparison.OrdinalIgnoreCase))");
        builder.AppendLine("            {");
        builder.AppendLine("                return text.ToUpperInvariant();");
        builder.AppendLine("            }");
        builder.AppendLine();
        builder.AppendLine("            if (string.Equals(specifier, \"lower\", StringComparison.OrdinalIgnoreCase))");
        builder.AppendLine("            {");
        builder.AppendLine("                return text.ToLowerInvariant();");
        builder.AppendLine("            }");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        return AxomStringify(value);");
        builder.AppendLine("    }");
    }

    private static void WriteRandomHelpers(StringBuilder builder)
    {
        builder.AppendLine("    static readonly object AxomRandomLock = new();");
        builder.AppendLine("    static Random AxomRandomState = new();");
        builder.AppendLine();
        builder.AppendLine("    static void AxomRandSeed(int seed)");
        builder.AppendLine("    {");
        builder.AppendLine("        lock (AxomRandomLock)");
        builder.AppendLine("        {");
        builder.AppendLine("            AxomRandomState = new Random(seed);");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    static double AxomRandFloat()");
        builder.AppendLine("    {");
        builder.AppendLine("        lock (AxomRandomLock)");
        builder.AppendLine("        {");
        builder.AppendLine("            return AxomRandomState.NextDouble();");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    static AxomResult<int> AxomRandInt(int max)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (max <= 0)");
        builder.AppendLine("        {");
        builder.AppendLine("            return AxomResult<int>.Error(\"max must be > 0\");");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        lock (AxomRandomLock)");
        builder.AppendLine("        {");
        builder.AppendLine("            return AxomResult<int>.Ok(AxomRandomState.Next(max));");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
    }

    private static void WriteRangeHelper(StringBuilder builder)
    {
        builder.AppendLine("    static List<int> AxomRange(int start, int end, int step = 1)");
        builder.AppendLine("    {");
        builder.AppendLine("        var values = new List<int>();");
        builder.AppendLine("        if (step == 0)");
        builder.AppendLine("        {");
        builder.AppendLine("            return values;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        if (step > 0)");
        builder.AppendLine("        {");
        builder.AppendLine("            for (var i = start; i < end; i += step)");
        builder.AppendLine("            {");
        builder.AppendLine("                values.Add(i);");
        builder.AppendLine("            }");
        builder.AppendLine("        }");
        builder.AppendLine("        else");
        builder.AppendLine("        {");
        builder.AppendLine("            for (var i = start; i > end; i += step)");
        builder.AppendLine("            {");
        builder.AppendLine("                values.Add(i);");
        builder.AppendLine("            }");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        return values;");
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
        if (string.IsNullOrEmpty(name))
        {
            return "_";
        }

        var builder = new StringBuilder(name.Length + 1);
        var first = name[0];
        if (char.IsLetter(first) || first == '_')
        {
            builder.Append(first);
        }
        else
        {
            builder.Append('_');
            builder.Append(char.IsLetterOrDigit(first) ? first : '_');
        }

        for (var i = 1; i < name.Length; i++)
        {
            var ch = name[i];
            builder.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');
        }

        var sanitized = builder.ToString();
        return CSharpKeywords.Contains(sanitized) ? $"@{sanitized}" : sanitized;
    }

    private static string TypeToCSharp(TypeSymbol type)
    {
        if (type.ResultValueType is not null && type.ResultErrorType is not null)
        {
            return $"AxomResult<{TypeToCSharp(type.ResultValueType)}>";
        }

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
        builder.AppendLine("    public BlockingCollection<T> Queue { get; }");
        builder.AppendLine("    public AxomChannelState(int capacity)");
        builder.AppendLine("    {");
        builder.AppendLine("        Queue = new BlockingCollection<T>(new ConcurrentQueue<T>(), capacity);");
        builder.AppendLine("    }");
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
        builder.AppendLine("    public AxomResult<T> recv()");
        builder.AppendLine("    {");
        builder.AppendLine("        return AxomResult<T>.Ok(state.Queue.Take());");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("static class AxomChannels");
        builder.AppendLine("{");
        builder.AppendLine("    public static (AxomSender<T>, AxomReceiver<T>) channel<T>(int capacity = 64)");
        builder.AppendLine("    {");
        builder.AppendLine("        var state = new AxomChannelState<T>(capacity);");
        builder.AppendLine("        return (new AxomSender<T>(state), new AxomReceiver<T>(state));");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }

    private static void WriteAxomResultRuntime(StringBuilder builder)
    {
        builder.AppendLine("sealed class AxomResult<T>");
        builder.AppendLine("{");
        builder.AppendLine("    public string Tag { get; }");
        builder.AppendLine("    public object? Value { get; }");
        builder.AppendLine("    private AxomResult(string tag, object? value)");
        builder.AppendLine("    {");
        builder.AppendLine("        Tag = tag;");
        builder.AppendLine("        Value = value;");
        builder.AppendLine("    }");
        builder.AppendLine("    public static AxomResult<T> Ok(T value) => new(\"Ok\", value);");
        builder.AppendLine("    public static AxomResult<T> Error(string message) => new(\"Error\", message);");
        builder.AppendLine("    public override string ToString() => Tag == \"Ok\" ? $\"Ok({Value})\" : $\"Error({Value})\";");
        builder.AppendLine("}");
    }

    private static void WriteDotNetInteropRuntime(StringBuilder builder)
    {
        var whitelist = DotNetInteropWhitelist.GetAllowedMethods();
        var allowedTypes = whitelist.Keys.ToList();

        builder.AppendLine("static class DotNetInterop");
        builder.AppendLine("{");
        builder.AppendLine("    public static T Call<T>(string typeName, string methodName, params object?[] args)");
        builder.AppendLine("    {");
        builder.AppendLine("        var result = TryCall<T>(typeName, methodName, args);");
        builder.AppendLine("        if (result.Tag == \"Error\")");
        builder.AppendLine("        {");
        builder.AppendLine("            throw new InvalidOperationException(result.Value?.ToString() ?? \"dotnet call failed.\");");
        builder.AppendLine("        }");
        builder.AppendLine("        return (T)result.Value!;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public static AxomResult<T> TryCall<T>(string typeName, string methodName, params object?[] args)");
        builder.AppendLine("    {");
        builder.AppendLine($"        if ({BuildAllowedTypeCondition("typeName", allowedTypes)} == false)");
        builder.AppendLine("        {");
        builder.AppendLine("            return AxomResult<T>.Error($\"dotnet type '{typeName}' is not allowed.\");");
        builder.AppendLine("        }");
        builder.AppendLine($"        if ({BuildAllowedMethodCondition("typeName", "methodName", whitelist)} == false)");
        builder.AppendLine("        {");
        builder.AppendLine("            return AxomResult<T>.Error($\"dotnet method '{typeName}.{methodName}' is not allowed.\");");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        var targetType = typeName switch");
        builder.AppendLine("        {");
        foreach (var typeName in allowedTypes)
        {
            builder.AppendLine($"            \"{EscapeString(typeName)}\" => Type.GetType(\"{EscapeString(typeName)}\", throwOnError: true)!,");
        }

        builder.AppendLine("            _ => throw new InvalidOperationException(\"dotnet type is not allowed.\")");
        builder.AppendLine("        };");
        builder.AppendLine("        var methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Static)");
        builder.AppendLine("            .Where(m => string.Equals(m.Name, methodName, StringComparison.Ordinal) && m.GetParameters().Length == args.Length)");
        builder.AppendLine("            .ToList();");
        builder.AppendLine("        foreach (var method in methods)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (!TryConvertArgs(args, method.GetParameters(), out var converted))");
        builder.AppendLine("            {");
        builder.AppendLine("                continue;");
        builder.AppendLine("            }");
        builder.AppendLine();
        builder.AppendLine("            try");
        builder.AppendLine("            {");
        builder.AppendLine("                var raw = method.Invoke(null, converted);");
        builder.AppendLine("                if (raw is T typed)");
        builder.AppendLine("                {");
        builder.AppendLine("                    return AxomResult<T>.Ok(typed);");
        builder.AppendLine("                }");
        builder.AppendLine("                if (typeof(T) == typeof(double) && raw is long l)");
        builder.AppendLine("                {");
        builder.AppendLine("                    return AxomResult<T>.Ok((T)(object)(double)l);");
        builder.AppendLine("                }");
        builder.AppendLine("                return AxomResult<T>.Error(\"dotnet return type mismatch.\");");
        builder.AppendLine("            }");
        builder.AppendLine("            catch (Exception ex)");
        builder.AppendLine("            {");
        builder.AppendLine("                return AxomResult<T>.Error(ex.InnerException?.Message ?? ex.Message);");
        builder.AppendLine("            }");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        return AxomResult<T>.Error($\"dotnet method '{typeName}.{methodName}' not found for provided arguments.\");");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static bool TryConvertArgs(object?[] args, ParameterInfo[] parameters, out object?[] converted)");
        builder.AppendLine("    {");
        builder.AppendLine("        converted = new object?[args.Length];");
        builder.AppendLine("        for (var i = 0; i < args.Length; i++)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (!TryConvertArg(args[i], parameters[i].ParameterType, out var value))");
        builder.AppendLine("            {");
        builder.AppendLine("                return false;");
        builder.AppendLine("            }");
        builder.AppendLine("            converted[i] = value;");
        builder.AppendLine("        }");
        builder.AppendLine("        return true;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static bool TryConvertArg(object? value, Type targetType, out object? converted)");
        builder.AppendLine("    {");
        builder.AppendLine("        converted = null;");
        builder.AppendLine("        if (targetType == typeof(long))");
        builder.AppendLine("        {");
        builder.AppendLine("            if (value is long l) { converted = l; return true; }");
        builder.AppendLine("            if (value is int i) { converted = (long)i; return true; }");
        builder.AppendLine("            return false;");
        builder.AppendLine("        }");
        builder.AppendLine("        if (targetType == typeof(double))");
        builder.AppendLine("        {");
        builder.AppendLine("            if (value is double d) { converted = d; return true; }");
        builder.AppendLine("            if (value is long li) { converted = (double)li; return true; }");
        builder.AppendLine("            return false;");
        builder.AppendLine("        }");
        builder.AppendLine("        if (targetType == typeof(bool) && value is bool b) { converted = b; return true; }");
        builder.AppendLine("        if (targetType == typeof(string) && value is string s) { converted = s; return true; }");
        builder.AppendLine("        return false;");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }

    private static string BuildAllowedTypeCondition(string typeVar, IReadOnlyList<string> types)
    {
        if (types.Count == 0)
        {
            return "false";
        }

        return string.Join(" || ", types.Select(type => $"string.Equals({typeVar}, \"{EscapeString(type)}\", StringComparison.Ordinal)"));
    }

    private static string BuildAllowedMethodCondition(
        string typeVar,
        string methodVar,
        IReadOnlyDictionary<string, IReadOnlyList<string>> whitelist)
    {
        var groups = new List<string>();
        foreach (var (type, methods) in whitelist)
        {
            if (methods.Count == 0)
            {
                continue;
            }

            var methodCondition = string.Join(" || ", methods.Select(method =>
                $"string.Equals({methodVar}, \"{EscapeString(method)}\", StringComparison.Ordinal)"));
            groups.Add($"(string.Equals({typeVar}, \"{EscapeString(type)}\", StringComparison.Ordinal) && ({methodCondition}))");
        }

        if (groups.Count == 0)
        {
            return "false";
        }

        return string.Join(" || ", groups);
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
