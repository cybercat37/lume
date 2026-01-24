namespace Lume.Compiler.Diagnostics;

public sealed class Diagnostic
{
    public string File { get; }
    public int Line { get; }
    public int Column { get; }
    public string Message { get; }
    public DiagnosticSeverity Severity { get; }

    private Diagnostic(
        DiagnosticSeverity severity,
        string file,
        int line,
        int column,
        string message)
    {
        Severity = severity;
        File = file;
        Line = line;
        Column = column;
        Message = message;
    }

    public static Diagnostic Error(
        string file,
        int line,
        int column,
        string message) =>
        new(DiagnosticSeverity.Error, file, line, column, message);

    public override string ToString() =>
        $"{File}({Line},{Column}): {Severity}: {Message}";
}
