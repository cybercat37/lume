using Axom.Compiler.Binding;

namespace Axom.Compiler.Lowering;

public sealed class LoweredDotNetCallExpression : LoweredExpression
{
    public bool IsTryCall { get; }
    public TypeSymbol ReturnType { get; }
    public LoweredExpression TypeNameExpression { get; }
    public LoweredExpression MethodNameExpression { get; }
    public IReadOnlyList<LoweredExpression> Arguments { get; }

    public LoweredDotNetCallExpression(
        bool isTryCall,
        TypeSymbol returnType,
        LoweredExpression typeNameExpression,
        LoweredExpression methodNameExpression,
        IReadOnlyList<LoweredExpression> arguments)
    {
        IsTryCall = isTryCall;
        ReturnType = returnType;
        TypeNameExpression = typeNameExpression;
        MethodNameExpression = methodNameExpression;
        Arguments = arguments;
    }

    public override TypeSymbol Type =>
        IsTryCall ? TypeSymbol.Result(ReturnType, TypeSymbol.String) : ReturnType;
}
