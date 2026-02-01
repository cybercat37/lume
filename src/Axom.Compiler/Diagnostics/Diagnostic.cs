using Axom.Compiler.Text;

namespace Axom.Compiler.Diagnostics;

public sealed class Diagnostic
{
    public string File { get; }
    public int Line { get; }
    public int Column { get; }
    public string Message { get; }
    public DiagnosticSeverity Severity { get; }
    public TextSpan Span { get; }

    private Diagnostic(
        DiagnosticSeverity severity,
        string file,
        int line,
        int column,
        string message,
        TextSpan span)
    {
        Severity = severity;
        File = file;
        Line = line;
        Column = column;
        Message = message;
        Span = span;
    }

    public static Diagnostic Error(
        string file,
        int line,
        int column,
        string message) =>
        new(DiagnosticSeverity.Error, file, line, column, message, new TextSpan(0, 0));

    public static Diagnostic Error(
        SourceText sourceText,
        TextSpan span,
        string message)
    {
        var (line, column) = sourceText.GetLineAndColumn(span.Start);
        return new Diagnostic(
            DiagnosticSeverity.Error,
            sourceText.FileName,
            line,
            column,
            message,
            span);
    }

    public override string ToString() =>
        $"{File}({Line},{Column}): {Severity}: {Message}";
}
