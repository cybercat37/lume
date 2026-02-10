namespace Axom.Compiler.Binding;

public sealed class BoundDotNetCallExpression : BoundExpression
{
    public bool IsTryCall { get; }
    public TypeSymbol ReturnType { get; }
    public BoundExpression TypeNameExpression { get; }
    public BoundExpression MethodNameExpression { get; }
    public IReadOnlyList<BoundExpression> Arguments { get; }

    public BoundDotNetCallExpression(
        bool isTryCall,
        TypeSymbol returnType,
        BoundExpression typeNameExpression,
        BoundExpression methodNameExpression,
        IReadOnlyList<BoundExpression> arguments)
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
