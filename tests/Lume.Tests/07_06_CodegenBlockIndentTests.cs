using Lume.Compiler;

public class CodegenBlockIndentTests
{
    [Fact]
    public void Compile_block_emits_indented_statements()
    {
        var compiler = new CompilerDriver();
        var result = compiler.Compile("{\nprint 1\n}", "test.lume");

        Assert.True(result.Success);
        Assert.Contains("{", result.GeneratedCode);
        Assert.Contains("    Console.WriteLine(1);", result.GeneratedCode);
        Assert.Contains("}", result.GeneratedCode);
    }

    [Fact]
    public void Compile_nested_blocks_three_levels()
    {
        var compiler = new CompilerDriver();
        var source = """
            {
                let a = 1
                {
                    let b = 2
                    {
                        let c = 3
                        print c
                    }
                }
            }
            """;
        var result = compiler.Compile(source, "test.lume");

        Assert.True(result.Success);
        Assert.Contains("var a = 1;", result.GeneratedCode);
        Assert.Contains("var b = 2;", result.GeneratedCode);
        Assert.Contains("var c = 3;", result.GeneratedCode);
    }

    [Fact]
    public void Compile_nested_blocks_preserve_indentation_depth()
    {
        var compiler = new CompilerDriver();
        var source = "{\n{\nprint 1\n}\n}";
        var result = compiler.Compile(source, "test.lume");

        Assert.True(result.Success);
        // Verifica che ci siano almeno 3 livelli di indentazione (Main + 2 blocchi)
        Assert.Contains("                Console.WriteLine(1);", result.GeneratedCode);
    }

    [Fact]
    public void Compile_sequential_blocks()
    {
        var compiler = new CompilerDriver();
        var source = """
            { print 1 }
            { print 2 }
            """;
        var result = compiler.Compile(source, "test.lume");

        Assert.True(result.Success);
        Assert.Contains("Console.WriteLine(1);", result.GeneratedCode);
        Assert.Contains("Console.WriteLine(2);", result.GeneratedCode);
    }
}
