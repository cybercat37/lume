namespace Lume.Compiler.Lexing;

public enum TokenKind
{
    BadToken,
    EndOfFile,
    NewLine,
    Identifier,
    PrintKeyword,
    PrintlnKeyword,
    InputKeyword,
    LetKeyword,
    MutKeyword,
    TrueKeyword,
    FalseKeyword,
    NumberLiteral,
    StringLiteral,
    EqualsToken,
    Plus,
    Minus,
    Star,
    Slash,
    OpenParen,
    CloseParen,
    OpenBrace,
    CloseBrace,
    Semicolon
}
