namespace Axom.Compiler.Text;

public sealed class SourceText
{
    public string Text { get; }
    public string FileName { get; }

    public SourceText(string text, string fileName)
    {
        Text = text;
        FileName = fileName;
    }

    public (int line, int column) GetLineAndColumn(int position)
    {
        var line = 1;
        var column = 1;

        for (var i = 0; i < position && i < Text.Length; i++)
        {
            if (Text[i] == '\r')
            {
                if (i + 1 < Text.Length && Text[i + 1] == '\n')
                {
                    i++;
                }

                line++;
                column = 1;
                continue;
            }

            if (Text[i] == '\n')
            {
                line++;
                column = 1;
                continue;
            }

            column++;
        }

        return (line, column);
    }
}
