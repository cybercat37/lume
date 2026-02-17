using Axom.Compiler.Binding;
using Axom.Compiler.Parsing;
using Axom.Compiler.Text;

public class HttpClientBuiltinTypeTests
{
    [Fact]
    public void Http_client_builtins_type_check_for_valid_signatures()
    {
        var sourceText = new SourceText(
            "let client = http(\"http://127.0.0.1:8080\") |> http_timeout(2000) |> header(\"x-test\", \"ok\")\nlet request = client |> get(\"/health\") |> header(\"x-req\", \"v\")\nlet sent = send(request)\nprint sent",
            "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }

    [Fact]
    public void Http_builtin_rejects_non_string_base_url()
    {
        var sourceText = new SourceText("let client = http(1)", "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.NotEmpty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }

    [Fact]
    public void Http_error_payload_can_be_matched_by_variant()
    {
        var sourceText = new SourceText(
            "let sent = http(\"not-a-url\") |> get(\"/health\") |> send()\nprint match sent {\n  Ok(resp) -> \"ok\"\n  Error(err) -> match err {\n    InvalidUrl(msg) -> msg\n    Timeout(msg) -> msg\n    NetworkError(msg) -> msg\n    StatusError(msg) -> msg\n  }\n}",
            "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }

    [Fact]
    public void Http_request_builders_type_check_for_extended_verbs_and_decorators()
    {
        var sourceText = new SourceText(
            "let client = http(\"http://127.0.0.1:8080\")\nlet putReq = client |> put(\"/users/1\", \"payload\") |> header(\"x-id\", \"1\") |> request_text(\"override\") |> json(\"{\\\"id\\\":1}\") |> accept_json()\nlet patchReq = client |> patch(\"/users/1\", \"patch\")\nlet deleteReq = client |> delete(\"/users/1\")\nprint putReq\nprint patchReq\nprint deleteReq",
            "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }

    [Fact]
    public void Http_config_sugar_type_checks_and_desugars_to_http_pipeline()
    {
        var sourceText = new SourceText(
            "let client = http { baseUrl: \"http://127.0.0.1:8080\", headers: [\"x-test\": \"ok\"], timeout: 1500, retry: 2 }\nlet request = client |> get(\"/health\")\nprint request",
            "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }

    [Fact]
    public void Http_status_range_and_require_range_type_check()
    {
        var sourceText = new SourceText(
            "let sent = http(\"http://127.0.0.1:8080\") |> get(\"/health\") |> send()\nprint match sent {\n  Ok(resp) -> require_range(resp, 200..299)\n  Error(_) -> sent\n}",
            "test.axom");
        var syntaxTree = SyntaxTree.Parse(sourceText);

        var binder = new Binder();
        var result = binder.Bind(syntaxTree);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == Axom.Compiler.Diagnostics.DiagnosticSeverity.Error));
    }
}
